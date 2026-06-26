import { Component, inject, signal, computed, effect, untracked } from '@angular/core';
import { rxResource, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { AdminService } from '../../services/admin.service';
import type { InventoryItem, PagedResult } from '../../models/admin.models';

@Component({
  selector: 'app-admin-inventory',
  imports: [CurrencyPipe, DecimalPipe],
  templateUrl: './inventory.component.html',
  styleUrl:    './inventory.component.scss',
})
export class InventoryComponent {
  #admin = inject(AdminService);

  // ── Filter / sort signals ─────────────────────────────────────────────────
  page         = signal(1);
  readonly pageSize    = signal(20);
  searchInput  = signal('');
  sortBy       = signal('name');
  sortDir      = signal<'asc' | 'desc'>('asc');
  lowStockOnly = signal(false);
  isActiveFilter = signal<boolean | undefined>(undefined);

  readonly #searchDebounced = toSignal(
    toObservable(this.searchInput).pipe(debounceTime(500), distinctUntilChanged()),
    { initialValue: '' },
  );

  readonly #resetPageOnSearch = effect(() => {
    this.#searchDebounced();
    untracked(() => this.page.set(1));
  });

  // ── Data ──────────────────────────────────────────────────────────────────
  inventoryResource = rxResource<PagedResult<InventoryItem>, unknown>({
    stream: () => this.#admin.getInventory(
      this.page(),
      this.pageSize(),
      this.#searchDebounced() || undefined,
      this.sortBy(),
      this.sortDir(),
      this.lowStockOnly() || undefined,
      this.isActiveFilter(),
    ),
  });

  inventoryItems = computed(() => this.inventoryResource.value()?.items ?? []);
  totalPages     = computed(() => this.inventoryResource.value()?.totalPages ?? 1);
  totalCount     = computed(() => this.inventoryResource.value()?.totalCount ?? 0);

  // ── Inline stock edit state ───────────────────────────────────────────────
  editedStock = signal<Record<number, number>>({});
  savingId    = signal<number | null>(null);
  saveError   = signal<string | null>(null);

  // ── Actions ───────────────────────────────────────────────────────────────
  sort(field: string) {
    if (this.sortBy() === field) {
      this.sortDir.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(field);
      this.sortDir.set('asc');
    }
    this.page.set(1);
  }

  sortIcon(field: string): string {
    if (this.sortBy() !== field) return '';
    return this.sortDir() === 'asc' ? ' ↑' : ' ↓';
  }

  setLowStockOnly(v: boolean) {
    this.lowStockOnly.set(v);
    this.page.set(1);
  }

  setIsActiveFilter(v: string) {
    this.isActiveFilter.set(v === '' ? undefined : v === 'true');
    this.page.set(1);
  }

  prevPage() { this.page.update(p => Math.max(1, p - 1)); }
  nextPage() { this.page.update(p => Math.min(this.totalPages(), p + 1)); }

  onStockInput(productId: number, value: number) {
    this.editedStock.update(e => ({ ...e, [productId]: value }));
  }

  isStockDirty(productId: number, currentStock: number): boolean {
    return productId in this.editedStock() &&
           this.editedStock()[productId] !== currentStock;
  }

  saveStock(productId: number, currentStock: number) {
    const newStock = this.editedStock()[productId];
    if (newStock === undefined || newStock === currentStock || newStock < 0) return;

    this.savingId.set(productId);
    this.saveError.set(null);

    this.#admin.updateStock(productId, newStock).subscribe({
      next: () => {
        this.savingId.set(null);
        this.editedStock.update(e => {
          const updated = { ...e };
          delete updated[productId];
          return updated;
        });
        this.inventoryResource.reload();
      },
      error: (e: any) => {
        this.savingId.set(null);
        this.saveError.set(e?.error?.title ?? 'Failed to update stock.');
      },
    });
  }

  cancelEdit(productId: number) {
    this.editedStock.update(e => {
      const updated = { ...e };
      delete updated[productId];
      return updated;
    });
  }
}
