<script lang="ts">
  import { onMount } from 'svelte'
  import * as listingsApi from '../api/listings'
  import type { ListingSummary } from '../api/types'
  import Link from '../components/Link.svelte'
  import ListingCard from '../components/ListingCard.svelte'

  let listings: ListingSummary[] = []

  onMount(() => {
    void listingsApi.getMine().then((loadedListings) => {
      listings = loadedListings
    })
  })
</script>

<h1>My listings</h1>
<Link href="/listings/new">Post a new listing</Link>

{#if listings.length === 0}<p class="muted">You haven't posted any listings yet.</p>{/if}
<ul class="listing-grid">
  {#each listings as listing (listing.id)}
    <li><ListingCard {listing} /></li>
  {/each}
</ul>
