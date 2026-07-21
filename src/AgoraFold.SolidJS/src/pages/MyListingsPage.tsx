import { createSignal, For, Show, onMount } from 'solid-js'
import { A } from '@solidjs/router'
import * as listingsApi from '../api/listings'
import ListingCard from '../components/ListingCard'
import type { ListingSummary } from '../api/types'

export default function MyListingsPage() {
  const [listings, setListings] = createSignal<ListingSummary[]>([])

  onMount(() => {
    listingsApi.getMine().then(setListings)
  })

  return (
    <>
      <h1>My listings</h1>
      <A href="/listings/new">Post a new listing</A>

      <Show when={listings().length === 0}>
        <p class="muted">You haven't posted any listings yet.</p>
      </Show>
      <ul class="listing-grid">
        <For each={listings()}>{(listing) => <li><ListingCard listing={listing} /></li>}</For>
      </ul>
    </>
  )
}
