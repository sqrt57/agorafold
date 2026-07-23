import { afterEach, beforeEach, expect, it, vi } from 'vitest'
import { reactive } from 'vue'
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import ConversationThreadView from './ConversationThreadView.vue'
import * as conversationsApi from '../api/conversations'
import type { ConversationMessage, ConversationThread } from '../api/types'
import { useAuthStore } from '../stores/auth'

// Reactive so tests can simulate in-app navigation between conversations; the factory's
// arrow reads mockRoute lazily, after this module has finished evaluating.
const mockRoute = reactive({ params: { id: '3' } })

vi.mock('vue-router', () => ({
  useRoute: () => mockRoute,
}))

vi.mock('../api/conversations')

const ACK_TIMEOUT_MS = 10000

// Stand-in for the browser WebSocket that lets tests drive the handlers directly.
class FakeSocket {
  readyState: number = WebSocket.CONNECTING
  onopen: (() => void) | null = null
  onmessage: ((event: { data: string }) => void) | null = null
  onerror: (() => void) | null = null
  onclose: ((event: { code: number }) => void) | null = null
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

  fireClose(code = 1006) {
    this.readyState = WebSocket.CLOSED
    this.onclose?.({ code })
  }
}

function makeMessage(id: number, body: string): ConversationMessage {
  return { id, senderDisplayName: 'Buyer', body, sentAt: '2026-07-23T12:00:00Z', isMine: true }
}

function makeThread(messages: ConversationMessage[] = []): ConversationThread {
  return { id: 3, listingId: 1, listingTitle: 'Vintage Bicycle', messages }
}

function makeSocketMessage(
  id: number,
  body: string,
  sentAt = '2026-07-23T12:00:00Z',
): conversationsApi.ConversationWebSocketMessage {
  return { id, senderId: 'buyer-1', senderDisplayName: 'Buyer', body, sentAt }
}

let wrapper: VueWrapper | null = null

// Mounts the view with a loaded thread and a connected fake socket. `snapshot` is
// returned in the given order, as ConversationService would (sentAt asc, id tiebreak).
async function mountConnected(snapshot: ConversationMessage[] = []) {
  const socket = new FakeSocket()
  vi.mocked(conversationsApi.openSocket).mockReturnValue(socket as unknown as WebSocket)
  vi.mocked(conversationsApi.getThread).mockImplementation(async () => makeThread(snapshot))

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
  mockRoute.params.id = '3'
  vi.useFakeTimers()
})

afterEach(() => {
  wrapper?.unmount()
  wrapper = null
  vi.useRealTimers()
  vi.resetAllMocks()
})

function renderedBodies(view: VueWrapper): string[] {
  return view.findAll('.message div:not(.meta)').map((el) => el.text())
}

// Drives the view through every reconnect attempt (each gets a socket that immediately
// drops) until the backoff gives up and the view parks in the offline state.
async function goOffline(socket: FakeSocket) {
  let current = socket
  for (let i = 0; i < 8; i++) {
    const next = new FakeSocket()
    vi.mocked(conversationsApi.openSocket).mockReturnValue(next as unknown as WebSocket)
    current.fireClose()
    vi.advanceTimersByTime(10000) // covers the largest backoff delay
    await flushPromises()
    current = next
  }
  current.fireClose()
  await flushPromises()
}

it('orders messages by sentAt then id, matching the snapshot order', async () => {
  // Concurrent sends can persist with sentAt order opposing id order. The snapshot
  // arrives in canonical order (sentAt asc, id tiebreak); the merge must preserve it,
  // not re-sort by id. The timestamps also differ only in fractional seconds of
  // different lengths, where lexicographic comparison inverts chronological order —
  // so an id-only sort and a string-compare sort both fail this test.
  const early = { ...makeMessage(6, 'early'), sentAt: '2026-07-23T12:00:00.12Z' }
  const late = { ...makeMessage(5, 'late'), sentAt: '2026-07-23T12:00:00.1234567Z' }
  const { view, socket } = await mountConnected([early, late])

  expect(renderedBodies(view)).toEqual(['early', 'late'])

  // A live broadcast merges in without disturbing the canonical order.
  socket.emit({ type: 'message', message: makeSocketMessage(7, 'newest', '2026-07-23T12:00:01Z') })
  await flushPromises()
  expect(renderedBodies(view)).toEqual(['early', 'late', 'newest'])
})

it('preserves server order for messages that differ only below a millisecond', async () => {
  // SentAt carries 100ns ticks, so two messages can differ only past the third
  // fractional digit. Date.parse truncates to milliseconds, which would make these
  // compare equal and fall back to id order — inverting the server's order.
  const early = { ...makeMessage(6, 'early'), sentAt: '2026-07-23T12:00:00.123Z' }
  const late = { ...makeMessage(5, 'late'), sentAt: '2026-07-23T12:00:00.1230001Z' }
  const { view } = await mountConnected([early, late])

  expect(renderedBodies(view)).toEqual(['early', 'late'])
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

it('ignores errors from an obsolete socket after switching conversations', async () => {
  const { view, socket } = await mountConnected()

  // Navigate to another conversation; it gets its own fresh socket.
  const nextSocket = new FakeSocket()
  vi.mocked(conversationsApi.openSocket).mockReturnValue(nextSocket as unknown as WebSocket)
  mockRoute.params.id = '4'
  await flushPromises()
  nextSocket.open()
  nextSocket.emit({ type: 'connected' })
  await flushPromises()

  // The previous conversation's socket now reports a failure. Its handler is stale —
  // the error must not leak into the newly selected thread.
  socket.onerror?.()
  await flushPromises()

  expect(view.find('.error').exists()).toBe(false)
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

it('stops reconnecting when the server revokes the session', async () => {
  const { view, socket } = await mountConnected()

  // The server's revalidation sends an error event, then closes with PolicyViolation.
  socket.emit({ type: 'error', error: 'Your session is no longer valid.' })
  socket.fireClose(1008)
  await flushPromises()

  expect(view.find('.error').text()).toContain('no longer valid')
  expect(view.text()).toContain('Live chat is unavailable')

  // No reconnect may be scheduled — the handshake would just repeat the 401.
  vi.mocked(conversationsApi.openSocket).mockClear()
  vi.advanceTimersByTime(60000)
  await flushPromises()
  expect(conversationsApi.openSocket).not.toHaveBeenCalled()
})

it('reconnects with a fresh backoff budget when Retry is clicked while offline', async () => {
  const { view, socket } = await mountConnected()
  await goOffline(socket)
  expect(view.text()).toContain('Live chat is unavailable')

  const retrySocket = new FakeSocket()
  vi.mocked(conversationsApi.openSocket).mockClear().mockReturnValue(retrySocket as unknown as WebSocket)

  await view.find('button.retry').trigger('click')
  expect(conversationsApi.openSocket).toHaveBeenCalledTimes(1)

  retrySocket.open()
  retrySocket.emit({ type: 'connected' })
  await flushPromises()
  expect(view.text()).toContain('Live chat connected')
})

it('reconnects after a successful HTTP reply while offline', async () => {
  const { view, socket } = await mountConnected()
  await goOffline(socket)

  const retrySocket = new FakeSocket()
  vi.mocked(conversationsApi.openSocket).mockClear().mockReturnValue(retrySocket as unknown as WebSocket)
  vi.mocked(conversationsApi.reply).mockResolvedValue(makeThread([makeMessage(10, 'hello again')]))

  // With no open socket the send goes straight to HTTP; a successful round-trip proves
  // the server is reachable, so the live connection restarts.
  await sendReply(view, 'hello again')
  await flushPromises()

  expect(conversationsApi.reply).toHaveBeenCalledTimes(1)
  expect(conversationsApi.openSocket).toHaveBeenCalledTimes(1)
})
