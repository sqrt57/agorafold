import { createEffect, createSignal, For, Show } from 'solid-js'
import { A, useParams } from '@solidjs/router'
import * as conversationsApi from '../api/conversations'
import { ApiError } from '../api/client'
import type { ConversationThread } from '../api/types'

export default function ConversationThreadPage() {
  const params = useParams<{ id: string }>()

  const [thread, setThread] = createSignal<ConversationThread | null>(null)
  const [replyBody, setReplyBody] = createSignal('')
  const [error, setError] = createSignal('')
  const [sending, setSending] = createSignal(false)

  createEffect(() => {
    conversationsApi.getThread(params.id).then(setThread)
  })

  async function sendReply(e: SubmitEvent) {
    e.preventDefault()
    setSending(true)
    setError('')
    try {
      setThread(await conversationsApi.reply(params.id, replyBody()))
      setReplyBody('')
    } catch (err) {
      setError(err instanceof ApiError ? (err.errors[0] ?? err.message) : (err as Error).message)
    } finally {
      setSending(false)
    }
  }

  return (
    <Show when={thread()}>
      {(thread) => (
        <>
          <h1>{thread().listingTitle}</h1>
          <A href={`/listings/${thread().listingId}`}>View listing</A>

          <div>
            <For each={thread().messages}>
              {(m) => (
                <div class={`message${m.isMine ? ' mine' : ''}`}>
                  <div class="meta">
                    {m.senderDisplayName} &middot; {new Date(m.sentAt).toLocaleString()}
                  </div>
                  <div>{m.body}</div>
                </div>
              )}
            </For>
          </div>

          <Show when={error()}>
            <p class="error">{error()}</p>
          </Show>
          <form onSubmit={sendReply}>
            <label>
              Reply
              <textarea value={replyBody()} onInput={(e) => setReplyBody(e.currentTarget.value)} required />
            </label>
            <button type="submit" disabled={sending()}>
              Send
            </button>
          </form>
        </>
      )}
    </Show>
  )
}
