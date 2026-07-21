import { Component, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiError, imageUrl } from '../../api/client';
import { CategoriesApi } from '../../api/categories.service';
import { ListingsApi } from '../../api/listings.service';
import type { Category, ListingImage } from '../../api/types';

@Component({
  selector: 'app-listing-form',
  imports: [FormsModule],
  templateUrl: './listing-form.html',
})
export class ListingForm {
  private readonly categoriesApi = inject(CategoriesApi);
  private readonly listingsApi = inject(ListingsApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly imageUrl = imageUrl;
  protected readonly categories = signal<Category[]>([]);
  protected readonly images = signal<ListingImage[]>([]);
  protected readonly errors = signal<string[]>([]);
  protected readonly saving = signal(false);

  protected readonly title = signal('');
  protected readonly description = signal('');
  protected readonly price = signal<number | string>('');
  protected readonly categoryId = signal<number | string>('');

  private newImageFiles: FileList | null = null;
  private loadedId: string | null = null;

  private readonly paramMap = toSignal(this.route.paramMap, { initialValue: this.route.snapshot.paramMap });

  protected isEdit(): boolean {
    return this.paramMap().has('id');
  }

  protected id(): string | null {
    return this.paramMap().get('id');
  }

  constructor() {
    void this.categoriesApi.getAll().then((loaded) => {
      this.categories.set(loaded);
      if (!this.isEdit() && loaded.length) this.categoryId.set(loaded[0].id);
    });

    effect(() => {
      const id = this.id();
      if (this.isEdit() && id && id !== this.loadedId) {
        this.loadedId = id;
        void this.listingsApi.getDetail(id).then((listing) => {
          this.title.set(listing.title);
          this.description.set(listing.description);
          this.price.set(listing.price ?? '');
          this.categoryId.set(listing.categoryId);
          this.images.set(listing.images);
        });
      }
    });
  }

  private errorMessages(exception: unknown): string[] {
    if (exception instanceof ApiError && exception.errors.length) return exception.errors;
    return [(exception as Error).message];
  }

  async submit(event: SubmitEvent): Promise<void> {
    event.preventDefault();
    this.saving.set(true);
    this.errors.set([]);
    const fields = { title: this.title(), description: this.description(), price: this.price(), categoryId: this.categoryId() };
    try {
      if (this.isEdit()) {
        const id = this.id()!;
        await this.listingsApi.update(id, fields);
        await this.router.navigateByUrl(`/listings/${id}`);
      } else {
        const created = await this.listingsApi.create({ ...fields, images: this.newImageFiles });
        this.errors.set(created.imageErrors ?? []);
        await this.router.navigateByUrl(`/listings/${created.id}`);
      }
    } catch (exception) {
      this.errors.set(this.errorMessages(exception));
    } finally {
      this.saving.set(false);
    }
  }

  async addMoreImages(): Promise<void> {
    const id = this.id();
    if (!this.newImageFiles?.length || !id) return;
    this.errors.set([]);
    try {
      this.images.set(await this.listingsApi.addImages(id, this.newImageFiles));
      this.newImageFiles = null;
    } catch (exception) {
      this.errors.set(this.errorMessages(exception));
    }
  }

  async removeImage(imageId: number): Promise<void> {
    const id = this.id();
    if (!id) return;
    await this.listingsApi.deleteImage(id, imageId);
    this.images.update((images) => images.filter((image) => image.id !== imageId));
  }

  onImageFilesChange(event: Event): void {
    this.newImageFiles = (event.currentTarget as HTMLInputElement).files;
  }
}
