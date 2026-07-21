<script lang="ts">
  import * as conversationsApi from '../api/conversations'
  import { ApiError } from '../api/client'
  import type { ConversationThread } from '../api/types'
  import Link from '../components/Link.svelte'
  import { route } from '../router'

  let thread: ConversationThread | null = null
  let replyBody = ''
  let error = ''
  let sending = false
  let loadedId = ''

  $: id = $route.params.id
  $: if (id && id !== loadedId) {
    loadedId = id
    thread = null
    error = ''
    void conversationsApi.getThread(id).then((loadedThread) => {
      thread = loadedThread
    })
  }

  async function sendReply(event: SubmitEvent) {
    event.preventDefault()
    if (!thread) return
    sending = true
    error = ''
    try {
      thread = await conversationsApi.reply(thread.id, replyBody)
      replyBody = ''
    } catch (exception) {
      error = exception instanceof ApiError ? (exception.errors[0] ?? exception.message) : (exception as Error).message
    } finally {
      sending = false
    }
  }
</script>

{#if thread}
  <h1>{thread.listingTitle}</h1>
  <Link href={`/listings/${thread.listingId}`}>View listing</Link>

  <div>
    {#each thread.messages as message, index (index)}
      <div class:mine={message.isMine} class="message">
        <div class="meta">{message.senderDisplayName} &middot; {new Date(message.sentAt).toLocaleString()}</div>
        <div>{message.body}</div>
      </div>
    {/each}
  </div>

  {#if error}<p class="error">{error}</p>{/if}
  <form onsubmit={sendReply}>
    <label>
      Reply
      <textarea bind:value={replyBody} required></textarea>
    </label>
    <button type="submit" disabled={sending}>Send</button>
  </form>
{/if}
