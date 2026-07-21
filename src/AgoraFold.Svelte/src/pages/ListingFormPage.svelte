<script lang="ts">
  import { onMount } from 'svelte'
  import * as categoriesApi from '../api/categories'
  import * as listingsApi from '../api/listings'
  import { ApiError, imageUrl } from '../api/client'
  import type { Category, ListingImage } from '../api/types'
  import { navigate, route } from '../router'

  interface FormState {
    title: string
    description: string
    price: number | string
    categoryId: number | string
  }

  let categories: Category[] = []
  let images: ListingImage[] = []
  let errors: string[] = []
  let saving = false
  let form: FormState = { title: '', description: '', price: '', categoryId: '' }
  let newImageFiles: FileList | null = null
  let loadedId = ''

  $: id = $route.params.id
  $: isEdit = /^\/listings\/\d+\/edit$/.test($route.path)

  onMount(() => {
    void categoriesApi.getAll().then((loadedCategories) => {
      categories = loadedCategories
      if (!isEdit && loadedCategories.length) form = { ...form, categoryId: loadedCategories[0].id }
    })
  })

  $: if (isEdit && id && id !== loadedId) {
    loadedId = id
    void listingsApi.getDetail(id).then((listing) => {
      form = {
        title: listing.title,
        description: listing.description,
        price: listing.price ?? '',
        categoryId: listing.categoryId,
      }
      images = listing.images
    })
  }

  function errorMessages(exception: unknown): string[] {
    if (exception instanceof ApiError && exception.errors.length) return exception.errors
    return [(exception as Error).message]
  }

  async function submit(event: SubmitEvent) {
    event.preventDefault()
    saving = true
    errors = []
    try {
      if (isEdit) {
        await listingsApi.update(id!, form)
        navigate(`/listings/${id}`)
      } else {
        const created = await listingsApi.create({ ...form, images: newImageFiles })
        errors = created.imageErrors ?? []
        navigate(`/listings/${created.id}`)
      }
    } catch (exception) {
      errors = errorMessages(exception)
    } finally {
      saving = false
    }
  }

  async function addMoreImages() {
    if (!newImageFiles?.length || !id) return
    errors = []
    try {
      images = await listingsApi.addImages(id, newImageFiles)
      newImageFiles = null
    } catch (exception) {
      errors = errorMessages(exception)
    }
  }

  async function removeImage(imageId: number) {
    if (!id) return
    await listingsApi.deleteImage(id, imageId)
    images = images.filter((image) => image.id !== imageId)
  }

  function onImageFilesChange(event: Event) {
    newImageFiles = (event.currentTarget as HTMLInputElement).files
  }
</script>

<h1>{isEdit ? 'Edit listing' : 'Post a listing'}</h1>

{#if errors.length}
  <ul class="error-list">
    {#each errors as error}<li>{error}</li>{/each}
  </ul>
{/if}

<form onsubmit={submit}>
  <label>
    Title
    <input bind:value={form.title} type="text" required maxlength="200" />
  </label>
  <label>
    Description
    <textarea bind:value={form.description} required maxlength="4000"></textarea>
  </label>
  <label>
    Price (optional)
    <input bind:value={form.price} type="number" min="0" step="0.01" />
  </label>
  <label>
    Category
    <select bind:value={form.categoryId} required>
      {#each categories as category}
        <option value={category.id}>{category.name}</option>
      {/each}
    </select>
  </label>
  {#if !isEdit}
    <label>
      Images
      <input type="file" accept="image/*" multiple onchange={onImageFilesChange} />
    </label>
  {/if}
  <button type="submit" disabled={saving}>{isEdit ? 'Save changes' : 'Post listing'}</button>
</form>

{#if isEdit}
  <h2>Images</h2>
  <ul class="image-gallery">
    {#each images as image (image.id)}
      <li>
        <img src={imageUrl(image.url)} alt="" />
        <button class="danger" onclick={() => removeImage(image.id)}>Remove</button>
      </li>
    {/each}
  </ul>
  <label>
    Add images
    <input type="file" accept="image/*" multiple onchange={onImageFilesChange} />
  </label>
  <button class="secondary" onclick={addMoreImages}>Upload</button>
{/if}
