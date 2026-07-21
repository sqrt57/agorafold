<script lang="ts">
  import { onMount } from 'svelte'
  import { hydrated, hydrate, user } from './auth'
  import { navigate, route } from './router'
  import NavBar from './components/NavBar.svelte'
  import BrowsePage from './pages/BrowsePage.svelte'
  import LoginPage from './pages/LoginPage.svelte'
  import RegisterPage from './pages/RegisterPage.svelte'
  import ListingFormPage from './pages/ListingFormPage.svelte'
  import ListingDetailPage from './pages/ListingDetailPage.svelte'
  import MyListingsPage from './pages/MyListingsPage.svelte'
  import ConversationsInboxPage from './pages/ConversationsInboxPage.svelte'
  import ConversationThreadPage from './pages/ConversationThreadPage.svelte'

  const protectedPaths = ['/listings/new', '/listings/mine', '/conversations']

  onMount(() => {
    void hydrate()
  })

  $: isProtected = protectedPaths.includes($route.path) || /^\/listings\/\d+\/edit$/.test($route.path) || /^\/conversations\/\d+$/.test($route.path)
  $: if ($hydrated && !$user && isProtected) {
    const returnUrl = `${$route.path}${$route.query.toString() ? `?${$route.query.toString()}` : ''}`
    navigate(`/login?returnUrl=${encodeURIComponent(returnUrl)}`, true)
  }

  function isListingEdit() {
    return /^\/listings\/\d+\/edit$/.test($route.path)
  }
</script>

<NavBar />
<main class="container">
  {#if !$hydrated}
    <p>Loading...</p>
  {:else if isProtected && !$user}
    <p>Redirecting to log in...</p>
  {:else if $route.path === '/'}
    <BrowsePage />
  {:else if $route.path === '/login'}
    <LoginPage />
  {:else if $route.path === '/register'}
    <RegisterPage />
  {:else if $route.path === '/listings/new' || isListingEdit()}
    <ListingFormPage />
  {:else if /^\/listings\/\d+$/.test($route.path)}
    <ListingDetailPage />
  {:else if $route.path === '/listings/mine'}
    <MyListingsPage />
  {:else if $route.path === '/conversations'}
    <ConversationsInboxPage />
  {:else if /^\/conversations\/\d+$/.test($route.path)}
    <ConversationThreadPage />
  {:else}
    <BrowsePage />
  {/if}
</main>
