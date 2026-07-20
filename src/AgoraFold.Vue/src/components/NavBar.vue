<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()

async function handleLogout() {
  await auth.logout()
  router.push({ name: 'browse' })
}
</script>

<template>
  <nav class="navbar">
    <RouterLink class="brand" :to="{ name: 'browse' }">AgoraFold</RouterLink>
    <div class="links">
      <RouterLink :to="{ name: 'browse' }">Browse</RouterLink>
      <template v-if="auth.isAuthenticated">
        <RouterLink :to="{ name: 'listing-new' }">Post a listing</RouterLink>
        <RouterLink :to="{ name: 'listing-mine' }">My listings</RouterLink>
        <RouterLink :to="{ name: 'conversations-inbox' }">Messages</RouterLink>
        <span class="muted">{{ auth.user?.displayName }}</span>
        <button class="secondary" @click="handleLogout">Log out</button>
      </template>
      <template v-else>
        <RouterLink :to="{ name: 'login' }">Log in</RouterLink>
        <RouterLink :to="{ name: 'register' }">Register</RouterLink>
      </template>
    </div>
  </nav>
</template>
