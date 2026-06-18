import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Product } from '../models/product.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  #http = inject(HttpClient);

  getProducts(): Observable<Product[]> {
    return this.#http.get<Product[]>(`${environment.apiUrl}/api/catalog/products`);
  }
}
