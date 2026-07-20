<script setup>
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const email = ref('')
const password = ref('')
const rememberMe = ref(false)
const error = ref('')
const submitting = ref(false)

async function submit() {
  submitting.value = true
  error.value = ''
  try {
    await auth.login(email.value, password.value, rememberMe.value)
    router.push(route.query.returnUrl || { name: 'browse' })
  } catch (err) {
    error.value = err.message
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <h1>Log in</h1>
  <p v-if="error" class="error">{{ error }}</p>
  <form @submit.prevent="submit">
    <label>
      Email
      <input v-model="email" type="email" required autocomplete="username" />
    </label>
    <label>
      Password
      <input v-model="password" type="password" required autocomplete="current-password" />
    </label>
    <label style="flex-direction: row; align-items: center; gap: 8px;">
      <input v-model="rememberMe" type="checkbox" style="width: auto;" />
      Remember me
    </label>
    <button type="submit" :disabled="submitting">Log in</button>
  </form>
</template>
