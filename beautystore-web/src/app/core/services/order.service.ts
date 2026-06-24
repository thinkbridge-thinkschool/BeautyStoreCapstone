import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PlaceOrderResponse {
  orderId: number;
  status: string;
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  #http = inject(HttpClient);

  placeOrder(productId: number, quantity: number): Observable<PlaceOrderResponse> {
    return this.#http.post<PlaceOrderResponse>(
      `${environment.apiUrl}/api/orders`,
      { productId, quantity }
    );
  }
}
