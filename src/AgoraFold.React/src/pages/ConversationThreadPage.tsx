import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import * as conversationsApi from '../api/conversations'
import { ApiError } from '../api/client'
import type { ConversationMessage, ConversationThread } from '../api/types'
import { useAuth } from '../context/AuthContext'

type ConnectionState = 'connecting' | 'connected' | 'reconnecting' | 'offline'

interface PendingSend {
  clientMessageId: string
  body: string
}

const MAX_RECONNECT_ATTEMPTS = 8
const ACK_TIMEOUT_MS = 10000
// WebSocketCloseStatus.PolicyViolation — the server closes with this code only when the
// session stopped being valid (rotated security stamp, deactivated account).
const SESSION_REVOKED_CLOSE_CODE = 1008

function displayError(err: unknown): string {
  return err instanceof ApiError ? (err.errors[0] ?? err.message) : (err as Error).message
}

// sentAt carries the server's full 100ns-tick precision with trailing fractional zeros
// trimmed (".12Z" vs ".1234567Z"), so it can be neither string-compared (lexicographic
// order breaks across different fraction lengths) nor Date.parse'd (truncates to
// milliseconds, collapsing sub-millisecond neighbors into id-order ties that can
// contradict the server's ordering). Compare the fixed-width prefix up to whole
// seconds, then the zero-padded fractions.
function compareSentAt(a: string, b: string): number {
  const [aSeconds = '', aFraction = ''] = a.replace(/Z$/, '').split('.')
  const [bSeconds = '', bFraction = ''] = b.replace(/Z$/, '').split('.')
  if (aSeconds !== bSeconds) return aSeconds < bSeconds ? -1 : 1

  const aTicks = aFraction.padEnd(7, '0')
  const bTicks = bFraction.padEnd(7, '0')
  return aTicks < bTicks ? -1 : aTicks > bTicks ? 1 : 0
}

// All message arrival paths (snapshot, live broadcast, ack, HTTP reply response) funnel
// through this union-by-id merge, so duplicates collapse and rendering follows the
// canonical thread order — sentAt ascending with id as tiebreak, the same rule
// ConversationService applies to snapshots — not arrival order.
function mergeMessages(existing: ConversationMessage[], incoming: ConversationMessage[]): ConversationMessage[] {
  const byId = new Map(existing.map((m) => [m.id, m]))
  for (const m of incoming) byId.set(m.id, m)
  return [...byId.values()].sort((a, b) => compareSentAt(a.sentAt, b.sentAt) || a.id - b.id)
}

export default function ConversationThreadPage() {
  const { id } = useParams<{ id: string }>()
  const { user } = useAuth()

  const [thread, setThread] = useState<ConversationThread | null>(null)
  const [replyBody, setReplyBody] = useState('')
  const [error, setError] = useState('')
  const [connectionState, setConnectionState] = useState<ConnectionState>('connecting')
  const [pendingSend, setPendingSend] = useState<PendingSend | null>(null)

  // Mirrors of the state above for synchronous reads from socket callbacks and timers,
  // which must see the latest value rather than whatever render closed over them.
  const threadRef = useRef<ConversationThread | null>(null)
  const pendingSendRef = useRef<PendingSend | null>(null)
  const connectionStateRef = useRef<ConnectionState>('connecting')
  const userIdRef = useRef<string | undefined>(user?.id)
  const idRef = useRef<string | undefined>(id)

  const socketRef = useRef<WebSocket | null>(null)
  const reconnectTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const ackTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const reconnectAttemptRef = useRef(0)
  const disposedRef = useRef(false)
  const connectionVersionRef = useRef(0)
  // The ack timeout and the socket-close handler can both decide to fall back to HTTP for
  // the same send. Only the first attempt may run — a duplicate would complete later and
  // mutate send state that no longer belongs to it.
  const httpSendStartedForRef = useRef<string | null>(null)

  userIdRef.current = user?.id
  idRef.current = id

  function setThreadBoth(value: ConversationThread | null) {
    threadRef.current = value
    setThread(value)
  }

  function setPendingSendBoth(value: PendingSend | null) {
    pendingSendRef.current = value
    setPendingSend(value)
  }

  function setConnectionStateBoth(value: ConnectionState) {
    connectionStateRef.current = value
    setConnectionState(value)
  }

  function toMessage(m: conversationsApi.ConversationWebSocketMessage): ConversationMessage {
    return {
      id: m.id,
      senderDisplayName: m.senderDisplayName,
      body: m.body,
      sentAt: m.sentAt,
      isMine: m.senderId === userIdRef.current,
    }
  }

  // Unreachable in practice — the socket only connects after load() has stored the
  // snapshot — but kept for type narrowing.
  function mergeIntoThread(incoming: ConversationMessage[]) {
    if (!threadRef.current) return
    setThreadBoth({ ...threadRef.current, messages: mergeMessages(threadRef.current.messages, incoming) })
  }

  async function load(threadId: string, version: number) {
    try {
      const loadedThread = await conversationsApi.getThread(threadId)
      if (version !== connectionVersionRef.current || disposedRef.current) return

      if (threadRef.current) {
        mergeIntoThread(loadedThread.messages)
      } else {
        setThreadBoth(loadedThread)
      }
    } catch (err) {
      if (version === connectionVersionRef.current && !disposedRef.current) {
        setError(displayError(err))
      }
    }
  }

  function clearAckTimer() {
    if (ackTimerRef.current) {
      clearTimeout(ackTimerRef.current)
      ackTimerRef.current = null
    }
  }

  function closeSocket() {
    if (socketRef.current) {
      socketRef.current.onclose = null
      socketRef.current.close()
      socketRef.current = null
    }
  }

  function scheduleReconnect(threadId: string, version: number) {
    if (disposedRef.current || version !== connectionVersionRef.current || reconnectTimerRef.current) return

    if (reconnectAttemptRef.current >= MAX_RECONNECT_ATTEMPTS) {
      setConnectionStateBoth('offline')
      return
    }

    const delay = Math.min(1000 * 2 ** reconnectAttemptRef.current, 10000)
    reconnectAttemptRef.current += 1
    setConnectionStateBoth('reconnecting')
    reconnectTimerRef.current = setTimeout(() => {
      reconnectTimerRef.current = null
      // HTTP reload keeps the thread serviceable while the socket is down; merge-by-id
      // means a concurrent live delivery can't be clobbered or duplicated.
      void (async () => {
        await load(threadId, version)
        if (!disposedRef.current && version === connectionVersionRef.current) {
          connect(threadId, version)
        }
      })()
    }, delay)
  }

  function connect(threadId: string, version: number) {
    if (disposedRef.current || version !== connectionVersionRef.current) return

    closeSocket()
    setConnectionStateBoth(reconnectAttemptRef.current > 0 ? 'reconnecting' : 'connecting')
    const socket = conversationsApi.openSocket(threadId)
    socketRef.current = socket

    socket.onopen = () => {
      if (version !== connectionVersionRef.current) return
      reconnectAttemptRef.current = 0
      setConnectionStateBoth('connected')
      setError('')
    }

    socket.onmessage = (event) => {
      if (version !== connectionVersionRef.current) return

      try {
        const messageEvent = JSON.parse(event.data) as conversationsApi.ConversationWebSocketEvent

        if (messageEvent.type === 'connected') {
          // The server has registered this socket for broadcasts, so a snapshot fetched
          // now (merged by id with anything delivered meanwhile) cannot miss messages —
          // this closes the load/subscribe race on both initial connect and reconnect.
          void load(threadId, version)
          return
        }

        if (messageEvent.type === 'error') {
          setError(messageEvent.error ?? 'The server rejected the message.')
          if (pendingSendRef.current && messageEvent.clientMessageId === pendingSendRef.current.clientMessageId) {
            // The send definitively failed; keep the draft for the user to fix and retry.
            clearAckTimer()
            setPendingSendBoth(null)
          }
          return
        }

        if (!messageEvent.message) return

        if (messageEvent.type === 'ack') {
          mergeIntoThread([toMessage(messageEvent.message)])
          if (pendingSendRef.current && messageEvent.clientMessageId === pendingSendRef.current.clientMessageId) {
            clearAckTimer()
            setPendingSendBoth(null)
            setReplyBody('')
          }
          setError('')
          return
        }

        if (messageEvent.type === 'message') {
          mergeIntoThread([toMessage(messageEvent.message)])
          setError('')
        }
      } catch {
        setError('Received an invalid message from the server.')
      }
    }

    socket.onerror = () => {
      // Same guards as the other handlers: an obsolete socket (route change, unmount)
      // must not surface its failure in the currently selected thread.
      if (disposedRef.current || version !== connectionVersionRef.current) return
      setError('The live chat connection failed. Reconnecting…')
    }

    socket.onclose = (event) => {
      if (disposedRef.current || version !== connectionVersionRef.current) return

      if (event.code === SESSION_REVOKED_CLOSE_CODE) {
        // Reconnecting or falling back to HTTP would only repeat the 401. Stop here;
        // the draft stays in the textarea for after re-login.
        clearAckTimer()
        setPendingSendBoth(null)
        setConnectionStateBoth('offline')
        setError('Your session is no longer valid. Sign in again to continue.')
        return
      }

      if (pendingSendRef.current) {
        // Ambiguous outcome: the message may or may not have been persisted before the
        // drop. Retrying over HTTP with the same clientMessageId is safe either way.
        clearAckTimer()
        const { clientMessageId, body } = pendingSendRef.current
        void sendViaHttp(threadId, clientMessageId, body, version)
      }

      scheduleReconnect(threadId, version)
    }
  }

  async function sendViaHttp(threadId: string, clientMessageId: string, body: string, version: number) {
    if (httpSendStartedForRef.current === clientMessageId) return
    httpSendStartedForRef.current = clientMessageId

    setPendingSendBoth({ clientMessageId, body })
    try {
      const loadedThread = await conversationsApi.reply(threadId, body, clientMessageId)
      if (version !== connectionVersionRef.current || disposedRef.current) return
      mergeIntoThread(loadedThread.messages)
      // A successful round-trip proves the server is reachable again — restart the live
      // connection if the reconnect backoff had given up.
      if (connectionStateRef.current === 'offline') {
        reconnectAttemptRef.current = 0
        connect(threadId, version)
      }
      // A socket ack may have completed this send (and a newer send may have started)
      // while the request was in flight — only touch send state that is still ours.
      if (pendingSendRef.current?.clientMessageId === clientMessageId) {
        setReplyBody('')
        setError('')
      }
    } catch (err) {
      if (
        version === connectionVersionRef.current &&
        !disposedRef.current &&
        pendingSendRef.current?.clientMessageId === clientMessageId
      ) {
        setError(displayError(err))
      }
    } finally {
      if (
        version === connectionVersionRef.current &&
        !disposedRef.current &&
        pendingSendRef.current?.clientMessageId === clientMessageId
      ) {
        setPendingSendBoth(null)
      }
    }
  }

  function sendReply(e: FormEvent) {
    e.preventDefault()
    if (pendingSendRef.current) return
    setError('')

    const threadId = idRef.current!
    const version = connectionVersionRef.current
    const body = replyBody
    const clientMessageId = crypto.randomUUID()

    if (socketRef.current && socketRef.current.readyState === WebSocket.OPEN) {
      setPendingSendBoth({ clientMessageId, body })
      socketRef.current.send(JSON.stringify({ type: 'message', body, clientMessageId }))

      // No ack and no disconnect within the window — resolve the ambiguity by retrying
      // over HTTP with the same clientMessageId (idempotent server-side).
      clearAckTimer()
      ackTimerRef.current = setTimeout(() => {
        ackTimerRef.current = null
        if (
          pendingSendRef.current?.clientMessageId === clientMessageId &&
          !disposedRef.current &&
          version === connectionVersionRef.current
        ) {
          void sendViaHttp(threadId, clientMessageId, body, version)
        }
      }, ACK_TIMEOUT_MS)
    } else {
      // Live transport unavailable — fall back to the plain HTTP reply endpoint.
      void sendViaHttp(threadId, clientMessageId, body, version)
    }
  }

  // Leaves the offline state (exhausted backoff or a revoked session the user may since
  // have renewed) with a fresh reconnect budget. Wired to the Retry control and to the
  // browser's 'online' event — network restoration shouldn't wait on the user noticing.
  function retryConnection() {
    if (disposedRef.current || connectionStateRef.current !== 'offline') return
    reconnectAttemptRef.current = 0
    setError('')
    connect(idRef.current!, connectionVersionRef.current)
  }

  // Kept in a ref so the mount-only 'online' listener below always calls the latest
  // closure (current id, current connection state) instead of the one from first mount.
  const retryConnectionRef = useRef(retryConnection)
  retryConnectionRef.current = retryConnection

  useEffect(() => {
    if (!id) return

    disposedRef.current = false
    connectionVersionRef.current += 1
    const version = connectionVersionRef.current
    reconnectAttemptRef.current = 0
    setError('')
    setThreadBoth(null)
    setReplyBody('')
    setPendingSendBoth(null)
    clearAckTimer()
    closeSocket()
    if (reconnectTimerRef.current) {
      clearTimeout(reconnectTimerRef.current)
      reconnectTimerRef.current = null
    }

    void (async () => {
      await load(id, version)
      if (!disposedRef.current && version === connectionVersionRef.current && threadRef.current) {
        connect(id, version)
      }
    })()

    return () => {
      disposedRef.current = true
      if (reconnectTimerRef.current) clearTimeout(reconnectTimerRef.current)
      clearAckTimer()
      closeSocket()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id])

  useEffect(() => {
    const handler = () => retryConnectionRef.current()
    window.addEventListener('online', handler)
    return () => window.removeEventListener('online', handler)
  }, [])

  if (!thread) {
    return error ? <p className="error">{error}</p> : null
  }

  return (
    <>
      <h1>{thread.listingTitle}</h1>
      <Link to={`/listings/${thread.listingId}`}>View listing</Link>

      <p className="muted">
        {connectionState === 'connected' && <span>Live chat connected</span>}
        {connectionState === 'reconnecting' && <span>Reconnecting to live chat…</span>}
        {connectionState === 'offline' && (
          <span>
            Live chat is unavailable — replies still send, but new messages won&apos;t appear until it reconnects.
            <button type="button" className="retry" onClick={retryConnection}>
              Retry
            </button>
          </span>
        )}
        {connectionState === 'connecting' && <span>Connecting to live chat…</span>}
      </p>

      <div>
        {thread.messages.map((m) => (
          <div key={m.id} className={`message${m.isMine ? ' mine' : ''}`}>
            <div className="meta">
              {m.senderDisplayName} &middot; {new Date(m.sentAt).toLocaleString()}
            </div>
            <div>{m.body}</div>
          </div>
        ))}
      </div>

      {error && <p className="error">{error}</p>}
      <form onSubmit={sendReply}>
        <label>
          Reply
          <textarea
            value={replyBody}
            onChange={(e) => setReplyBody(e.target.value)}
            required
            disabled={pendingSend !== null}
          />
        </label>
        <button type="submit" disabled={pendingSend !== null}>
          {pendingSend ? 'Sending…' : 'Send'}
        </button>
      </form>
    </>
  )
}
