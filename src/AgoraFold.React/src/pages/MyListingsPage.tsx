import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import * as listingsApi from '../api/listings'
import ListingCard from '../components/ListingCard'
import type { ListingSummary } from '../api/types'

export default function MyListingsPage() {
  const [listings, setListings] = useState<ListingSummary[]>([])

  useEffect(() => {
    listingsApi.getMine().then(setListings)
  }, [])

  return (
    <>
      <h1>My listings</h1>
      <Link to="/listings/new">Post a new listing</Link>

      {listings.length === 0 && <p className="muted">You haven't posted any listings yet.</p>}
      <ul className="listing-grid">
        {listings.map((listing) => (
          <li key={listing.id}>
            <ListingCard listing={listing} />
          </li>
        ))}
      </ul>
    </>
  )
}
