import { apiFetch } from './client'

export function browse({ categoryId, searchTerm, page = 1 } = {}) {
  const params = new URLSearchParams()
  if (categoryId) params.set('categoryId', categoryId)
  if (searchTerm) params.set('searchTerm', searchTerm)
  params.set('page', page)

  return apiFetch(`/api/listings?${params.toString()}`)
}

export function getDetail(id) {
  return apiFetch(`/api/listings/${id}`)
}

export function getMine() {
  return apiFetch('/api/listings/mine')
}

export function create({ title, description, price, categoryId, images }) {
  const form = new FormData()
  form.set('Title', title)
  form.set('Description', description)
  if (price !== null && price !== '') form.set('Price', price)
  form.set('CategoryId', categoryId)
  for (const image of images ?? []) {
    form.append('Images', image)
  }

  return apiFetch('/api/listings', { method: 'POST', body: form, isForm: true })
}

export function update(id, { title, description, price, categoryId }) {
  return apiFetch(`/api/listings/${id}`, {
    method: 'PUT',
    body: { title, description, price: price === '' ? null : price, categoryId },
  })
}

export function remove(id) {
  return apiFetch(`/api/listings/${id}`, { method: 'DELETE' })
}

export function addImages(id, images) {
  const form = new FormData()
  for (const image of images) {
    form.append('Images', image)
  }

  return apiFetch(`/api/listings/${id}/images`, { method: 'POST', body: form, isForm: true })
}

export function deleteImage(id, imageId) {
  return apiFetch(`/api/listings/${id}/images/${imageId}`, { method: 'DELETE' })
}
