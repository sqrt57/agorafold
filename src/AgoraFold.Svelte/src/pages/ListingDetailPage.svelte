<script lang="ts">
  import * as conversationsApi from '../api/conversations'
  import * as listingsApi from '../api/listings'
  import { ApiError, imageUrl } from '../api/client'
  import type { ListingDetail } from '../api/types'
  import Link from '../components/Link.svelte'
  import { navigate, route } from '../router'

  let listing: ListingDetail | null = null
  let messaging = false
  let error = ''
  let loadedId = ''

  $: id = $route.params.id
  $: if (id && id !== loadedId) {
    loadedId = id
    listing = null
    error = ''
    void listingsApi.getDetail(id).then((loadedListing) => {
      listing = loadedListing
    })
  }

  async function deleteListing() {
    if (!listing || !confirm(`Delete "${listing.title}"? This cannot be undone.`)) return
    await listingsApi.remove(listing.id)
    navigate('/listings/mine')
  }

  async function messageSeller() {
    if (!listing) return
    messaging = true
    error = ''
    try {
      const thread = await conversationsApi.start(listing.id)
      navigate(`/conversations/${thread.id}`)
    } catch (exception) {
      error = exception instanceof ApiError ? (exception.errors[0] ?? exception.message) : (exception as Error).message
    } finally {
      messaging = false
    }
  }
</script>

{#if listing}
  <h1>{listing.title}</h1>
  <p class="muted">{listing.categoryName} &middot; listed by {listing.ownerDisplayName}</p>
  {#if listing.price != null}<p><strong>${listing.price}</strong></p>{/if}

  {#if listing.images.length > 0}
    <ul class="image-gallery">
      {#each listing.images as image (image.id)}
        <li><img src={imageUrl(image.url)} alt={listing.title} /></li>
      {/each}
    </ul>
  {/if}

  <p>{listing.description}</p>
  {#if error}<p class="error">{error}</p>{/if}

  {#if listing.isOwner}
    <div>
      <Link href={`/listings/${listing.id}/edit`}>Edit listing</Link> &middot;
      <button class="danger" onclick={deleteListing}>Delete listing</button>
    </div>
  {:else if listing.canMessage}
    <button onclick={messageSeller} disabled={messaging}>Message seller</button>
  {/if}
{/if}
