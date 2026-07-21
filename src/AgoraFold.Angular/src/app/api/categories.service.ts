import { Injectable, inject } from '@angular/core';
import { ApiClient } from './client';
import type { Category } from './types';

@Injectable({ providedIn: 'root' })
export class CategoriesApi {
  private readonly api = inject(ApiClient);

  getAll(): Promise<Category[]> {
    return this.api.request<Category[]>('/api/categories');
  }
}
