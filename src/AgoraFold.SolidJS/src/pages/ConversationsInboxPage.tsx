import { createSignal, For, Show, onMount } from 'solid-js'
import { A } from '@solidjs/router'
import * as conversationsApi from '../api/conversations'
import type { ConversationSummary } from '../api/types'

export default function ConversationsInboxPage() {
  const [conversations, setConversations] = createSignal<ConversationSummary[]>([])

  onMount(() => {
    conversationsApi.getInbox().then(setConversations)
  })

  return (
    <>
      <h1>Messages</h1>
      <Show when={conversations().length === 0}>
        <p class="muted">No conversations yet.</p>
      </Show>
      <ul class="conversation-list">
        <For each={conversations()}>
          {(c) => (
            <li>
              <A href={`/conversations/${c.id}`}>
                <strong>{c.listingTitle}</strong> &middot; {c.otherPartyDisplayName}
              </A>
              <p class="muted">
                {c.lastMessageIsMine ? 'You: ' : ''}
                {c.lastMessageBody}
              </p>
            </li>
          )}
        </For>
      </ul>
    </>
  )
}
