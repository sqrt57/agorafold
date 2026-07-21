import { Injectable, inject } from '@angular/core';
import { ApiClient } from './client';
import type { ListingDetail, ListingImage, ListingSummary, PagedListings } from './types';

export interface BrowseParams {
  categoryId?: number | string;
  searchTerm?: string;
  page?: number;
}

export interface ListingFormFields {
  title: string;
  description: string;
  price: number | string | null;
  categoryId: number | string;
}

export interface CreateListingInput extends ListingFormFields {
  images?: FileList | File[] | null;
}

@Injectable({ providedIn: 'root' })
export class ListingsApi {
  private readonly api = inject(ApiClient);

  browse({ categoryId, searchTerm, page = 1 }: BrowseParams = {}): Promise<PagedListings> {
    const params = new URLSearchParams();
    if (categoryId) params.set('categoryId', String(categoryId));
    if (searchTerm) params.set('searchTerm', searchTerm);
    params.set('page', String(page));
    return this.api.request<PagedListings>(`/api/listings?${params.toString()}`);
  }

  getDetail(id: number | string): Promise<ListingDetail> {
    return this.api.request<ListingDetail>(`/api/listings/${id}`);
  }

  getMine(): Promise<ListingSummary[]> {
    return this.api.request<ListingSummary[]>('/api/listings/mine');
  }

  create({ title, description, price, categoryId, images }: CreateListingInput): Promise<ListingDetail> {
    const form = new FormData();
    form.set('Title', title);
    form.set('Description', description);
    if (price !== null && price !== '') form.set('Price', String(price));
    form.set('CategoryId', String(categoryId));
    for (const image of images ?? []) form.append('Images', image);
    return this.api.request<ListingDetail>('/api/listings', { method: 'POST', body: form });
  }

  update(id: number | string, { title, description, price, categoryId }: ListingFormFields): Promise<ListingDetail> {
    return this.api.request<ListingDetail>(`/api/listings/${id}`, {
      method: 'PUT',
      body: { title, description, price: price === '' ? null : price, categoryId },
    });
  }

  remove(id: number | string): Promise<null> {
    return this.api.request<null>(`/api/listings/${id}`, { method: 'DELETE' });
  }

  addImages(id: number | string, images: FileList | File[]): Promise<ListingImage[]> {
    const form = new FormData();
    for (const image of images) form.append('Images', image);
    return this.api.request<ListingImage[]>(`/api/listings/${id}/images`, { method: 'POST', body: form });
  }

  deleteImage(id: number | string, imageId: number): Promise<null> {
    return this.api.request<null>(`/api/listings/${id}/images/${imageId}`, { method: 'DELETE' });
  }
}
