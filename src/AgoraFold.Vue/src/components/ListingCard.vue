<script setup>
import { imageUrl } from '../api/client'

defineProps({
  listing: { type: Object, required: true },
})
</script>

<template>
  <RouterLink class="listing-card" :to="{ name: 'listing-detail', params: { id: listing.id } }">
    <img v-if="listing.thumbnailUrl" :src="imageUrl(listing.thumbnailUrl)" :alt="listing.title" />
    <div v-else class="thumbnail-placeholder" />
    <div class="body">
      <strong>{{ listing.title }}</strong>
      <div class="muted">{{ listing.categoryName }}</div>
      <div v-if="listing.price != null">${{ listing.price }}</div>
    </div>
  </RouterLink>
</template>
