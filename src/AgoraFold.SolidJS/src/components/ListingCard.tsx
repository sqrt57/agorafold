import { A } from '@solidjs/router'
import { Show } from 'solid-js'
import { imageUrl } from '../api/client'
import type { ListingSummary } from '../api/types'

export default function ListingCard(props: { listing: ListingSummary }) {
  return (
    <A class="listing-card" href={`/listings/${props.listing.id}`}>
      <Show when={props.listing.thumbnailUrl} fallback={<div class="thumbnail-placeholder" />}>
        <img src={imageUrl(props.listing.thumbnailUrl)} alt={props.listing.title} />
      </Show>
      <div class="body">
        <strong>{props.listing.title}</strong>
        <div class="muted">{props.listing.categoryName}</div>
        <Show when={props.listing.price != null}>
          <div>${props.listing.price}</div>
        </Show>
      </div>
    </A>
  )
}
