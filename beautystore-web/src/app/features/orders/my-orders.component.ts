import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../auth/services/auth.service';
import { environment } from '../../../environments/environment';

interface OrderItem {
  orderId: number;
  productName: string;
  quantity: number;
  totalPrice: number;
  status: string;
  createdAtUtc: string;
}

@Component({
  selector: 'app-my-orders',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="page">
      <header class="page-header">
        <a routerLink="/" class="brand">beautystore</a>
        <nav class="header-nav">
          <a routerLink="/profile" class="nav-link">{{ auth.currentUser()?.fullName }}</a>
          <button class="btn-logout" (click)="auth.logout()">Sign Out</button>
        </nav>
      </header>

      <main class="content">
        <div class="container">
          <div class="page-title">
            <h1>My Orders</h1>
            <a routerLink="/products" class="btn-shop">Shop More</a>
          </div>

          @if (loading()) {
            <div class="state-box">
              <div class="spinner"></div>
              <p>Loading your orders…</p>
            </div>
          } @else if (error()) {
            <div class="state-box error">
              <p>{{ error() }}</p>
              <button (click)="load()">Try again</button>
            </div>
          } @else if (orders().length === 0) {
            <div class="state-box">
              <div class="empty-icon">🛍️</div>
              <h2>No orders yet</h2>
              <p>Start shopping to place your first order.</p>
              <a routerLink="/products" class="btn-primary">Browse Products</a>
            </div>
          } @else {
            <div class="orders-list">
              @for (order of orders(); track order.orderId) {
                <div class="order-card">
                  <div class="order-info">
                    <span class="order-id">#{{ order.orderId }}</span>
                    <h3>{{ order.productName }}</h3>
                    <div class="order-meta">
                      <span>Qty: {{ order.quantity }}</span>
                      <span class="dot">·</span>
                      <span>₹{{ order.totalPrice.toLocaleString() }}</span>
                      <span class="dot">·</span>
                      <span>{{ formatDate(order.createdAtUtc) }}</span>
                    </div>
                  </div>
                  <div class="order-actions">
                    <span class="status-badge">{{ order.status }}</span>
                    <button class="btn-delete"
                            [disabled]="deleting() === order.orderId"
                            (click)="remove(order.orderId)"
                            title="Remove order">
                      {{ deleting() === order.orderId ? '…' : '✕' }}
                    </button>
                  </div>
                </div>
              }
            </div>
          }
        </div>
      </main>
    </div>
  `,
  styles: [`
    .page {
      min-height: 100vh;
      background: #fdf2f8;
      display: flex;
      flex-direction: column;
    }
    .page-header {
      background: linear-gradient(135deg, #ff4fa3 0%, #c0006a 100%);
      padding: 20px 48px;
      display: flex;
      align-items: center;
      justify-content: space-between;
    }
    .brand {
      font-family: 'Playfair Display', serif;
      font-size: 1.5rem;
      font-weight: 700;
      color: white;
      text-decoration: none;
    }
    .header-nav {
      display: flex;
      align-items: center;
      gap: 20px;
    }
    .nav-link {
      color: white;
      font-size: 0.9rem;
      font-weight: 600;
      text-decoration: none;
      opacity: 0.9;
    }
    .nav-link:hover { opacity: 1; }
    .btn-logout {
      background: rgba(255,255,255,0.2);
      color: white;
      border: 1px solid rgba(255,255,255,0.4);
      border-radius: 100px;
      padding: 8px 20px;
      font-size: 0.875rem;
      font-weight: 600;
      cursor: pointer;
      transition: background 0.2s;
    }
    .btn-logout:hover { background: rgba(255,255,255,0.35); }
    .content { flex: 1; padding: 40px 16px; }
    .container { max-width: 700px; margin: 0 auto; }
    .page-title {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 32px;
    }
    h1 {
      font-family: 'Playfair Display', serif;
      font-size: 2rem;
      color: #1a1a2e;
      margin: 0;
    }
    .btn-shop {
      background: linear-gradient(135deg, #ff4fa3 0%, #c0006a 100%);
      color: white;
      border-radius: 100px;
      padding: 10px 24px;
      font-size: 0.875rem;
      font-weight: 700;
      text-decoration: none;
      transition: opacity 0.2s;
    }
    .btn-shop:hover { opacity: 0.9; }
    .state-box {
      background: white;
      border-radius: 16px;
      padding: 60px 40px;
      text-align: center;
      box-shadow: 0 4px 32px rgba(224,0,122,0.06);
    }
    .state-box.error { background: #fff0f3; }
    .state-box p { color: #888; margin: 8px 0 20px; }
    .state-box h2 { font-family: 'Playfair Display', serif; color: #1a1a2e; margin: 8px 0 0; }
    .spinner {
      width: 40px; height: 40px;
      border: 3px solid #fce4f0;
      border-top-color: #e0007a;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
      margin: 0 auto 16px;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
    .empty-icon { font-size: 3rem; margin-bottom: 8px; }
    .btn-primary {
      display: inline-block;
      background: linear-gradient(135deg, #ff4fa3 0%, #c0006a 100%);
      color: white;
      border-radius: 100px;
      padding: 12px 28px;
      font-weight: 700;
      text-decoration: none;
      margin-top: 16px;
      transition: opacity 0.2s;
    }
    .btn-primary:hover { opacity: 0.9; }
    .orders-list { display: flex; flex-direction: column; gap: 16px; }
    .order-card {
      background: white;
      border-radius: 16px;
      padding: 24px 28px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      box-shadow: 0 2px 16px rgba(224,0,122,0.06);
      transition: box-shadow 0.2s;
    }
    .order-card:hover { box-shadow: 0 4px 24px rgba(224,0,122,0.12); }
    .order-id {
      font-size: 0.75rem;
      color: #e0007a;
      font-weight: 700;
      letter-spacing: 0.5px;
      display: block;
      margin-bottom: 4px;
    }
    .order-card h3 {
      font-size: 1rem;
      font-weight: 600;
      color: #1a1a2e;
      margin: 0 0 8px;
    }
    .order-meta {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 0.875rem;
      color: #666;
    }
    .dot { color: #ddd; }
    .order-actions {
      display: flex;
      align-items: center;
      gap: 12px;
      flex-shrink: 0;
    }
    .status-badge {
      background: #f0fdf4;
      color: #16a34a;
      border: 1px solid #bbf7d0;
      border-radius: 100px;
      padding: 6px 16px;
      font-size: 0.8rem;
      font-weight: 600;
      white-space: nowrap;
    }
    .btn-delete {
      width: 32px;
      height: 32px;
      border-radius: 50%;
      border: 1px solid #fecdd3;
      background: #fff0f3;
      color: #e11d48;
      font-size: 0.85rem;
      font-weight: 700;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: background 0.2s, border-color 0.2s;
      line-height: 1;
    }
    .btn-delete:hover:not(:disabled) { background: #ffe4e6; border-color: #fda4af; }
    .btn-delete:disabled { opacity: 0.5; cursor: not-allowed; }
  `],
})
export class MyOrdersComponent implements OnInit {
  auth   = inject(AuthService);
  http   = inject(HttpClient);

  orders   = signal<OrderItem[]>([]);
  loading  = signal(true);
  error    = signal('');
  deleting = signal<number | null>(null);

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.error.set('');
    this.http.get<OrderItem[]>(`${environment.apiUrl}/api/orders/my`).subscribe({
      next: (data) => { this.orders.set(data); this.loading.set(false); },
      error: ()    => { this.error.set('Failed to load orders.'); this.loading.set(false); },
    });
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-IN', { day: 'numeric', month: 'short', year: 'numeric' });
  }

  remove(orderId: number) {
    this.deleting.set(orderId);
    this.http.delete(`${environment.apiUrl}/api/orders/${orderId}`).subscribe({
      next: () => {
        this.orders.update(list => list.filter(o => o.orderId !== orderId));
        this.deleting.set(null);
      },
      error: () => { this.deleting.set(null); },
    });
  }
}
