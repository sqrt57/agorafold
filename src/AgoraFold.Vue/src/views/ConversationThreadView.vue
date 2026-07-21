<script setup lang="ts">
import { onUnmounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import * as conversationsApi from '../api/conversations'
import { ApiError } from '../api/client'
import type { ConversationThread } from '../api/types'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const auth = useAuthStore()
const thread = ref<ConversationThread | null>(null)
const replyBody = ref('')
const error = ref('')
const connectionState = ref<'connecting' | 'connected' | 'reconnecting' | 'closed'>('connecting')

let socket: WebSocket | null = null
let reconnectTimer: ReturnType<typeof setTimeout> | null = null
let reconnectAttempt = 0
let disposed = false
let connectionVersion = 0

function displayError(err: unknown): string {
  return err instanceof ApiError ? (err.errors[0] ?? err.message) : (err as Error).message
}

async function load(id: string, version: number) {
  try {
    const loadedThread = await conversationsApi.getThread(id)
    if (version === connectionVersion && !disposed) {
      thread.value = loadedThread
    }
  } catch (err) {
    if (version === connectionVersion && !disposed) {
      error.value = displayError(err)
    }
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

  const delay = Math.min(1000 * 2 ** reconnectAttempt, 10000)
  reconnectAttempt += 1
  connectionState.value = 'reconnecting'
  reconnectTimer = setTimeout(async () => {
    reconnectTimer = null
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
      if (messageEvent.type === 'error') {
        error.value = messageEvent.error ?? 'The server rejected the message.'
        return
      }

      if (messageEvent.type === 'message' && messageEvent.message && thread.value) {
        thread.value.messages.push({
          senderDisplayName: messageEvent.message.senderDisplayName,
          body: messageEvent.message.body,
          sentAt: messageEvent.message.sentAt,
          isMine: messageEvent.message.senderId === auth.user?.id,
        })
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
    scheduleReconnect(id, version)
  }
}

function sendReply() {
  error.value = ''

  if (!socket || socket.readyState !== WebSocket.OPEN) {
    error.value = 'The live chat connection is not ready yet.'
    return
  }

  socket.send(JSON.stringify({ type: 'message', body: replyBody.value }))
  replyBody.value = ''
}

async function start(id: string) {
  connectionVersion += 1
  const version = connectionVersion
  reconnectAttempt = 0
  error.value = ''
  thread.value = null
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
      <span v-else>Connecting to live chat…</span>
    </p>

    <div>
      <div v-for="(m, i) in thread.messages" :key="`${m.sentAt}-${i}`" class="message" :class="{ mine: m.isMine }">
        <div class="meta">{{ m.senderDisplayName }} &middot; {{ new Date(m.sentAt).toLocaleString() }}</div>
        <div>{{ m.body }}</div>
      </div>
    </div>

    <p v-if="error" class="error">{{ error }}</p>
    <form @submit.prevent="sendReply">
      <label>
        Reply
        <textarea v-model="replyBody" required></textarea>
      </label>
      <button type="submit" :disabled="connectionState !== 'connected'">Send</button>
    </form>
  </template>
  <p v-else-if="error" class="error">{{ error }}</p>
</template>
