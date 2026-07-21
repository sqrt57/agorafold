import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
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
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const isEdit = !!id

  const [categories, setCategories] = useState<Category[]>([])
  const [images, setImages] = useState<ListingImage[]>([])
  const [errors, setErrors] = useState<string[]>([])
  const [saving, setSaving] = useState(false)
  const [form, setForm] = useState<FormState>({ title: '', description: '', price: '', categoryId: '' })
  const [newImageFiles, setNewImageFiles] = useState<FileList | null>(null)

  useEffect(() => {
    categoriesApi.getAll().then((loadedCategories) => {
      setCategories(loadedCategories)
      if (isEdit) {
        listingsApi.getDetail(id!).then((listing) => {
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
  }, [id])

  async function submit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setErrors([])
    try {
      if (isEdit) {
        await listingsApi.update(id!, form)
        navigate(`/listings/${id}`)
      } else {
        const created = await listingsApi.create({ ...form, images: newImageFiles })
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
    if (!newImageFiles?.length) return
    setErrors([])
    try {
      setImages(await listingsApi.addImages(id!, newImageFiles))
      setNewImageFiles(null)
    } catch (err) {
      setErrors(errorMessages(err))
    }
  }

  async function removeImage(imageId: number) {
    await listingsApi.deleteImage(id!, imageId)
    setImages((imgs) => imgs.filter((i) => i.id !== imageId))
  }

  function onImageFilesChange(e: ChangeEvent<HTMLInputElement>) {
    setNewImageFiles(e.target.files)
  }

  return (
    <>
      <h1>{isEdit ? 'Edit listing' : 'Post a listing'}</h1>

      {errors.length > 0 && (
        <ul className="error-list">
          {errors.map((e, i) => (
            <li key={i}>{e}</li>
          ))}
        </ul>
      )}

      <form onSubmit={submit}>
        <label>
          Title
          <input
            value={form.title}
            onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))}
            type="text"
            required
            maxLength={200}
          />
        </label>
        <label>
          Description
          <textarea
            value={form.description}
            onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
            required
            maxLength={4000}
          />
        </label>
        <label>
          Price (optional)
          <input
            value={form.price}
            onChange={(e) => setForm((f) => ({ ...f, price: e.target.value }))}
            type="number"
            min="0"
            step="0.01"
          />
        </label>
        <label>
          Category
          <select
            value={form.categoryId}
            onChange={(e) => setForm((f) => ({ ...f, categoryId: e.target.value }))}
            required
          >
            {categories.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        </label>
        {!isEdit && (
          <label>
            Images
            <input type="file" accept="image/*" multiple onChange={onImageFilesChange} />
          </label>
        )}
        <button type="submit" disabled={saving}>
          {isEdit ? 'Save changes' : 'Post listing'}
        </button>
      </form>

      {isEdit && (
        <>
          <h2>Images</h2>
          <ul className="image-gallery">
            {images.map((image) => (
              <li key={image.id}>
                <img src={imageUrl(image.url)} alt="" />
                <button className="danger" onClick={() => removeImage(image.id)}>
                  Remove
                </button>
              </li>
            ))}
          </ul>
          <label>
            Add images
            <input type="file" accept="image/*" multiple onChange={onImageFilesChange} />
          </label>
          <button className="secondary" onClick={addMoreImages}>
            Upload
          </button>
        </>
      )}
    </>
  )
}
