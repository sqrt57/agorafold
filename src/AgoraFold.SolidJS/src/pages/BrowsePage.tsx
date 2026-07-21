import { createEffect, createSignal, For, Show, onMount } from 'solid-js'
import { useSearchParams } from '@solidjs/router'
import * as listingsApi from '../api/listings'
import * as categoriesApi from '../api/categories'
import ListingCard from '../components/ListingCard'
import Pagination from '../components/Pagination'
import type { Category, PagedListings } from '../api/types'

function firstParam(value: string | string[] | undefined): string {
  return (Array.isArray(value) ? value[0] : value) ?? ''
}

export default function BrowsePage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [categories, setCategories] = createSignal<Category[]>([])
  const [result, setResult] = createSignal<PagedListings | null>(null)
  const [loading, setLoading] = createSignal(true)

  const [categoryId, setCategoryId] = createSignal(firstParam(searchParams.categoryId))
  const [searchTerm, setSearchTerm] = createSignal(firstParam(searchParams.searchTerm))

  onMount(() => {
    categoriesApi.getAll().then(setCategories)
  })

  createEffect(() => {
    const page = Number(firstParam(searchParams.page) || 1)
    const categoryIdParam = firstParam(searchParams.categoryId)
    const searchTermParam = firstParam(searchParams.searchTerm)

    setLoading(true)
    listingsApi
      .browse({ categoryId: categoryIdParam || undefined, searchTerm: searchTermParam || undefined, page })
      .then((data) => {
        setResult(data)
        setLoading(false)
      })
  })

  function applyFilters(e: SubmitEvent) {
    e.preventDefault()
    setSearchParams({ categoryId: categoryId() || undefined, searchTerm: searchTerm() || undefined, page: undefined })
  }

  function goToPage(page: number) {
    setSearchParams({ page: String(page) })
  }

  return (
    <>
      <h1>Browse listings</h1>

      <form class="filters" onSubmit={applyFilters}>
        <label>
          Category
          <select value={categoryId()} onChange={(e) => setCategoryId(e.currentTarget.value)}>
            <option value="">All categories</option>
            <For each={categories()}>{(c) => <option value={c.id}>{c.name}</option>}</For>
          </select>
        </label>
        <label>
          Search
          <input
            value={searchTerm()}
            onInput={(e) => setSearchTerm(e.currentTarget.value)}
            type="text"
            placeholder="Search listings"
          />
        </label>
        <button type="submit">Apply</button>
      </form>

      <Show when={!loading()} fallback={<p>Loading...</p>}>
        <Show when={result()}>
          {(result) => (
            <>
              <Show when={result().items.length === 0}>
                <p class="muted">No listings found.</p>
              </Show>
              <ul class="listing-grid">
                <For each={result().items}>{(listing) => <li><ListingCard listing={listing} /></li>}</For>
              </ul>
              <Pagination
                page={result().page}
                totalPages={result().totalPages}
                hasPreviousPage={result().hasPreviousPage}
                hasNextPage={result().hasNextPage}
                onChange={goToPage}
              />
            </>
          )}
        </Show>
      </Show>
    </>
  )
}
