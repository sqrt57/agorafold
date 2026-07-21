import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import * as listingsApi from '../api/listings'
import * as conversationsApi from '../api/conversations'
import { imageUrl } from '../api/client'
import type { ListingDetail } from '../api/types'

export default function ListingDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const [listing, setListing] = useState<ListingDetail | null>(null)
  const [messaging, setMessaging] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    listingsApi.getDetail(id!).then(setListing)
  }, [id])

  async function deleteListing() {
    if (!listing) return
    if (!confirm(`Delete "${listing.title}"? This cannot be undone.`)) return
    await listingsApi.remove(listing.id)
    navigate('/listings/mine')
  }

  async function messageSeller() {
    if (!listing) return
    setMessaging(true)
    setError('')
    try {
      const thread = await conversationsApi.start(listing.id)
      navigate(`/conversations/${thread.id}`)
    } catch (err) {
      setError((err as Error).message)
    } finally {
      setMessaging(false)
    }
  }

  if (!listing) return null

  return (
    <>
      <h1>{listing.title}</h1>
      <p className="muted">
        {listing.categoryName} &middot; listed by {listing.ownerDisplayName}
      </p>
      {listing.price != null && (
        <p>
          <strong>${listing.price}</strong>
        </p>
      )}

      {listing.images.length > 0 && (
        <ul className="image-gallery">
          {listing.images.map((image) => (
            <li key={image.id}>
              <img src={imageUrl(image.url)} alt={listing.title} />
            </li>
          ))}
        </ul>
      )}

      <p>{listing.description}</p>

      {error && <p className="error">{error}</p>}

      {listing.isOwner ? (
        <div>
          <Link to={`/listings/${listing.id}/edit`}>Edit listing</Link> &middot;{' '}
          <button className="danger" onClick={deleteListing}>
            Delete listing
          </button>
        </div>
      ) : (
        listing.canMessage && (
          <button onClick={messageSeller} disabled={messaging}>
            Message seller
          </button>
        )
      )}
    </>
  )
}
