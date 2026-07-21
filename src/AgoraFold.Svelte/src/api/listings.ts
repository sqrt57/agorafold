import { apiFetch } from './client'
import type { ListingDetail, ListingImage, ListingSummary, PagedListings } from './types'

export interface BrowseParams {
  categoryId?: number | string
  searchTerm?: string
  page?: number
}

export interface ListingFormFields {
  title: string
  description: string
  price: number | string | null
  categoryId: number | string
}

export interface CreateListingInput extends ListingFormFields {
  images?: FileList | File[] | null
}

export function browse({ categoryId, searchTerm, page = 1 }: BrowseParams = {}): Promise<PagedListings> {
  const params = new URLSearchParams()
  if (categoryId) params.set('categoryId', String(categoryId))
  if (searchTerm) params.set('searchTerm', searchTerm)
  params.set('page', String(page))
  return apiFetch<PagedListings>(`/api/listings?${params.toString()}`)
}

export function getDetail(id: number | string): Promise<ListingDetail> {
  return apiFetch<ListingDetail>(`/api/listings/${id}`)
}

export function getMine(): Promise<ListingSummary[]> {
  return apiFetch<ListingSummary[]>('/api/listings/mine')
}

export function create({ title, description, price, categoryId, images }: CreateListingInput): Promise<ListingDetail> {
  const form = new FormData()
  form.set('Title', title)
  form.set('Description', description)
  if (price !== null && price !== '') form.set('Price', String(price))
  form.set('CategoryId', String(categoryId))
  for (const image of images ?? []) form.append('Images', image)
  return apiFetch<ListingDetail>('/api/listings', { method: 'POST', body: form, isForm: true })
}

export function update(id: number | string, { title, description, price, categoryId }: ListingFormFields): Promise<ListingDetail> {
  return apiFetch<ListingDetail>(`/api/listings/${id}`, {
    method: 'PUT',
    body: { title, description, price: price === '' ? null : price, categoryId },
  })
}

export function remove(id: number | string): Promise<null> {
  return apiFetch<null>(`/api/listings/${id}`, { method: 'DELETE' })
}

export function addImages(id: number | string, images: FileList | File[]): Promise<ListingImage[]> {
  const form = new FormData()
  for (const image of images) form.append('Images', image)
  return apiFetch<ListingImage[]>(`/api/listings/${id}/images`, { method: 'POST', body: form, isForm: true })
}

export function deleteImage(id: number | string, imageId: number): Promise<null> {
  return apiFetch<null>(`/api/listings/${id}/images/${imageId}`, { method: 'DELETE' })
}
