import { Link } from 'react-router-dom'
import { imageUrl } from '../api/client'
import type { ListingSummary } from '../api/types'

export default function ListingCard({ listing }: { listing: ListingSummary }) {
  return (
    <Link className="listing-card" to={`/listings/${listing.id}`}>
      {listing.thumbnailUrl ? (
        <img src={imageUrl(listing.thumbnailUrl)} alt={listing.title} />
      ) : (
        <div className="thumbnail-placeholder" />
      )}
      <div className="body">
        <strong>{listing.title}</strong>
        <div className="muted">{listing.categoryName}</div>
        {listing.price != null && <div>${listing.price}</div>}
      </div>
    </Link>
  )
}
