import { afterEach, beforeEach, expect, it, vi } from 'vitest'
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import ConversationThreadView from './ConversationThreadView.vue'
import * as conversationsApi from '../api/conversations'
import type { ConversationMessage, ConversationThread } from '../api/types'
import { useAuthStore } from '../stores/auth'

vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { id: '3' } }),
}))

vi.mock('../api/conversations')

const ACK_TIMEOUT_MS = 10000

// Stand-in for the browser WebSocket that lets tests drive the handlers directly.
class FakeSocket {
  readyState: number = WebSocket.CONNECTING
  onopen: (() => void) | null = null
  onmessage: ((event: { data: string }) => void) | null = null
  onerror: (() => void) | null = null
  onclose: (() => void) | null = null
  sent: string[] = []

  send(data: string) {
    this.sent.push(data)
  }

  close() {
    this.readyState = WebSocket.CLOSED
  }

  open() {
    this.readyState = WebSocket.OPEN
    this.onopen?.()
  }

  emit(event: unknown) {
    this.onmessage?.({ data: JSON.stringify(event) })
  }

  fireClose() {
    this.readyState = WebSocket.CLOSED
    this.onclose?.()
  }
}

function makeMessage(id: number, body: string): ConversationMessage {
  return { id, senderDisplayName: 'Buyer', body, sentAt: '2026-07-23T12:00:00Z', isMine: true }
}

function makeThread(messages: ConversationMessage[] = []): ConversationThread {
  return { id: 3, listingId: 1, listingTitle: 'Vintage Bicycle', messages }
}

function makeSocketMessage(id: number, body: string): conversationsApi.ConversationWebSocketMessage {
  return { id, senderId: 'buyer-1', senderDisplayName: 'Buyer', body, sentAt: '2026-07-23T12:00:00Z' }
}

let wrapper: VueWrapper | null = null

// Mounts the view with a loaded thread and a connected fake socket.
async function mountConnected() {
  const socket = new FakeSocket()
  vi.mocked(conversationsApi.openSocket).mockReturnValue(socket as unknown as WebSocket)
  vi.mocked(conversationsApi.getThread).mockImplementation(async () => makeThread())

  const pinia = createPinia()
  setActivePinia(pinia)
  useAuthStore().user = { id: 'buyer-1', email: 'buyer@example.com', displayName: 'Buyer' }

  wrapper = mount(ConversationThreadView, {
    global: {
      plugins: [pinia],
      components: { RouterLink: { props: ['to'], template: '<a><slot /></a>' } },
    },
  })

  await flushPromises() // initial snapshot load
  socket.open()
  socket.emit({ type: 'connected' })
  await flushPromises() // post-subscribe reload

  return { view: wrapper, socket }
}

async function sendReply(view: VueWrapper, body: string) {
  await view.find('textarea').setValue(body)
  await view.find('form').trigger('submit')
}

beforeEach(() => {
  vi.useFakeTimers()
})

afterEach(() => {
  wrapper?.unmount()
  wrapper = null
  vi.useRealTimers()
  vi.resetAllMocks()
})

it('starts only one HTTP fallback when the ack timeout and socket close both fire', async () => {
  const { view, socket } = await mountConnected()
  vi.mocked(conversationsApi.reply).mockImplementation(() => new Promise(() => {}))

  await sendReply(view, 'hello there')
  expect(socket.sent).toHaveLength(1)

  // No ack arrives; the timeout falls back to HTTP.
  vi.advanceTimersByTime(ACK_TIMEOUT_MS)
  expect(conversationsApi.reply).toHaveBeenCalledTimes(1)

  // The socket then drops while that fallback is still in flight — it must not
  // start a second request for the same clientMessageId.
  socket.fireClose()
  expect(conversationsApi.reply).toHaveBeenCalledTimes(1)
})

it('keeps a newer send pending when a stale HTTP fallback completes', async () => {
  const { view, socket } = await mountConnected()

  let resolveStale!: (thread: ConversationThread) => void
  vi.mocked(conversationsApi.reply).mockImplementationOnce(
    () => new Promise((resolve) => (resolveStale = resolve)),
  )

  // First send gets no ack within the window, so an HTTP fallback starts.
  await sendReply(view, 'first message')
  const first = JSON.parse(socket.sent[0]!) as { clientMessageId: string }
  vi.advanceTimersByTime(ACK_TIMEOUT_MS)
  expect(conversationsApi.reply).toHaveBeenCalledTimes(1)

  // The late socket ack lands anyway and completes the first send normally.
  socket.emit({ type: 'ack', clientMessageId: first.clientMessageId, message: makeSocketMessage(10, 'first message') })
  await flushPromises()
  expect(view.find('textarea').element.value).toBe('')

  // A second send goes out over the socket and is still awaiting its ack.
  await sendReply(view, 'second message')
  expect(socket.sent).toHaveLength(2)

  // Now the stale fallback for the first message completes. It must not clear the
  // newer send's draft or unlock the form while that send is unresolved.
  resolveStale(makeThread([makeMessage(10, 'first message')]))
  await flushPromises()

  const textarea = view.find('textarea')
  expect(textarea.element.value).toBe('second message')
  expect(textarea.attributes('disabled')).toBeDefined()
  expect(view.find('button').text()).toBe('Sending…')
})
