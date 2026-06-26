import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import type {
  DashboardStats,
  AdminProduct, CreateProductRequest, UpdateProductRequest,
  AdminCategory, CreateCategoryRequest, UpdateCategoryRequest,
  AdminOrder, UpdateOrderStatusRequest,
  AdminUser,
  PagedResult,
  AnalyticsData,
  InventoryItem,
  SettingsData,
} from '../models/admin.models';

const BASE = `${environment.apiUrl}/api/admin`;

@Injectable({ providedIn: 'root' })
export class AdminService {
  #http = inject(HttpClient);

  // ── Dashboard ───────────────────────────────────────────────────────────────
  getDashboard() {
    return this.#http.get<DashboardStats>(`${BASE}/dashboard`);
  }

  // ── Analytics ───────────────────────────────────────────────────────────────
  getAnalytics() {
    return this.#http.get<AnalyticsData>(`${BASE}/analytics`);
  }

  // ── Products ────────────────────────────────────────────────────────────────
  getProducts(
    page = 1, pageSize = 20,
    search?: string,
    categoryId?: number,
    isFeatured?: boolean,
    lowStockOnly?: boolean,
    minPrice?: number,
    maxPrice?: number,
    minRating?: number,
    maxRating?: number,
    sortBy = 'name', sortDir: 'asc' | 'desc' = 'asc',
    isActive?: boolean,
  ) {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize)
      .set('sortBy', sortBy)
      .set('sortDir', sortDir);
    if (search)                  params = params.set('search', search);
    if (categoryId !== undefined) params = params.set('categoryId', categoryId);
    if (isFeatured !== undefined) params = params.set('isFeatured', isFeatured);
    if (lowStockOnly)            params = params.set('lowStockOnly', lowStockOnly);
    if (minPrice !== undefined)  params = params.set('minPrice', minPrice);
    if (maxPrice !== undefined)  params = params.set('maxPrice', maxPrice);
    if (minRating !== undefined) params = params.set('minRating', minRating);
    if (maxRating !== undefined) params = params.set('maxRating', maxRating);
    if (isActive !== undefined)  params = params.set('isActive', isActive);
    return this.#http.get<PagedResult<AdminProduct>>(`${BASE}/products`, { params });
  }

  createProduct(req: CreateProductRequest) {
    return this.#http.post<{ id: number }>(`${BASE}/products`, req);
  }

  updateProduct(id: number, req: UpdateProductRequest) {
    return this.#http.put<void>(`${BASE}/products/${id}`, req);
  }

  deleteProduct(id: number) {
    return this.#http.delete<void>(`${BASE}/products/${id}`);
  }

  // ── Categories ──────────────────────────────────────────────────────────────
  getCategories() {
    return this.#http.get<AdminCategory[]>(`${BASE}/categories`);
  }

  createCategory(req: CreateCategoryRequest) {
    return this.#http.post<{ id: number }>(`${BASE}/categories`, req);
  }

  updateCategory(id: number, req: UpdateCategoryRequest) {
    return this.#http.put<void>(`${BASE}/categories/${id}`, req);
  }

  deleteCategory(id: number) {
    return this.#http.delete<{ softDeleted: boolean; message: string } | null>(
      `${BASE}/categories/${id}`,
    );
  }

  // ── Orders ──────────────────────────────────────────────────────────────────
  getOrders(
    page = 1, pageSize = 20,
    status?: string,
    search?: string,
    dateFrom?: string,
    dateTo?: string,
    minAmount?: number,
    maxAmount?: number,
    sortBy = 'date', sortDir: 'asc' | 'desc' = 'desc',
  ) {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize)
      .set('sortBy', sortBy)
      .set('sortDir', sortDir);
    if (status)                  params = params.set('status', status);
    if (search)                  params = params.set('search', search);
    if (dateFrom)                params = params.set('dateFrom', dateFrom);
    if (dateTo)                  params = params.set('dateTo', dateTo);
    if (minAmount !== undefined) params = params.set('minAmount', minAmount);
    if (maxAmount !== undefined) params = params.set('maxAmount', maxAmount);
    return this.#http.get<PagedResult<AdminOrder>>(`${BASE}/orders`, { params });
  }

  updateOrderStatus(id: number, req: UpdateOrderStatusRequest) {
    return this.#http.put<void>(`${BASE}/orders/${id}/status`, req);
  }

  // ── Users ───────────────────────────────────────────────────────────────────
  getUsers(
    page = 1, pageSize = 20,
    search?: string,
    role?: string,
    sortBy = 'email', sortDir: 'asc' | 'desc' = 'asc',
  ) {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize)
      .set('sortBy', sortBy)
      .set('sortDir', sortDir);
    if (search) params = params.set('search', search);
    if (role)   params = params.set('role', role);
    return this.#http.get<PagedResult<AdminUser>>(`${BASE}/users`, { params });
  }

  assignRole(userId: string, role: string) {
    return this.#http.post<void>(`${BASE}/users/${userId}/roles`, { role });
  }

  // ── Inventory ───────────────────────────────────────────────────────────────
  getInventory(
    page = 1, pageSize = 20,
    search?: string,
    sortBy = 'name', sortDir: 'asc' | 'desc' = 'asc',
    lowStockOnly?: boolean,
    isActive?: boolean,
  ) {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize)
      .set('sortBy', sortBy)
      .set('sortDir', sortDir);
    if (search)                  params = params.set('search', search);
    if (lowStockOnly !== undefined) params = params.set('lowStockOnly', lowStockOnly);
    if (isActive !== undefined)  params = params.set('isActive', isActive);
    return this.#http.get<PagedResult<InventoryItem>>(`${BASE}/inventory`, { params });
  }

  updateStock(productId: number, stock: number) {
    return this.#http.put<void>(`${BASE}/inventory/${productId}`, { stock });
  }

  // ── Images ──────────────────────────────────────────────────────────────────
  uploadImage(file: File) {
    const form = new FormData();
    form.append('file', file);
    return this.#http.post<{ url: string }>(`${BASE}/images/upload`, form);
  }

  // ── Settings ─────────────────────────────────────────────────────────────────
  getSettings() {
    return this.#http.get<SettingsData>(`${BASE}/settings`);
  }
}
