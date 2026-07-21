import { createEffect, createSignal, For, Show } from 'solid-js'
import { useNavigate, useParams } from '@solidjs/router'
import * as listingsApi from '../api/listings'
import * as categoriesApi from '../api/categories'
import { imageUrl, ApiError } from '../api/client'
import type { Category, ListingImage } from '../api/types'

interface FormState {
  title: string
  description: string
  price: number | string
  categoryId: number | string
}

function errorMessages(err: unknown): string[] {
  if (err instanceof ApiError && err.errors.length) return err.errors
  return [(err as Error).message]
}

export default function ListingFormPage() {
  const params = useParams<{ id: string }>()
  const navigate = useNavigate()
  const isEdit = () => !!params.id

  const [categories, setCategories] = createSignal<Category[]>([])
  const [images, setImages] = createSignal<ListingImage[]>([])
  const [errors, setErrors] = createSignal<string[]>([])
  const [saving, setSaving] = createSignal(false)
  const [form, setForm] = createSignal<FormState>({ title: '', description: '', price: '', categoryId: '' })
  const [newImageFiles, setNewImageFiles] = createSignal<FileList | null>(null)

  createEffect(() => {
    const id = params.id
    categoriesApi.getAll().then((loadedCategories) => {
      setCategories(loadedCategories)
      if (id) {
        listingsApi.getDetail(id).then((listing) => {
          setForm({
            title: listing.title,
            description: listing.description,
            price: listing.price ?? '',
            categoryId: listing.categoryId,
          })
          setImages(listing.images)
        })
      } else if (loadedCategories.length) {
        setForm((f) => ({ ...f, categoryId: loadedCategories[0].id }))
      }
    })
  })

  async function submit(e: SubmitEvent) {
    e.preventDefault()
    setSaving(true)
    setErrors([])
    try {
      if (isEdit()) {
        await listingsApi.update(params.id, form())
        navigate(`/listings/${params.id}`)
      } else {
        const created = await listingsApi.create({ ...form(), images: newImageFiles() })
        setErrors(created.imageErrors ?? [])
        navigate(`/listings/${created.id}`)
      }
    } catch (err) {
      setErrors(errorMessages(err))
    } finally {
      setSaving(false)
    }
  }

  async function addMoreImages() {
    const files = newImageFiles()
    if (!files?.length) return
    setErrors([])
    try {
      setImages(await listingsApi.addImages(params.id, files))
      setNewImageFiles(null)
    } catch (err) {
      setErrors(errorMessages(err))
    }
  }

  async function removeImage(imageId: number) {
    await listingsApi.deleteImage(params.id, imageId)
    setImages((imgs) => imgs.filter((i) => i.id !== imageId))
  }

  function onImageFilesChange(e: Event & { currentTarget: HTMLInputElement }) {
    setNewImageFiles(e.currentTarget.files)
  }

  return (
    <>
      <h1>{isEdit() ? 'Edit listing' : 'Post a listing'}</h1>

      <Show when={errors().length > 0}>
        <ul class="error-list">
          <For each={errors()}>{(e) => <li>{e}</li>}</For>
        </ul>
      </Show>

      <form onSubmit={submit}>
        <label>
          Title
          <input
            value={form().title}
            onInput={(e) => setForm((f) => ({ ...f, title: e.currentTarget.value }))}
            type="text"
            required
            maxlength={200}
          />
        </label>
        <label>
          Description
          <textarea
            value={form().description}
            onInput={(e) => setForm((f) => ({ ...f, description: e.currentTarget.value }))}
            required
            maxlength={4000}
          />
        </label>
        <label>
          Price (optional)
          <input
            value={form().price}
            onInput={(e) => setForm((f) => ({ ...f, price: e.currentTarget.value }))}
            type="number"
            min="0"
            step="0.01"
          />
        </label>
        <label>
          Category
          <select
            value={form().categoryId}
            onChange={(e) => setForm((f) => ({ ...f, categoryId: e.currentTarget.value }))}
            required
          >
            <For each={categories()}>{(c) => <option value={c.id}>{c.name}</option>}</For>
          </select>
        </label>
        <Show when={!isEdit()}>
          <label>
            Images
            <input type="file" accept="image/*" multiple onChange={onImageFilesChange} />
          </label>
        </Show>
        <button type="submit" disabled={saving()}>
          {isEdit() ? 'Save changes' : 'Post listing'}
        </button>
      </form>

      <Show when={isEdit()}>
        <h2>Images</h2>
        <ul class="image-gallery">
          <For each={images()}>
            {(image) => (
              <li>
                <img src={imageUrl(image.url)} alt="" />
                <button class="danger" onClick={() => removeImage(image.id)}>
                  Remove
                </button>
              </li>
            )}
          </For>
        </ul>
        <label>
          Add images
          <input type="file" accept="image/*" multiple onChange={onImageFilesChange} />
        </label>
        <button class="secondary" onClick={addMoreImages}>
          Upload
        </button>
      </Show>
    </>
  )
}
