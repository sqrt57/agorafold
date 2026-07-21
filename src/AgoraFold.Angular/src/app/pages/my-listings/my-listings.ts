import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ListingsApi } from '../../api/listings.service';
import type { ListingSummary } from '../../api/types';
import { ListingCard } from '../../shared/listing-card/listing-card';

@Component({
  selector: 'app-my-listings',
  imports: [RouterLink, ListingCard],
  templateUrl: './my-listings.html',
})
export class MyListings {
  private readonly listingsApi = inject(ListingsApi);

  protected readonly listings = signal<ListingSummary[]>([]);

  constructor() {
    void this.listingsApi.getMine().then((loaded) => this.listings.set(loaded));
  }
}
