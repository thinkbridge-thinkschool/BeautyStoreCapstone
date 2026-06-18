import { Component, computed, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { Product } from '../../../../core/models/product.model';

@Component({
  selector: 'app-product-card',
  imports: [DecimalPipe],
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
          <button class="btn-add">Add to Bag</button>
        </div>
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
    .btn-add:hover { opacity: 0.9; transform: scale(1.03); }
  `]
})
export class ProductCardComponent {
  product = input.required<Product>();
  stars = computed(() => Array(5).fill(0));
  filledStars = computed(() => Math.round(this.product().rating));
}
