import { Component, computed, inject, input, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { Product } from '../../../../core/models/product.model';
import { AuthService } from '../../../../auth/services/auth.service';
import { OrderService } from '../../../../core/services/order.service';

@Component({
  selector: 'app-product-card',
  imports: [DecimalPipe, RouterLink],
  template: `
    <div class="card">
      <div class="card-image-wrap">
        <img [src]="product().imageUrl" [alt]="product().name" loading="lazy" />
        <span class="category-badge">{{ product().category }}</span>
      </div>
      <div class="card-body">
        <p class="brand">{{ product().brand }}</p>
        <h3 class="name">{{ product().name }}</h3>
        <div class="stars">
          @for (star of stars(); track $index) {
            <span [class.filled]="$index < filledStars()">★</span>
          }
          <span class="rating-text">{{ product().rating }}</span>
        </div>
        <div class="card-footer">
          <span class="price">₹{{ product().price | number:'1.0-0' }}</span>

          @if (ordered()) {
            <a routerLink="/orders" class="btn-ordered">View Order ✓</a>
          } @else {
            <button class="btn-add"
                    [disabled]="loading()"
                    (click)="buy()">
              {{ loading() ? 'Placing…' : 'Add to Bag' }}
            </button>
          }
        </div>
        @if (errorMsg()) {
          <p class="err-msg">{{ errorMsg() }}</p>
        }
      </div>
    </div>
  `,
  styles: [`
    .card {
      background: #fff;
      border-radius: 16px;
      overflow: hidden;
      box-shadow: 0 4px 24px rgba(255, 79, 163, 0.08);
      transition: transform 0.25s, box-shadow 0.25s;
    }
    .card:hover {
      transform: translateY(-6px);
      box-shadow: 0 12px 40px rgba(255, 79, 163, 0.18);
    }
    .card-image-wrap {
      position: relative;
      overflow: hidden;
      height: 280px;
    }
    .card-image-wrap img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      transition: transform 0.4s;
    }
    .card-image-wrap:hover img { transform: scale(1.05); }
    .category-badge {
      position: absolute;
      top: 12px;
      left: 12px;
      background: rgba(255,255,255,0.9);
      backdrop-filter: blur(8px);
      padding: 4px 12px;
      border-radius: 100px;
      font-size: 0.7rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: #e0007a;
    }
    .card-body { padding: 20px; }
    .brand {
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.8px;
      color: #ff4fa3;
      margin-bottom: 6px;
    }
    .name {
      font-size: 1rem;
      font-weight: 600;
      color: #1a1a2e;
      line-height: 1.4;
      margin-bottom: 10px;
    }
    .stars {
      display: flex;
      align-items: center;
      gap: 2px;
      margin-bottom: 16px;
    }
    .stars span { color: #d1d5db; font-size: 1rem; }
    .stars span.filled { color: #f59e0b; }
    .rating-text {
      color: #6b7280;
      font-size: 0.8rem;
      margin-left: 6px;
    }
    .card-footer {
      display: flex;
      align-items: center;
      justify-content: space-between;
    }
    .price {
      font-family: 'Playfair Display', serif;
      font-size: 1.2rem;
      font-weight: 700;
      color: #1a1a2e;
    }
    .btn-add {
      background: linear-gradient(135deg, #ff4fa3, #e0007a);
      color: white;
      padding: 10px 20px;
      border-radius: 100px;
      font-size: 0.8rem;
      font-weight: 600;
      border: none;
      cursor: pointer;
      transition: opacity 0.2s, transform 0.2s;
    }
    .btn-add:disabled { opacity: 0.6; cursor: not-allowed; transform: none; }
    .btn-add:not(:disabled):hover { opacity: 0.9; transform: scale(1.03); }
    .btn-ordered {
      background: #f0fdf4;
      color: #16a34a;
      border: 1px solid #bbf7d0;
      padding: 10px 16px;
      border-radius: 100px;
      font-size: 0.8rem;
      font-weight: 600;
      text-decoration: none;
      white-space: nowrap;
    }
    .btn-ordered:hover { background: #dcfce7; }
    .err-msg {
      color: #dc2626;
      font-size: 0.75rem;
      margin-top: 8px;
      text-align: right;
    }
  `]
})
export class ProductCardComponent {
  product    = input.required<Product>();
  stars      = computed(() => Array(5).fill(0));
  filledStars = computed(() => Math.round(this.product().rating));

  loading  = signal(false);
  ordered  = signal(false);
  errorMsg = signal('');

  #auth   = inject(AuthService);
  #orders = inject(OrderService);
  #router = inject(Router);

  buy() {
    if (!this.#auth.isLoggedIn()) {
      this.#router.navigate(['/login']);
      return;
    }
    this.loading.set(true);
    this.errorMsg.set('');
    this.#orders.placeOrder(this.product().id, 1).subscribe({
      next: () => { this.ordered.set(true); this.loading.set(false); },
      error: () => { this.errorMsg.set('Could not place order. Try again.'); this.loading.set(false); },
    });
  }
}
