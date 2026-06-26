import { Component, inject, signal, computed, effect, untracked } from '@angular/core';
import { rxResource, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { DecimalPipe, CurrencyPipe } from '@angular/common';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { AdminService } from '../../services/admin.service';
import type { AdminProduct, UpdateProductRequest } from '../../models/admin.models';

@Component({
  selector: 'app-admin-products',
  imports: [ReactiveFormsModule, DecimalPipe, CurrencyPipe],
  templateUrl: './products.component.html',
  styleUrl: './products.component.scss',
})
export class ProductsComponent {
  #admin  = inject(AdminService);
  #fb     = inject(FormBuilder);

  // ── Pagination & modal state ───────────────────────────────────────────────
  page     = signal(1);
  pageSize = signal(20);
  editing  = signal<AdminProduct | null>(null);
  creating = signal(false);
  saving   = signal(false);
  deleting = signal<number | null>(null);
  error    = signal<string | null>(null);
  uploadingImage = signal(false);

  // ── Search / filter / sort signals ────────────────────────────────────────
  searchInput      = signal('');
  categoryFilter   = signal<number | undefined>(undefined);
  isActiveFilter   = signal<boolean | undefined>(undefined);
  isFeaturedFilter = signal<boolean | undefined>(undefined);
  lowStockFilter   = signal(false);
  minPrice         = signal<number | undefined>(undefined);
  maxPrice         = signal<number | undefined>(undefined);
  minRating        = signal<number | undefined>(undefined);
  maxRating        = signal<number | undefined>(undefined);
  sortBy           = signal('name');
  sortDir          = signal<'asc' | 'desc'>('asc');

  readonly #searchDebounced = toSignal(
    toObservable(this.searchInput).pipe(debounceTime(500), distinctUntilChanged()),
    { initialValue: '' },
  );

  // Reset to page 1 when any filter/sort changes
  readonly #resetPage = effect(() => {
    this.#searchDebounced();
    this.categoryFilter();
    this.isActiveFilter();
    this.isFeaturedFilter();
    this.lowStockFilter();
    this.minPrice();
    this.maxPrice();
    this.minRating();
    this.maxRating();
    this.sortBy();
    this.sortDir();
    untracked(() => this.page.set(1));
  });

  // ── Data ───────────────────────────────────────────────────────────────────
  productsResource = rxResource({
    stream: () => this.#admin.getProducts(
      this.page(),
      this.pageSize(),
      this.#searchDebounced() || undefined,
      this.categoryFilter(),
      this.isFeaturedFilter(),
      this.lowStockFilter() || undefined,
      this.minPrice(),
      this.maxPrice(),
      this.minRating(),
      this.maxRating(),
      this.sortBy(),
      this.sortDir(),
      this.isActiveFilter(),
    ),
  });

  categoriesResource = rxResource({ stream: () => this.#admin.getCategories() });

  readonly fallbackImage = 'data:image/svg+xml,' + encodeURIComponent(
    '<svg xmlns="http://www.w3.org/2000/svg" width="40" height="40">' +
    '<rect width="40" height="40" fill="#f3f4f6"/></svg>'
  );

  categories  = computed(() => this.categoriesResource.value() ?? []);
  pagedResult = computed(() => this.productsResource.value());
  products    = computed(() => this.pagedResult()?.items ?? []);
  totalPages  = computed(() => this.pagedResult()?.totalPages ?? 1);

  // ── Form ───────────────────────────────────────────────────────────────────
  form = this.#fb.nonNullable.group({
    categoryId:  [0,    [Validators.required, Validators.min(1)]],
    name:        ['',   [Validators.required, Validators.maxLength(256)]],
    brand:       ['',   [Validators.required, Validators.maxLength(100)]],
    description: ['' as string | null],
    price:       [0,    [Validators.required, Validators.min(0.01)]],
    rating:      [0,    [Validators.min(0), Validators.max(5)]],
    stock:       [0,    [Validators.required, Validators.min(0)]],
    imageUrl:    ['' as string | null],
    isFeatured:  [false],
    isActive:    [true],
  });

  // ── Filter / sort actions ─────────────────────────────────────────────────
  sort(field: string) {
    if (this.sortBy() === field) {
      this.sortDir.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(field);
      this.sortDir.set('asc');
    }
  }

  sortIcon(field: string): string {
    if (this.sortBy() !== field) return '';
    return this.sortDir() === 'asc' ? ' ↑' : ' ↓';
  }

  setCategoryFilter(v: string) { this.categoryFilter.set(v === '' ? undefined : +v); }
  setIsActiveFilter(v: string) { this.isActiveFilter.set(v === '' ? undefined : v === 'true'); }
  setIsFeaturedFilter(v: string) { this.isFeaturedFilter.set(v === '' ? undefined : v === 'true'); }

  resetFilters() {
    this.searchInput.set('');
    this.categoryFilter.set(undefined);
    this.isActiveFilter.set(undefined);
    this.isFeaturedFilter.set(undefined);
    this.lowStockFilter.set(false);
    this.minPrice.set(undefined);
    this.maxPrice.set(undefined);
    this.minRating.set(undefined);
    this.maxRating.set(undefined);
    this.sortBy.set('name');
    this.sortDir.set('asc');
  }

  // ── Modal actions (unchanged) ──────────────────────────────────────────────
  openCreate() {
    this.form.reset({ isActive: true, isFeatured: false, rating: 0, stock: 0 });
    this.editing.set(null);
    this.creating.set(true);
    this.error.set(null);
  }

  openEdit(p: AdminProduct) {
    this.form.setValue({
      categoryId:  p.categoryId,
      name:        p.name,
      brand:       p.brand,
      description: p.description ?? '',
      price:       p.price,
      rating:      p.rating,
      stock:       p.stock,
      imageUrl:    p.imageUrl ?? '',
      isFeatured:  p.isFeatured,
      isActive:    p.isActive,
    });
    this.editing.set(p);
    this.creating.set(true);
    this.error.set(null);
  }

  closeModal() {
    this.creating.set(false);
    this.editing.set(null);
  }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    this.error.set(null);

    const val = this.form.getRawValue();
    const req: UpdateProductRequest = {
      categoryId:  val.categoryId,
      name:        val.name,
      brand:       val.brand,
      description: val.description || null,
      price:       val.price,
      rating:      val.rating,
      stock:       val.stock,
      imageUrl:    val.imageUrl || null,
      isFeatured:  val.isFeatured,
      isActive:    val.isActive,
    };

    const onNext = () => { this.saving.set(false); this.closeModal(); this.productsResource.reload(); };
    const onError = (e: { error?: { title?: string } }) => {
      this.saving.set(false);
      this.error.set(e?.error?.title ?? 'Failed to save product. Please try again.');
    };

    const existing = this.editing();
    if (existing) {
      this.#admin.updateProduct(existing.id, req).subscribe({ next: onNext, error: onError });
    } else {
      this.#admin.createProduct(req).subscribe({ next: onNext, error: onError });
    }
  }

  softDelete(id: number) {
    if (!confirm('Deactivate this product?')) return;
    this.deleting.set(id);
    this.#admin.deleteProduct(id).subscribe({
      next: () => { this.deleting.set(null); this.productsResource.reload(); },
      error: () => { this.deleting.set(null); },
    });
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    if (!file) return;

    this.uploadingImage.set(true);
    this.#admin.uploadImage(file).subscribe({
      next: (res) => {
        this.form.patchValue({ imageUrl: res.url });
        this.uploadingImage.set(false);
      },
      error: () => {
        this.uploadingImage.set(false);
        this.error.set('Image upload failed. Max 5 MB (JPEG/PNG/WebP).');
      },
    });
  }

  prevPage() { if (this.page() > 1) this.page.update(p => p - 1); }
  nextPage() { if (this.page() < this.totalPages()) this.page.update(p => p + 1); }
}
