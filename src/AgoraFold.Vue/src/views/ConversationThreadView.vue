<script setup lang="ts">
import { ref } from 'vue'
import { useRoute } from 'vue-router'
import * as conversationsApi from '../api/conversations'
import { ApiError } from '../api/client'
import type { ConversationThread } from '../api/types'

const route = useRoute()
const thread = ref<ConversationThread | null>(null)
const replyBody = ref('')
const error = ref('')
const sending = ref(false)

async function load() {
  thread.value = await conversationsApi.getThread(route.params.id as string)
}

async function sendReply() {
  sending.value = true
  error.value = ''
  try {
    thread.value = await conversationsApi.reply(route.params.id as string, replyBody.value)
    replyBody.value = ''
  } catch (err) {
    error.value = err instanceof ApiError ? (err.errors[0] ?? err.message) : (err as Error).message
  } finally {
    sending.value = false
  }
}

load()
</script>

<template>
  <template v-if="thread">
    <h1>{{ thread.listingTitle }}</h1>
    <RouterLink :to="{ name: 'listing-detail', params: { id: thread.listingId } }">View listing</RouterLink>

    <div>
      <div v-for="(m, i) in thread.messages" :key="i" class="message" :class="{ mine: m.isMine }">
        <div class="meta">{{ m.senderDisplayName }} &middot; {{ new Date(m.sentAt).toLocaleString() }}</div>
        <div>{{ m.body }}</div>
      </div>
    </div>

    <p v-if="error" class="error">{{ error }}</p>
    <form @submit.prevent="sendReply">
      <label>
        Reply
        <textarea v-model="replyBody" required></textarea>
      </label>
      <button type="submit" :disabled="sending">Send</button>
    </form>
  </template>
</template>
