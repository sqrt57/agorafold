import { useEffect, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import * as listingsApi from '../api/listings'
import * as categoriesApi from '../api/categories'
import ListingCard from '../components/ListingCard'
import Pagination from '../components/Pagination'
import type { Category, PagedListings } from '../api/types'

export default function BrowsePage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [categories, setCategories] = useState<Category[]>([])
  const [result, setResult] = useState<PagedListings | null>(null)
  const [loading, setLoading] = useState(true)

  const [categoryId, setCategoryId] = useState(searchParams.get('categoryId') ?? '')
  const [searchTerm, setSearchTerm] = useState(searchParams.get('searchTerm') ?? '')

  useEffect(() => {
    categoriesApi.getAll().then(setCategories)
  }, [])

  useEffect(() => {
    setLoading(true)
    const page = Number(searchParams.get('page') ?? 1)
    listingsApi
      .browse({
        categoryId: searchParams.get('categoryId') || undefined,
        searchTerm: searchParams.get('searchTerm') || undefined,
        page,
      })
      .then((data) => {
        setResult(data)
        setLoading(false)
      })
  }, [searchParams])

  function applyFilters(e: FormEvent) {
    e.preventDefault()
    const next = new URLSearchParams()
    if (categoryId) next.set('categoryId', categoryId)
    if (searchTerm) next.set('searchTerm', searchTerm)
    setSearchParams(next)
  }

  function goToPage(page: number) {
    const next = new URLSearchParams(searchParams)
    next.set('page', String(page))
    setSearchParams(next)
  }

  return (
    <>
      <h1>Browse listings</h1>

      <form className="filters" onSubmit={applyFilters}>
        <label>
          Category
          <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
            <option value="">All categories</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        </label>
        <label>
          Search
          <input
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            type="text"
            placeholder="Search listings"
          />
        </label>
        <button type="submit">Apply</button>
      </form>

      {loading ? (
        <p>Loading...</p>
      ) : result ? (
        <>
          {result.items.length === 0 && <p className="muted">No listings found.</p>}
          <ul className="listing-grid">
            {result.items.map((listing) => (
              <li key={listing.id}>
                <ListingCard listing={listing} />
              </li>
            ))}
          </ul>
          <Pagination
            page={result.page}
            totalPages={result.totalPages}
            hasPreviousPage={result.hasPreviousPage}
            hasNextPage={result.hasNextPage}
            onChange={goToPage}
          />
        </>
      ) : null}
    </>
  )
}
