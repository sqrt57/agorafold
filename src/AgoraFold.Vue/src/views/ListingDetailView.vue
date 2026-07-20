<script setup>
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import * as listingsApi from '../api/listings'
import * as conversationsApi from '../api/conversations'
import { imageUrl } from '../api/client'

const route = useRoute()
const router = useRouter()

const listing = ref(null)
const messaging = ref(false)
const error = ref('')

async function load() {
  listing.value = await listingsApi.getDetail(route.params.id)
}

async function deleteListing() {
  if (!confirm(`Delete "${listing.value.title}"? This cannot be undone.`)) return
  await listingsApi.remove(listing.value.id)
  router.push({ name: 'listing-mine' })
}

async function messageSeller() {
  messaging.value = true
  error.value = ''
  try {
    const thread = await conversationsApi.start(listing.value.id)
    router.push({ name: 'conversation-thread', params: { id: thread.id } })
  } catch (err) {
    error.value = err.message
  } finally {
    messaging.value = false
  }
}

load()
</script>

<template>
  <template v-if="listing">
    <h1>{{ listing.title }}</h1>
    <p class="muted">{{ listing.categoryName }} &middot; listed by {{ listing.ownerDisplayName }}</p>
    <p v-if="listing.price != null"><strong>${{ listing.price }}</strong></p>

    <ul class="image-gallery" v-if="listing.images.length">
      <li v-for="image in listing.images" :key="image.id">
        <img :src="imageUrl(image.url)" :alt="listing.title" />
      </li>
    </ul>

    <p>{{ listing.description }}</p>

    <p v-if="error" class="error">{{ error }}</p>

    <div v-if="listing.isOwner">
      <RouterLink :to="{ name: 'listing-edit', params: { id: listing.id } }">Edit listing</RouterLink>
      &middot;
      <button class="danger" @click="deleteListing">Delete listing</button>
    </div>
    <button v-else-if="listing.canMessage" @click="messageSeller" :disabled="messaging">Message seller</button>
  </template>
</template>
