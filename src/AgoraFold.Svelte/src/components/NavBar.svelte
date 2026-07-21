<script lang="ts">
  import { navigate } from '../router'
  import { logout, user } from '../auth'
  import Link from './Link.svelte'

  let loggingOut = false

  async function handleLogout() {
    loggingOut = true
    try {
      await logout()
      navigate('/')
    } finally {
      loggingOut = false
    }
  }
</script>

<nav class="navbar">
  <Link href="/" className="brand">AgoraFold</Link>
  <div class="links">
    <Link href="/">Browse</Link>
    {#if $user}
      <Link href="/listings/new">Post a listing</Link>
      <Link href="/listings/mine">My listings</Link>
      <Link href="/conversations">Messages</Link>
      <span class="muted">{$user.displayName}</span>
      <button class="secondary" disabled={loggingOut} onclick={handleLogout}>Log out</button>
    {:else}
      <Link href="/login">Log in</Link>
      <Link href="/register">Register</Link>
    {/if}
  </div>
</nav>
