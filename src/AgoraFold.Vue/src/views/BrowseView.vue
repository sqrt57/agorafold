<script setup>
import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import * as listingsApi from '../api/listings'
import * as categoriesApi from '../api/categories'
import ListingCard from '../components/ListingCard.vue'
import Pagination from '../components/Pagination.vue'

const route = useRoute()
const router = useRouter()

const categories = ref([])
const result = ref(null)
const loading = ref(true)

const categoryId = ref(route.query.categoryId ?? '')
const searchTerm = ref(route.query.searchTerm ?? '')

async function load() {
  loading.value = true
  const page = Number(route.query.page ?? 1)
  result.value = await listingsApi.browse({
    categoryId: route.query.categoryId || undefined,
    searchTerm: route.query.searchTerm || undefined,
    page,
  })
  loading.value = false
}

function applyFilters() {
  router.push({
    query: {
      ...(categoryId.value ? { categoryId: categoryId.value } : {}),
      ...(searchTerm.value ? { searchTerm: searchTerm.value } : {}),
    },
  })
}

function goToPage(page) {
  router.push({ query: { ...route.query, page } })
}

categoriesApi.getAll().then((data) => (categories.value = data))
watch(() => route.query, load, { immediate: true })
</script>

<template>
  <h1>Browse listings</h1>

  <form class="filters" @submit.prevent="applyFilters">
    <label>
      Category
      <select v-model="categoryId">
        <option value="">All categories</option>
        <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
    </label>
    <label>
      Search
      <input v-model="searchTerm" type="text" placeholder="Search listings" />
    </label>
    <button type="submit">Apply</button>
  </form>

  <p v-if="loading">Loading...</p>
  <template v-else-if="result">
    <p v-if="result.items.length === 0" class="muted">No listings found.</p>
    <ul class="listing-grid">
      <li v-for="listing in result.items" :key="listing.id">
        <ListingCard :listing="listing" />
      </li>
    </ul>
    <Pagination
      :page="result.page"
      :total-pages="result.totalPages"
      :has-previous-page="result.hasPreviousPage"
      :has-next-page="result.hasNextPage"
      @change="goToPage"
    />
  </template>
</template>
