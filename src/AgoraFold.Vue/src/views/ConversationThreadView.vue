<script setup>
import { ref } from 'vue'
import { useRoute } from 'vue-router'
import * as conversationsApi from '../api/conversations'

const route = useRoute()
const thread = ref(null)
const replyBody = ref('')
const error = ref('')
const sending = ref(false)

async function load() {
  thread.value = await conversationsApi.getThread(route.params.id)
}

async function sendReply() {
  sending.value = true
  error.value = ''
  try {
    thread.value = await conversationsApi.reply(route.params.id, replyBody.value)
    replyBody.value = ''
  } catch (err) {
    error.value = err.errors?.[0] ?? err.message
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
