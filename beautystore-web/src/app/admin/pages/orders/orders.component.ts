import { Component, inject, signal, computed, effect, untracked } from '@angular/core';
import { rxResource, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { AdminService } from '../../services/admin.service';
import { ORDER_STATUSES, type AdminOrder, type PagedResult } from '../../models/admin.models';

@Component({
  selector: 'app-admin-orders',
  imports: [CurrencyPipe, DatePipe],
  templateUrl: './orders.component.html',
  styleUrl: './orders.component.scss',
})
export class OrdersComponent {
  #admin = inject(AdminService);

  // ── Existing signals ───────────────────────────────────────────────────────
  page         = signal(1);
  pageSize     = signal(20);
  statusFilter = signal<string | undefined>(undefined);
  updatingId   = signal<number | null>(null);

  readonly statuses = ORDER_STATUSES;

  readonly statusClass: Partial<Record<string, string>> = {
    Created:   'badge-blue',
    Confirmed: 'badge-yellow',
    Shipped:   'badge-purple',
    Delivered: 'badge-green',
    Cancelled: 'badge-red',
  };

  // ── New search / filter / sort signals ─────────────────────────────────────
  searchInput = signal('');
  dateFrom    = signal('');
  dateTo      = signal('');
  minAmount   = signal<number | undefined>(undefined);
  maxAmount   = signal<number | undefined>(undefined);
  sortBy      = signal('date');
  sortDir     = signal<'asc' | 'desc'>('desc');

  readonly #searchDebounced = toSignal(
    toObservable(this.searchInput).pipe(debounceTime(500), distinctUntilChanged()),
    { initialValue: '' },
  );

  readonly #resetPage = effect(() => {
    this.#searchDebounced();
    this.statusFilter();
    this.dateFrom();
    this.dateTo();
    this.minAmount();
    this.maxAmount();
    this.sortBy();
    this.sortDir();
    untracked(() => this.page.set(1));
  });

  // ── Data ───────────────────────────────────────────────────────────────────
  ordersResource = rxResource<PagedResult<AdminOrder>, unknown>({
    stream: () => this.#admin.getOrders(
      this.page(),
      this.pageSize(),
      this.statusFilter(),
      this.#searchDebounced() || undefined,
      this.dateFrom() || undefined,
      this.dateTo() || undefined,
      this.minAmount(),
      this.maxAmount(),
      this.sortBy(),
      this.sortDir(),
    ),
  });

  orders     = computed(() => this.ordersResource.value()?.items ?? []);
  totalPages = computed(() => this.ordersResource.value()?.totalPages ?? 1);
  totalCount = computed(() => this.ordersResource.value()?.totalCount ?? 0);

  // ── Sort ───────────────────────────────────────────────────────────────────
  sort(field: string) {
    if (this.sortBy() === field) {
      this.sortDir.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(field);
      this.sortDir.set(field === 'date' ? 'desc' : 'asc');
    }
  }

  sortIcon(field: string): string {
    if (this.sortBy() !== field) return '';
    return this.sortDir() === 'asc' ? ' ↑' : ' ↓';
  }

  // ── Existing filter / status actions ──────────────────────────────────────
  filterByStatus(status: string | undefined) {
    this.statusFilter.set(status || undefined);
    this.page.set(1);
  }

  updateStatus(order: AdminOrder, newStatus: string) {
    if (order.status === newStatus) return;
    this.updatingId.set(order.id);
    this.#admin.updateOrderStatus(order.id, { status: newStatus }).subscribe({
      next: () => { this.updatingId.set(null); this.ordersResource.reload(); },
      error: () => { this.updatingId.set(null); },
    });
  }

  prevPage() { if (this.page() > 1) this.page.update(p => p - 1); }
  nextPage() { if (this.page() < this.totalPages()) this.page.update(p => p + 1); }
}
