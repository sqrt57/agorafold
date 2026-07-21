import { useEffect, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import * as conversationsApi from '../api/conversations'
import { ApiError } from '../api/client'
import type { ConversationThread } from '../api/types'

export default function ConversationThreadPage() {
  const { id } = useParams<{ id: string }>()

  const [thread, setThread] = useState<ConversationThread | null>(null)
  const [replyBody, setReplyBody] = useState('')
  const [error, setError] = useState('')
  const [sending, setSending] = useState(false)

  useEffect(() => {
    conversationsApi.getThread(id!).then(setThread)
  }, [id])

  async function sendReply(e: FormEvent) {
    e.preventDefault()
    setSending(true)
    setError('')
    try {
      setThread(await conversationsApi.reply(id!, replyBody))
      setReplyBody('')
    } catch (err) {
      setError(err instanceof ApiError ? (err.errors[0] ?? err.message) : (err as Error).message)
    } finally {
      setSending(false)
    }
  }

  if (!thread) return null

  return (
    <>
      <h1>{thread.listingTitle}</h1>
      <Link to={`/listings/${thread.listingId}`}>View listing</Link>

      <div>
        {thread.messages.map((m, i) => (
          <div key={i} className={`message${m.isMine ? ' mine' : ''}`}>
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
          <textarea value={replyBody} onChange={(e) => setReplyBody(e.target.value)} required />
        </label>
        <button type="submit" disabled={sending}>
          Send
        </button>
      </form>
    </>
  )
}
