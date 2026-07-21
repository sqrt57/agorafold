<script lang="ts">
  import { onMount } from 'svelte'
  import * as conversationsApi from '../api/conversations'
  import type { ConversationSummary } from '../api/types'
  import Link from '../components/Link.svelte'

  let conversations: ConversationSummary[] = []

  onMount(() => {
    void conversationsApi.getInbox().then((loadedConversations) => {
      conversations = loadedConversations
    })
  })
</script>

<h1>Messages</h1>
{#if conversations.length === 0}<p class="muted">No conversations yet.</p>{/if}
<ul class="conversation-list">
  {#each conversations as conversation (conversation.id)}
    <li>
      <Link href={`/conversations/${conversation.id}`}>
        <strong>{conversation.listingTitle}</strong> &middot; {conversation.otherPartyDisplayName}
      </Link>
      <p class="muted">{conversation.lastMessageIsMine ? 'You: ' : ''}{conversation.lastMessageBody}</p>
    </li>
  {/each}
</ul>
