import { act } from 'react'
import { cleanup, fireEvent, render } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router-dom'
import { afterEach, beforeEach, expect, it, vi } from 'vitest'
import ConversationThreadPage from './ConversationThreadPage'
import * as conversationsApi from '../api/conversations'
import type { ConversationMessage, ConversationThread } from '../api/types'

vi.mock('../api/conversations')
vi.mock('../context/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'buyer-1', email: 'buyer@example.com', displayName: 'Buyer' } }),
}))

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

// Advances React state past microtask-only continuations (mock promise resolutions)
// that neither fake timers nor RTL's own act wrapping otherwise flush.
async function flushPromises() {
  await act(async () => {
    for (let i = 0; i < 10; i++) await Promise.resolve()
  })
}

let router: ReturnType<typeof createMemoryRouter> | null = null

// Renders the page with a loaded thread and a connected fake socket. `snapshot` is
// returned in the given order, as ConversationService would (sentAt asc, id tiebreak).
async function mountConnected(snapshot: ConversationMessage[] = []) {
  const socket = new FakeSocket()
  vi.mocked(conversationsApi.openSocket).mockReturnValue(socket as unknown as WebSocket)
  vi.mocked(conversationsApi.getThread).mockImplementation(async () => makeThread(snapshot))

  router = createMemoryRouter([{ path: '/conversations/:id', element: <ConversationThreadPage /> }], {
    initialEntries: ['/conversations/3'],
  })
  const { container } = render(<RouterProvider router={router} />)

  await flushPromises() // initial snapshot load
  socket.open()
  socket.emit({ type: 'connected' })
  await flushPromises() // post-subscribe reload

  return { container, socket }
}

async function sendReply(container: HTMLElement, body: string) {
  const textarea = container.querySelector('textarea')!
  fireEvent.change(textarea, { target: { value: body } })
  await act(async () => {
    fireEvent.submit(container.querySelector('form')!)
  })
}

function renderedBodies(container: HTMLElement): string[] {
  return [...container.querySelectorAll('.message div:not(.meta)')].map((el) => el.textContent ?? '')
}

// Drives the page through every reconnect attempt (each gets a socket that immediately
// drops) until the backoff gives up and the page parks in the offline state.
async function goOffline(socket: FakeSocket) {
  let current = socket
  for (let i = 0; i < 8; i++) {
    const next = new FakeSocket()
    vi.mocked(conversationsApi.openSocket).mockReturnValue(next as unknown as WebSocket)
    current.fireClose()
    await act(async () => {
      await vi.advanceTimersByTimeAsync(10000) // covers the largest backoff delay
    })
    current = next
  }
  current.fireClose()
  await flushPromises()
}

beforeEach(() => {
  vi.useFakeTimers()
})

afterEach(() => {
  cleanup()
  router = null
  vi.useRealTimers()
  vi.resetAllMocks()
})

it('orders messages by sentAt then id, matching the snapshot order', async () => {
  // Concurrent sends can persist with sentAt order opposing id order. The snapshot
  // arrives in canonical order (sentAt asc, id tiebreak); the merge must preserve it,
  // not re-sort by id. The timestamps also differ only in fractional seconds of
  // different lengths, where lexicographic comparison inverts chronological order —
  // so an id-only sort and a string-compare sort both fail this test.
  const early = { ...makeMessage(6, 'early'), sentAt: '2026-07-23T12:00:00.12Z' }
  const late = { ...makeMessage(5, 'late'), sentAt: '2026-07-23T12:00:00.1234567Z' }
  const { container, socket } = await mountConnected([early, late])

  expect(renderedBodies(container)).toEqual(['early', 'late'])

  // A live broadcast merges in without disturbing the canonical order.
  await act(async () => {
    socket.emit({ type: 'message', message: makeSocketMessage(7, 'newest', '2026-07-23T12:00:01Z') })
  })
  await flushPromises()
  expect(renderedBodies(container)).toEqual(['early', 'late', 'newest'])
})

it('preserves server order for messages that differ only below a millisecond', async () => {
  // SentAt carries 100ns ticks, so two messages can differ only past the third
  // fractional digit. Date.parse truncates to milliseconds, which would make these
  // compare equal and fall back to id order — inverting the server's order.
  const early = { ...makeMessage(6, 'early'), sentAt: '2026-07-23T12:00:00.123Z' }
  const late = { ...makeMessage(5, 'late'), sentAt: '2026-07-23T12:00:00.1230001Z' }
  const { container } = await mountConnected([early, late])

  expect(renderedBodies(container)).toEqual(['early', 'late'])
})

it('starts only one HTTP fallback when the ack timeout and socket close both fire', async () => {
  const { container, socket } = await mountConnected()
  vi.mocked(conversationsApi.reply).mockImplementation(() => new Promise(() => {}))

  await sendReply(container, 'hello there')
  expect(socket.sent).toHaveLength(1)

  // No ack arrives; the timeout falls back to HTTP.
  await act(async () => {
    await vi.advanceTimersByTimeAsync(ACK_TIMEOUT_MS)
  })
  expect(conversationsApi.reply).toHaveBeenCalledTimes(1)

  // The socket then drops while that fallback is still in flight — it must not
  // start a second request for the same clientMessageId.
  await act(async () => {
    socket.fireClose()
  })
  expect(conversationsApi.reply).toHaveBeenCalledTimes(1)
})

it('ignores errors from an obsolete socket after switching conversations', async () => {
  const { container, socket } = await mountConnected()

  // Navigate to another conversation; it gets its own fresh socket.
  const nextSocket = new FakeSocket()
  vi.mocked(conversationsApi.openSocket).mockReturnValue(nextSocket as unknown as WebSocket)
  await act(async () => {
    await router!.navigate('/conversations/4')
  })
  await flushPromises()
  await act(async () => {
    nextSocket.open()
    nextSocket.emit({ type: 'connected' })
  })
  await flushPromises()

  // The previous conversation's socket now reports a failure. Its handler is stale —
  // the error must not leak into the newly selected thread.
  await act(async () => {
    socket.onerror?.()
  })
  await flushPromises()

  expect(container.querySelector('.error')).toBeNull()
})

it('keeps a newer send pending when a stale HTTP fallback completes', async () => {
  const { container, socket } = await mountConnected()

  let resolveStale!: (thread: ConversationThread) => void
  vi.mocked(conversationsApi.reply).mockImplementationOnce(
    () => new Promise((resolve) => (resolveStale = resolve)),
  )

  // First send gets no ack within the window, so an HTTP fallback starts.
  await sendReply(container, 'first message')
  const first = JSON.parse(socket.sent[0]!) as { clientMessageId: string }
  await act(async () => {
    await vi.advanceTimersByTimeAsync(ACK_TIMEOUT_MS)
  })
  expect(conversationsApi.reply).toHaveBeenCalledTimes(1)

  // The late socket ack lands anyway and completes the first send normally.
  await act(async () => {
    socket.emit({
      type: 'ack',
      clientMessageId: first.clientMessageId,
      message: makeSocketMessage(10, 'first message'),
    })
  })
  await flushPromises()
  expect((container.querySelector('textarea') as HTMLTextAreaElement).value).toBe('')

  // A second send goes out over the socket and is still awaiting its ack.
  await sendReply(container, 'second message')
  expect(socket.sent).toHaveLength(2)

  // Now the stale fallback for the first message completes. It must not clear the
  // newer send's draft or unlock the form while that send is unresolved.
  await act(async () => {
    resolveStale(makeThread([makeMessage(10, 'first message')]))
  })
  await flushPromises()

  const textarea = container.querySelector('textarea') as HTMLTextAreaElement
  expect(textarea.value).toBe('second message')
  expect(textarea.disabled).toBe(true)
  expect(container.querySelector('button[type="submit"]')?.textContent).toBe('Sending…')
})

it('stops reconnecting when the server revokes the session', async () => {
  const { container, socket } = await mountConnected()

  // The server's revalidation sends an error event, then closes with PolicyViolation.
  await act(async () => {
    socket.emit({ type: 'error', error: 'Your session is no longer valid.' })
    socket.fireClose(1008)
  })
  await flushPromises()

  expect(container.querySelector('.error')?.textContent).toContain('no longer valid')
  expect(container.textContent).toContain('Live chat is unavailable')

  // No reconnect may be scheduled — the handshake would just repeat the 401.
  vi.mocked(conversationsApi.openSocket).mockClear()
  await act(async () => {
    await vi.advanceTimersByTimeAsync(60000)
  })
  expect(conversationsApi.openSocket).not.toHaveBeenCalled()
})

it('reconnects with a fresh backoff budget when Retry is clicked while offline', async () => {
  const { container, socket } = await mountConnected()
  await goOffline(socket)
  expect(container.textContent).toContain('Live chat is unavailable')

  const retrySocket = new FakeSocket()
  vi.mocked(conversationsApi.openSocket).mockClear().mockReturnValue(retrySocket as unknown as WebSocket)

  await act(async () => {
    fireEvent.click(container.querySelector('button.retry')!)
  })
  expect(conversationsApi.openSocket).toHaveBeenCalledTimes(1)

  await act(async () => {
    retrySocket.open()
    retrySocket.emit({ type: 'connected' })
  })
  await flushPromises()
  expect(container.textContent).toContain('Live chat connected')
})

it('reconnects after a successful HTTP reply while offline', async () => {
  const { container, socket } = await mountConnected()
  await goOffline(socket)

  const retrySocket = new FakeSocket()
  vi.mocked(conversationsApi.openSocket).mockClear().mockReturnValue(retrySocket as unknown as WebSocket)
  vi.mocked(conversationsApi.reply).mockResolvedValue(makeThread([makeMessage(10, 'hello again')]))

  // With no open socket the send goes straight to HTTP; a successful round-trip proves
  // the server is reachable, so the live connection restarts.
  await sendReply(container, 'hello again')
  await flushPromises()

  expect(conversationsApi.reply).toHaveBeenCalledTimes(1)
  expect(conversationsApi.openSocket).toHaveBeenCalledTimes(1)
})
