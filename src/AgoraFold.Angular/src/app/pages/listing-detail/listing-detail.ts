import { Component, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink, Router, ActivatedRoute } from '@angular/router';
import { ApiError, imageUrl } from '../../api/client';
import { ConversationsApi } from '../../api/conversations.service';
import { ListingsApi } from '../../api/listings.service';
import type { ListingDetail as ListingDetailModel } from '../../api/types';

@Component({
  selector: 'app-listing-detail',
  imports: [RouterLink],
  templateUrl: './listing-detail.html',
})
export class ListingDetail {
  private readonly conversationsApi = inject(ConversationsApi);
  private readonly listingsApi = inject(ListingsApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly imageUrl = imageUrl;
  protected readonly listing = signal<ListingDetailModel | null>(null);
  protected readonly messaging = signal(false);
  protected readonly error = signal('');

  private loadedId: string | null = null;

  private readonly paramMap = toSignal(this.route.paramMap, { initialValue: this.route.snapshot.paramMap });

  constructor() {
    effect(() => {
      const id = this.paramMap().get('id');
      if (id && id !== this.loadedId) {
        this.loadedId = id;
        this.listing.set(null);
        this.error.set('');
        void this.listingsApi.getDetail(id).then((loaded) => this.listing.set(loaded));
      }
    });
  }

  async deleteListing(): Promise<void> {
    const listing = this.listing();
    if (!listing || !confirm(`Delete "${listing.title}"? This cannot be undone.`)) return;
    await this.listingsApi.remove(listing.id);
    await this.router.navigateByUrl('/listings/mine');
  }

  async messageSeller(): Promise<void> {
    const listing = this.listing();
    if (!listing) return;
    this.messaging.set(true);
    this.error.set('');
    try {
      const thread = await this.conversationsApi.start(listing.id);
      await this.router.navigateByUrl(`/conversations/${thread.id}`);
    } catch (exception) {
      this.error.set(exception instanceof ApiError ? (exception.errors[0] ?? exception.message) : (exception as Error).message);
    } finally {
      this.messaging.set(false);
    }
  }
}
