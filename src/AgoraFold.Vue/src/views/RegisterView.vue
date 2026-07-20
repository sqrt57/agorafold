<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { ApiError } from '../api/client'

const router = useRouter()
const auth = useAuthStore()

const email = ref('')
const displayName = ref('')
const password = ref('')
const errors = ref<string[]>([])
const submitting = ref(false)

async function submit() {
  submitting.value = true
  errors.value = []
  try {
    await auth.register(email.value, displayName.value, password.value)
    router.push({ name: 'browse' })
  } catch (err) {
    errors.value = err instanceof ApiError && err.errors.length ? err.errors : [(err as Error).message]
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <h1>Register</h1>
  <ul v-if="errors.length" class="error-list">
    <li v-for="(e, i) in errors" :key="i">{{ e }}</li>
  </ul>
  <form @submit.prevent="submit">
    <label>
      Email
      <input v-model="email" type="email" required autocomplete="username" />
    </label>
    <label>
      Display name
      <input v-model="displayName" type="text" required />
    </label>
    <label>
      Password
      <input v-model="password" type="password" required autocomplete="new-password" />
    </label>
    <button type="submit" :disabled="submitting">Register</button>
  </form>
</template>
