import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Product } from '../models/product.model';
import { CatalogProduct, CatalogProductDetail, Category, PagedResult } from '../models/catalog.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  #http = inject(HttpClient);

  // Legacy — used by home component
  getProducts(): Observable<Product[]> {
    return this.#http.get<Product[]>(`${environment.apiUrl}/api/catalog/products`);
  }

  // Properly-typed paged listing used by the products catalog page
  getProductsListing(pageSize = 200): Observable<PagedResult<CatalogProduct>> {
    return this.#http.get<PagedResult<CatalogProduct>>(
      `${environment.apiUrl}/api/catalog/products?pageSize=${pageSize}`
    );
  }

  getProductDetail(id: number): Observable<CatalogProductDetail> {
    return this.#http.get<CatalogProductDetail>(
      `${environment.apiUrl}/api/catalog/products/${id}`
    );
  }

  getCategories(): Observable<Category[]> {
    return this.#http.get<Category[]>(`${environment.apiUrl}/api/catalog/categories`);
  }
}
