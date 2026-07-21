import { Component, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CategoriesApi } from '../../api/categories.service';
import { ListingsApi } from '../../api/listings.service';
import type { Category, PagedListings } from '../../api/types';
import { ListingCard } from '../../shared/listing-card/listing-card';
import { Pagination } from '../../shared/pagination/pagination';

@Component({
  selector: 'app-browse',
  imports: [FormsModule, ListingCard, Pagination],
  templateUrl: './browse.html',
})
export class Browse {
  private readonly categoriesApi = inject(CategoriesApi);
  private readonly listingsApi = inject(ListingsApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly categories = signal<Category[]>([]);
  protected readonly result = signal<PagedListings | null>(null);
  protected readonly loading = signal(true);
  protected readonly categoryId = signal('');
  protected readonly searchTerm = signal('');

  private loadedQuery: string | null = null;

  private readonly queryParams = toSignal(this.route.queryParams, { initialValue: this.route.snapshot.queryParams });

  constructor() {
    void this.categoriesApi.getAll().then((loaded) => this.categories.set(loaded));

    effect(() => {
      const params = this.queryParams();
      this.categoryId.set(params['categoryId'] ?? '');
      this.searchTerm.set(params['searchTerm'] ?? '');

      const query = JSON.stringify(params);
      if (query !== this.loadedQuery) {
        this.loadedQuery = query;
        this.loading.set(true);
        void this.listingsApi
          .browse({
            categoryId: params['categoryId'] || undefined,
            searchTerm: params['searchTerm'] || undefined,
            page: Number(params['page'] ?? 1),
          })
          .then((data) => {
            this.result.set(data);
            this.loading.set(false);
          });
      }
    });
  }

  applyFilters(event: SubmitEvent): void {
    event.preventDefault();
    const queryParams: Record<string, string> = {};
    if (this.categoryId()) queryParams['categoryId'] = this.categoryId();
    if (this.searchTerm()) queryParams['searchTerm'] = this.searchTerm();
    void this.router.navigate(['/'], { queryParams });
  }

  goToPage(page: number): void {
    const queryParams = { ...this.route.snapshot.queryParams, page: String(page) };
    void this.router.navigate(['/'], { queryParams });
  }
}
