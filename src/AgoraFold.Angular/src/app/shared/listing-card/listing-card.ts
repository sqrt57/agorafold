import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { imageUrl } from '../../api/client';
import type { ListingSummary } from '../../api/types';

@Component({
  selector: 'app-listing-card',
  imports: [RouterLink],
  templateUrl: './listing-card.html',
})
export class ListingCard {
  readonly listing = input.required<ListingSummary>();
  protected readonly imageUrl = imageUrl;
}
