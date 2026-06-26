import { Component, inject, signal, computed, effect, untracked } from '@angular/core';
import { rxResource, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { AdminService } from '../../services/admin.service';
import type { AdminUser, PagedResult } from '../../models/admin.models';

@Component({
  selector: 'app-admin-users',
  imports: [DatePipe],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss',
})
export class UsersComponent {
  #admin = inject(AdminService);

  // ── Pagination / search / filter / sort signals ───────────────────────────
  page       = signal(1);
  pageSize   = signal(20);
  searchInput = signal('');
  roleFilter = signal<string | undefined>(undefined);
  sortBy     = signal('email');
  sortDir    = signal<'asc' | 'desc'>('asc');

  readonly #searchDebounced = toSignal(
    toObservable(this.searchInput).pipe(debounceTime(500), distinctUntilChanged()),
    { initialValue: '' },
  );

  readonly #resetPage = effect(() => {
    this.#searchDebounced();
    this.roleFilter();
    this.sortBy();
    this.sortDir();
    untracked(() => this.page.set(1));
  });

  // ── Data ───────────────────────────────────────────────────────────────────
  usersResource = rxResource<PagedResult<AdminUser>, unknown>({
    stream: () => this.#admin.getUsers(
      this.page(),
      this.pageSize(),
      this.#searchDebounced() || undefined,
      this.roleFilter(),
      this.sortBy(),
      this.sortDir(),
    ),
  });

  users      = computed(() => this.usersResource.value()?.items ?? []);
  totalPages = computed(() => this.usersResource.value()?.totalPages ?? 1);
  totalCount = computed(() => this.usersResource.value()?.totalCount ?? 0);

  // ── Existing modal state ──────────────────────────────────────────────────
  assigningFor = signal<AdminUser | null>(null);
  assigning    = signal(false);
  error        = signal<string | null>(null);

  // ── Sort ───────────────────────────────────────────────────────────────────
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

  setRoleFilter(v: string) { this.roleFilter.set(v || undefined); }

  // ── Existing modal actions (unchanged) ────────────────────────────────────
  openAssign(user: AdminUser) {
    this.assigningFor.set(user);
    this.error.set(null);
  }

  closeModal() {
    this.assigningFor.set(null);
  }

  assignAdmin(user: AdminUser) {
    if (user.roles.includes('Admin')) return;
    if (!confirm(`Promote ${user.email} to Admin?`)) return;
    this.assigning.set(true);
    this.#admin.assignRole(user.id, 'Admin').subscribe({
      next: () => { this.assigning.set(false); this.closeModal(); this.usersResource.reload(); },
      error: (e: { error?: { title?: string } }) => {
        this.assigning.set(false);
        this.error.set(e?.error?.title ?? 'Failed to assign role.');
      },
    });
  }

  prevPage() { if (this.page() > 1) this.page.update(p => p - 1); }
  nextPage() { if (this.page() < this.totalPages()) this.page.update(p => p + 1); }
}
