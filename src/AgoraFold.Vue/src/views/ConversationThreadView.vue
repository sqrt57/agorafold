<script setup lang="ts">
import { onUnmounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import * as conversationsApi from '../api/conversations'
import { ApiError } from '../api/client'
import type { ConversationMessage, ConversationThread } from '../api/types'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const auth = useAuthStore()
const thread = ref<ConversationThread | null>(null)
const replyBody = ref('')
const error = ref('')
const connectionState = ref<'connecting' | 'connected' | 'reconnecting' | 'offline'>('connecting')
// The in-flight send. The draft stays in the textarea until the server acknowledges
// persistence (socket ack or HTTP response); an ambiguous failure retries over HTTP with
// the same clientMessageId, which the server treats idempotently.
const pendingSend = ref<{ clientMessageId: string; body: string } | null>(null)

const MAX_RECONNECT_ATTEMPTS = 8
const ACK_TIMEOUT_MS = 10000

let socket: WebSocket | null = null
let reconnectTimer: ReturnType<typeof setTimeout> | null = null
let ackTimer: ReturnType<typeof setTimeout> | null = null
let reconnectAttempt = 0
let disposed = false
let connectionVersion = 0
// The ack timeout and the socket-close handler can both decide to fall back to HTTP for
// the same send. Only the first attempt may run — a duplicate would complete later and
// mutate send state that no longer belongs to it.
let httpSendStartedFor: string | null = null
// Live messages that arrive over the socket before the thread snapshot has loaded.
let bufferedLive: ConversationMessage[] = []

function displayError(err: unknown): string {
  return err instanceof ApiError ? (err.errors[0] ?? err.message) : (err as Error).message
}

function toMessage(m: conversationsApi.ConversationWebSocketMessage): ConversationMessage {
  return {
    id: m.id,
    senderDisplayName: m.senderDisplayName,
    body: m.body,
    sentAt: m.sentAt,
    isMine: m.senderId === auth.user?.id,
  }
}

// All message arrival paths (snapshot, live broadcast, ack, HTTP reply response) funnel
// through this union-by-id merge, so duplicates collapse and rendering follows the
// canonical thread order — sentAt ascending with id as tiebreak, the same rule
// ConversationService applies to snapshots — not arrival order. sentAt must be parsed,
// not compared as a string: the serializer trims trailing zeros in fractional seconds,
// which breaks lexicographic ordering.
function mergeIntoThread(incoming: ConversationMessage[]) {
  if (!thread.value) {
    bufferedLive.push(...incoming)
    return
  }

  const byId = new Map(thread.value.messages.map((m) => [m.id, m]))
  for (const m of incoming) byId.set(m.id, m)
  thread.value.messages = [...byId.values()].sort(
    (a, b) => Date.parse(a.sentAt) - Date.parse(b.sentAt) || a.id - b.id,
  )
}

async function load(id: string, version: number) {
  try {
    const loadedThread = await conversationsApi.getThread(id)
    if (version !== connectionVersion || disposed) return

    if (thread.value) {
      mergeIntoThread(loadedThread.messages)
    } else {
      thread.value = loadedThread
      mergeIntoThread(bufferedLive)
      bufferedLive = []
    }
  } catch (err) {
    if (version === connectionVersion && !disposed) {
      error.value = displayError(err)
    }
  }
}

function clearAckTimer() {
  if (ackTimer) {
    clearTimeout(ackTimer)
    ackTimer = null
  }
}

function closeSocket() {
  if (socket) {
    socket.onclose = null
    socket.close()
    socket = null
  }
}

function scheduleReconnect(id: string, version: number) {
  if (disposed || version !== connectionVersion || reconnectTimer) return

  if (reconnectAttempt >= MAX_RECONNECT_ATTEMPTS) {
    connectionState.value = 'offline'
    return
  }

  const delay = Math.min(1000 * 2 ** reconnectAttempt, 10000)
  reconnectAttempt += 1
  connectionState.value = 'reconnecting'
  reconnectTimer = setTimeout(async () => {
    reconnectTimer = null
    // HTTP reload keeps the thread serviceable while the socket is down; merge-by-id
    // means a concurrent live delivery can't be clobbered or duplicated.
    await load(id, version)
    if (!disposed && version === connectionVersion) {
      connect(id, version)
    }
  }, delay)
}

function connect(id: string, version: number) {
  if (disposed || version !== connectionVersion) return

  closeSocket()
  connectionState.value = reconnectAttempt > 0 ? 'reconnecting' : 'connecting'
  socket = conversationsApi.openSocket(id)

  socket.onopen = () => {
    if (version !== connectionVersion) return
    reconnectAttempt = 0
    connectionState.value = 'connected'
    error.value = ''
  }

  socket.onmessage = (event) => {
    if (version !== connectionVersion) return

    try {
      const messageEvent = JSON.parse(event.data) as conversationsApi.ConversationWebSocketEvent

      if (messageEvent.type === 'connected') {
        // The server has registered this socket for broadcasts, so a snapshot fetched
        // now (merged by id with anything delivered meanwhile) cannot miss messages —
        // this closes the load/subscribe race on both initial connect and reconnect.
        void load(id, version)
        return
      }

      if (messageEvent.type === 'error') {
        error.value = messageEvent.error ?? 'The server rejected the message.'
        if (pendingSend.value && messageEvent.clientMessageId === pendingSend.value.clientMessageId) {
          // The send definitively failed; keep the draft for the user to fix and retry.
          clearAckTimer()
          pendingSend.value = null
        }
        return
      }

      if (!messageEvent.message) return

      if (messageEvent.type === 'ack') {
        mergeIntoThread([toMessage(messageEvent.message)])
        if (pendingSend.value && messageEvent.clientMessageId === pendingSend.value.clientMessageId) {
          clearAckTimer()
          pendingSend.value = null
          replyBody.value = ''
        }
        error.value = ''
        return
      }

      if (messageEvent.type === 'message') {
        mergeIntoThread([toMessage(messageEvent.message)])
        error.value = ''
      }
    } catch {
      error.value = 'Received an invalid message from the server.'
    }
  }

  socket.onerror = () => {
    error.value = 'The live chat connection failed. Reconnecting…'
  }

  socket.onclose = () => {
    if (disposed || version !== connectionVersion) return

    if (pendingSend.value) {
      // Ambiguous outcome: the message may or may not have been persisted before the
      // drop. Retrying over HTTP with the same clientMessageId is safe either way.
      clearAckTimer()
      const { clientMessageId, body } = pendingSend.value
      void sendViaHttp(id, clientMessageId, body, version)
    }

    scheduleReconnect(id, version)
  }
}

async function sendViaHttp(id: string, clientMessageId: string, body: string, version: number) {
  if (httpSendStartedFor === clientMessageId) return
  httpSendStartedFor = clientMessageId

  pendingSend.value = { clientMessageId, body }
  try {
    const loadedThread = await conversationsApi.reply(id, body, clientMessageId)
    if (version !== connectionVersion || disposed) return
    mergeIntoThread(loadedThread.messages)
    // A socket ack may have completed this send (and a newer send may have started)
    // while the request was in flight — only touch send state that is still ours.
    if (pendingSend.value?.clientMessageId === clientMessageId) {
      replyBody.value = ''
      error.value = ''
    }
  } catch (err) {
    if (version === connectionVersion && !disposed && pendingSend.value?.clientMessageId === clientMessageId) {
      error.value = displayError(err)
    }
  } finally {
    if (version === connectionVersion && !disposed && pendingSend.value?.clientMessageId === clientMessageId) {
      pendingSend.value = null
    }
  }
}

function sendReply() {
  if (pendingSend.value) return
  error.value = ''

  const id = route.params.id as string
  const version = connectionVersion
  const body = replyBody.value
  const clientMessageId = crypto.randomUUID()

  if (socket && socket.readyState === WebSocket.OPEN) {
    pendingSend.value = { clientMessageId, body }
    socket.send(JSON.stringify({ type: 'message', body, clientMessageId }))

    // No ack and no disconnect within the window — resolve the ambiguity by retrying
    // over HTTP with the same clientMessageId (idempotent server-side).
    clearAckTimer()
    ackTimer = setTimeout(() => {
      ackTimer = null
      if (pendingSend.value?.clientMessageId === clientMessageId && !disposed && version === connectionVersion) {
        void sendViaHttp(id, clientMessageId, body, version)
      }
    }, ACK_TIMEOUT_MS)
  } else {
    // Live transport unavailable — fall back to the plain HTTP reply endpoint.
    void sendViaHttp(id, clientMessageId, body, version)
  }
}

async function start(id: string) {
  connectionVersion += 1
  const version = connectionVersion
  reconnectAttempt = 0
  error.value = ''
  thread.value = null
  replyBody.value = ''
  pendingSend.value = null
  bufferedLive = []
  clearAckTimer()
  closeSocket()

  if (reconnectTimer) {
    clearTimeout(reconnectTimer)
    reconnectTimer = null
  }

  await load(id, version)
  if (!disposed && version === connectionVersion && thread.value) {
    connect(id, version)
  }
}

watch(
  () => route.params.id as string,
  (id) => void start(id),
  { immediate: true },
)

onUnmounted(() => {
  disposed = true
  if (reconnectTimer) clearTimeout(reconnectTimer)
  clearAckTimer()
  closeSocket()
})
</script>

<template>
  <template v-if="thread">
    <h1>{{ thread.listingTitle }}</h1>
    <RouterLink :to="{ name: 'listing-detail', params: { id: thread.listingId } }">View listing</RouterLink>

    <p class="muted">
      <span v-if="connectionState === 'connected'">Live chat connected</span>
      <span v-else-if="connectionState === 'reconnecting'">Reconnecting to live chat…</span>
      <span v-else-if="connectionState === 'offline'">Live chat is unavailable — replies still send, but new messages need a refresh.</span>
      <span v-else>Connecting to live chat…</span>
    </p>

    <div>
      <div v-for="m in thread.messages" :key="m.id" class="message" :class="{ mine: m.isMine }">
        <div class="meta">{{ m.senderDisplayName }} &middot; {{ new Date(m.sentAt).toLocaleString() }}</div>
        <div>{{ m.body }}</div>
      </div>
    </div>

    <p v-if="error" class="error">{{ error }}</p>
    <form @submit.prevent="sendReply">
      <label>
        Reply
        <textarea v-model="replyBody" required :disabled="pendingSend !== null"></textarea>
      </label>
      <button type="submit" :disabled="pendingSend !== null">{{ pendingSend ? 'Sending…' : 'Send' }}</button>
    </form>
  </template>
  <p v-else-if="error" class="error">{{ error }}</p>
</template>
