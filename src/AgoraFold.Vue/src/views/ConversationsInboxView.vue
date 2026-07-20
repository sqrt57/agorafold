<script setup lang="ts">
import { ref } from 'vue'
import * as conversationsApi from '../api/conversations'
import type { ConversationSummary } from '../api/types'

const conversations = ref<ConversationSummary[]>([])

conversationsApi.getInbox().then((data) => (conversations.value = data))
</script>

<template>
  <h1>Messages</h1>
  <p v-if="conversations.length === 0" class="muted">No conversations yet.</p>
  <ul class="conversation-list">
    <li v-for="c in conversations" :key="c.id">
      <RouterLink :to="{ name: 'conversation-thread', params: { id: c.id } }">
        <strong>{{ c.listingTitle }}</strong> &middot; {{ c.otherPartyDisplayName }}
      </RouterLink>
      <p class="muted">{{ c.lastMessageIsMine ? 'You: ' : '' }}{{ c.lastMessageBody }}</p>
    </li>
  </ul>
</template>
