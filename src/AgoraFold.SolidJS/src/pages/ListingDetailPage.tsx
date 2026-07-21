import { createEffect, createSignal, For, Show } from 'solid-js'
import { A, useNavigate, useParams } from '@solidjs/router'
import * as listingsApi from '../api/listings'
import * as conversationsApi from '../api/conversations'
import { imageUrl } from '../api/client'
import type { ListingDetail } from '../api/types'

export default function ListingDetailPage() {
  const params = useParams<{ id: string }>()
  const navigate = useNavigate()

  const [listing, setListing] = createSignal<ListingDetail | null>(null)
  const [messaging, setMessaging] = createSignal(false)
  const [error, setError] = createSignal('')

  createEffect(() => {
    listingsApi.getDetail(params.id).then(setListing)
  })

  async function deleteListing() {
    const current = listing()
    if (!current) return
    if (!confirm(`Delete "${current.title}"? This cannot be undone.`)) return
    await listingsApi.remove(current.id)
    navigate('/listings/mine')
  }

  async function messageSeller() {
    const current = listing()
    if (!current) return
    setMessaging(true)
    setError('')
    try {
      const thread = await conversationsApi.start(current.id)
      navigate(`/conversations/${thread.id}`)
    } catch (err) {
      setError((err as Error).message)
    } finally {
      setMessaging(false)
    }
  }

  return (
    <Show when={listing()}>
      {(listing) => (
        <>
          <h1>{listing().title}</h1>
          <p class="muted">
            {listing().categoryName} &middot; listed by {listing().ownerDisplayName}
          </p>
          <Show when={listing().price != null}>
            <p>
              <strong>${listing().price}</strong>
            </p>
          </Show>

          <Show when={listing().images.length > 0}>
            <ul class="image-gallery">
              <For each={listing().images}>
                {(image) => (
                  <li>
                    <img src={imageUrl(image.url)} alt={listing().title} />
                  </li>
                )}
              </For>
            </ul>
          </Show>

          <p>{listing().description}</p>

          <Show when={error()}>
            <p class="error">{error()}</p>
          </Show>

          <Show
            when={listing().isOwner}
            fallback={
              <Show when={listing().canMessage}>
                <button onClick={messageSeller} disabled={messaging()}>
                  Message seller
                </button>
              </Show>
            }
          >
            <div>
              <A href={`/listings/${listing().id}/edit`}>Edit listing</A> &middot;{' '}
              <button class="danger" onClick={deleteListing}>
                Delete listing
              </button>
            </div>
          </Show>
        </>
      )}
    </Show>
  )
}
