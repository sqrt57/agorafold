<script lang="ts">
  import { onMount } from 'svelte'
  import * as categoriesApi from '../api/categories'
  import * as listingsApi from '../api/listings'
  import type { Category, PagedListings } from '../api/types'
  import { navigate, route } from '../router'
  import ListingCard from '../components/ListingCard.svelte'
  import Pagination from '../components/Pagination.svelte'

  let categories: Category[] = []
  let result: PagedListings | null = null
  let loading = true
  let categoryId = ''
  let searchTerm = ''
  let loadedQuery: string | null = null

  onMount(() => {
    void categoriesApi.getAll().then((loadedCategories) => {
      categories = loadedCategories
    })
  })

  $: if ($route.path === '/') {
    const query = $route.query.toString()
    categoryId = $route.query.get('categoryId') ?? ''
    searchTerm = $route.query.get('searchTerm') ?? ''
    if (query !== loadedQuery) {
      loadedQuery = query
      loading = true
      void listingsApi.browse({
        categoryId: $route.query.get('categoryId') || undefined,
        searchTerm: $route.query.get('searchTerm') || undefined,
        page: Number($route.query.get('page') ?? 1),
      }).then((data) => {
        result = data
        loading = false
      })
    }
  }

  function applyFilters(event: SubmitEvent) {
    event.preventDefault()
    const query = new URLSearchParams()
    if (categoryId) query.set('categoryId', categoryId)
    if (searchTerm) query.set('searchTerm', searchTerm)
    navigate(`/${query.toString() ? `?${query.toString()}` : ''}`)
  }

  function goToPage(page: number) {
    const query = new URLSearchParams($route.query)
    query.set('page', String(page))
    navigate(`/?${query.toString()}`)
  }
</script>

<h1>Browse listings</h1>

<form class="filters" onsubmit={applyFilters}>
  <label>
    Category
    <select bind:value={categoryId}>
      <option value="">All categories</option>
      {#each categories as category}
        <option value={category.id}>{category.name}</option>
      {/each}
    </select>
  </label>
  <label>
    Search
    <input bind:value={searchTerm} type="text" placeholder="Search listings" />
  </label>
  <button type="submit">Apply</button>
</form>

{#if loading}
  <p>Loading...</p>
{:else if result}
  {#if result.items.length === 0}<p class="muted">No listings found.</p>{/if}
  <ul class="listing-grid">
    {#each result.items as listing (listing.id)}
      <li><ListingCard {listing} /></li>
    {/each}
  </ul>
  <Pagination
    page={result.page}
    totalPages={result.totalPages}
    hasPreviousPage={result.hasPreviousPage}
    hasNextPage={result.hasNextPage}
    onChange={goToPage}
  />
{/if}
