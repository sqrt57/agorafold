<script setup lang="ts">
import { ref } from 'vue'
import * as listingsApi from '../api/listings'
import ListingCard from '../components/ListingCard.vue'
import type { ListingSummary } from '../api/types'

const listings = ref<ListingSummary[]>([])

listingsApi.getMine().then((data) => (listings.value = data))
</script>

<template>
  <h1>My listings</h1>
  <RouterLink :to="{ name: 'listing-new' }">Post a new listing</RouterLink>

  <p v-if="listings.length === 0" class="muted">You haven't posted any listings yet.</p>
  <ul class="listing-grid">
    <li v-for="listing in listings" :key="listing.id">
      <ListingCard :listing="listing" />
    </li>
  </ul>
</template>
