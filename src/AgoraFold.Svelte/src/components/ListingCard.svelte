<script lang="ts">
  import { imageUrl } from '../api/client'
  import type { ListingSummary } from '../api/types'
  import Link from './Link.svelte'

  export let listing: ListingSummary
</script>

<Link href={`/listings/${listing.id}`} className="listing-card">
  {#if listing.thumbnailUrl}
    <img src={imageUrl(listing.thumbnailUrl)} alt={listing.title} />
  {:else}
    <div class="thumbnail-placeholder"></div>
  {/if}
  <div class="body">
    <strong>{listing.title}</strong>
    <div class="muted">{listing.categoryName}</div>
    {#if listing.price != null}<div>${listing.price}</div>{/if}
  </div>
</Link>
