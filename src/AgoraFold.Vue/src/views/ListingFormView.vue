<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import * as listingsApi from '../api/listings'
import * as categoriesApi from '../api/categories'
import { imageUrl, ApiError } from '../api/client'
import type { Category, ListingImage } from '../api/types'

const route = useRoute()
const router = useRouter()

const isEdit = computed(() => !!route.params.id)
const categories = ref<Category[]>([])
const images = ref<ListingImage[]>([])
const errors = ref<string[]>([])
const saving = ref(false)

const form = ref<{
  title: string
  description: string
  price: number | string
  categoryId: number | string
}>({
  title: '',
  description: '',
  price: '',
  categoryId: '',
})

const newImageFiles = ref<FileList | null>(null)

function errorMessages(err: unknown): string[] {
  if (err instanceof ApiError && err.errors.length) return err.errors
  return [(err as Error).message]
}

async function load() {
  categories.value = await categoriesApi.getAll()

  if (isEdit.value) {
    const listing = await listingsApi.getDetail(route.params.id as string)
    form.value = {
      title: listing.title,
      description: listing.description,
      price: listing.price ?? '',
      categoryId: listing.categoryId,
    }
    images.value = listing.images
  } else if (categories.value.length) {
    form.value.categoryId = categories.value[0].id
  }
}

async function submit() {
  saving.value = true
  errors.value = []
  try {
    if (isEdit.value) {
      await listingsApi.update(route.params.id as string, form.value)
      router.push({ name: 'listing-detail', params: { id: route.params.id as string } })
    } else {
      const created = await listingsApi.create({ ...form.value, images: newImageFiles.value })
      errors.value = created.imageErrors ?? []
      router.push({ name: 'listing-detail', params: { id: created.id } })
    }
  } catch (err) {
    errors.value = errorMessages(err)
  } finally {
    saving.value = false
  }
}

async function addMoreImages() {
  if (!newImageFiles.value?.length) return
  errors.value = []
  try {
    images.value = await listingsApi.addImages(route.params.id as string, newImageFiles.value)
    newImageFiles.value = null
  } catch (err) {
    errors.value = errorMessages(err)
  }
}

async function removeImage(imageId: number) {
  await listingsApi.deleteImage(route.params.id as string, imageId)
  images.value = images.value.filter((i) => i.id !== imageId)
}

function onImageFilesChange(event: Event) {
  newImageFiles.value = (event.target as HTMLInputElement).files
}

load()
</script>

<template>
  <h1>{{ isEdit ? 'Edit listing' : 'Post a listing' }}</h1>

  <ul v-if="errors.length" class="error-list">
    <li v-for="(e, i) in errors" :key="i">{{ e }}</li>
  </ul>

  <form @submit.prevent="submit">
    <label>
      Title
      <input v-model="form.title" type="text" required maxlength="200" />
    </label>
    <label>
      Description
      <textarea v-model="form.description" required maxlength="4000"></textarea>
    </label>
    <label>
      Price (optional)
      <input v-model="form.price" type="number" min="0" step="0.01" />
    </label>
    <label>
      Category
      <select v-model="form.categoryId" required>
        <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
    </label>
    <label v-if="!isEdit">
      Images
      <input type="file" accept="image/*" multiple @change="onImageFilesChange" />
    </label>
    <button type="submit" :disabled="saving">{{ isEdit ? 'Save changes' : 'Post listing' }}</button>
  </form>

  <template v-if="isEdit">
    <h2>Images</h2>
    <ul class="image-gallery">
      <li v-for="image in images" :key="image.id">
        <img :src="imageUrl(image.url)" alt="" />
        <button class="danger" @click="removeImage(image.id)">Remove</button>
      </li>
    </ul>
    <label>
      Add images
      <input type="file" accept="image/*" multiple @change="onImageFilesChange" />
    </label>
    <button class="secondary" @click="addMoreImages">Upload</button>
  </template>
</template>
