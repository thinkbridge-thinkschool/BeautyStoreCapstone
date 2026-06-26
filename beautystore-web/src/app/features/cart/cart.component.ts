import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../auth/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { OrderService } from '../../core/services/order.service';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [RouterLink, DecimalPipe],
  template: `
    <div class="page">

      <!-- ── Header ─────────────────────────────────────────────── -->
      <header class="page-header">
        <a routerLink="/" class="brand">beautystore</a>
        <nav class="header-nav">
          <a routerLink="/products" class="nav-link">Products</a>
          @if (auth.isLoggedIn()) {
            <a routerLink="/orders" class="nav-link">My Orders</a>
            <button class="btn-signout" (click)="auth.logout()">Sign Out</button>
          } @else {
            <a routerLink="/login" class="btn-signin">Sign In</a>
          }
        </nav>
      </header>

      <main class="content">
        <div class="container">

          <div class="page-title">
            <h1>
              Shopping Cart
              @if (cart.totalItems() > 0) {
                <span class="item-count">{{ cart.totalItems() }} item{{ cart.totalItems() !== 1 ? 's' : '' }}</span>
              }
            </h1>
            <a routerLink="/products" class="btn-continue">← Continue Shopping</a>
          </div>

          @if (cart.items().length === 0) {
            <!-- Empty cart -->
            <div class="state-box">
              <div class="empty-icon">🛒</div>
              <h2>Your cart is empty</h2>
              <p>Browse our products and add something beautiful.</p>
              <a routerLink="/products" class="btn-primary">Browse Products</a>
            </div>

          } @else {
            <div class="cart-layout">

              <!-- Items list -->
              <div class="cart-items">
                @for (item of cart.items(); track item.productId) {
                  <div class="cart-item">
                    <div class="item-img-wrap">
                      @if (item.imageUrl) {
                        <img class="item-img" [src]="item.imageUrl" [alt]="item.name" (error)="onImgError($event)"/>
                      }
                      <div class="img-fallback">{{ item.name.charAt(0) }}</div>
                    </div>

                    <div class="item-info">
                      <p class="item-brand">{{ item.brand }}</p>
                      <h3 class="item-name">{{ item.name }}</h3>
                      <p class="item-unit-price">₹{{ item.price | number:'1.0-0' }} each</p>
                    </div>

                    <div class="item-controls">
                      <div class="qty-row">
                        <button class="qty-btn" (click)="cart.decreaseQty(item.productId)" title="Decrease">−</button>
                        <span class="qty-val">{{ item.quantity }}</span>
                        <button class="qty-btn" (click)="cart.increaseQty(item.productId)" title="Increase">+</button>
                      </div>
                      <p class="item-subtotal">₹{{ (item.price * item.quantity) | number:'1.0-0' }}</p>
                    </div>

                    <button class="btn-remove" (click)="cart.remove(item.productId)" title="Remove">✕</button>
                  </div>
                }
              </div>

              <!-- Summary panel -->
              <div class="summary-panel">
                <h2 class="summary-title">Order Summary</h2>

                <div class="summary-rows">
                  @for (item of cart.items(); track item.productId) {
                    <div class="summary-row">
                      <span class="summary-item-name">{{ item.name }} × {{ item.quantity }}</span>
                      <span>₹{{ (item.price * item.quantity) | number:'1.0-0' }}</span>
                    </div>
                  }
                </div>

                <div class="summary-divider"></div>

                <div class="summary-total-row">
                  <span class="summary-total-label">Total</span>
                  <span class="summary-total-val">₹{{ cart.grandTotal() | number:'1.0-0' }}</span>
                </div>

                @if (errorMsg()) {
                  <div class="error-box">{{ errorMsg() }}</div>
                }

                <button
                  class="btn-checkout"
                  [disabled]="placing()"
                  (click)="checkout()">
                  @if (placing()) {
                    <span class="spinner"></span> Placing Orders…
                  } @else {
                    Place Order
                  }
                </button>

                <p class="checkout-note">
                  @if (!auth.isLoggedIn()) {
                    You'll be asked to sign in before placing your order.
                  } @else {
                    {{ cart.items().length }} product{{ cart.items().length !== 1 ? 's' : '' }} will be ordered separately.
                  }
                </p>

                <button class="btn-clear" (click)="cart.clear()">Clear Cart</button>
              </div>

            </div>
          }

        </div>
      </main>
    </div>
  `,
  styles: [`
    .page { min-height: 100vh; background: #fdf2f8; display: flex; flex-direction: column; }

    .page-header {
      background: linear-gradient(135deg, #ff4fa3 0%, #c0006a 100%);
      padding: 18px 40px;
      display: flex; align-items: center; justify-content: space-between;
      position: sticky; top: 0; z-index: 100;
      box-shadow: 0 2px 16px rgba(192,0,106,0.18);
    }
    .brand { font-family: 'Playfair Display', serif; font-size: 1.5rem; font-weight: 700; color: white; text-decoration: none; }
    .header-nav { display: flex; align-items: center; gap: 20px; }
    .nav-link { color: rgba(255,255,255,0.9); font-size: 0.9rem; font-weight: 500; text-decoration: none; transition: opacity 0.2s; }
    .nav-link:hover { opacity: 0.75; }
    .btn-signout {
      background: rgba(255,255,255,0.18); color: white;
      border: 1px solid rgba(255,255,255,0.4); border-radius: 100px;
      padding: 7px 18px; font-size: 0.85rem; font-weight: 600; cursor: pointer; transition: background 0.2s;
    }
    .btn-signout:hover { background: rgba(255,255,255,0.32); }
    .btn-signin {
      background: white; color: #e0007a; border-radius: 100px;
      padding: 8px 20px; font-size: 0.85rem; font-weight: 700; text-decoration: none; transition: opacity 0.2s;
    }
    .btn-signin:hover { opacity: 0.9; }

    .content { flex: 1; padding: 40px 16px 60px; }
    .container { max-width: 1100px; margin: 0 auto; }
    .page-title { display: flex; align-items: center; justify-content: space-between; margin-bottom: 32px; flex-wrap: wrap; gap: 12px; }
    h1 { font-family: 'Playfair Display', serif; font-size: 2rem; color: #1a1a2e; margin: 0; display: flex; align-items: center; gap: 12px; }
    .item-count {
      font-family: 'Inter', sans-serif; font-size: 0.9rem; font-weight: 600;
      background: #fff0f7; color: #e0007a; border-radius: 100px; padding: 4px 12px;
    }
    .btn-continue {
      font-size: 0.875rem; font-weight: 600; color: #ff4fa3; text-decoration: none; transition: opacity 0.2s;
    }
    .btn-continue:hover { opacity: 0.75; }

    /* ── Empty state ─────────────────────────────────────────────── */
    .state-box {
      background: white; border-radius: 20px; padding: 64px 40px;
      text-align: center; max-width: 480px; margin: 0 auto;
      box-shadow: 0 4px 32px rgba(224,0,122,0.06);
    }
    .empty-icon { font-size: 3.5rem; margin-bottom: 12px; }
    .state-box h2 { font-family: 'Playfair Display', serif; font-size: 1.5rem; color: #1a1a2e; margin: 0 0 8px; }
    .state-box p { color: #6b7280; margin: 0 0 24px; font-size: 0.9rem; }
    .btn-primary {
      display: inline-block; background: linear-gradient(135deg, #ff4fa3, #e0007a); color: white;
      border-radius: 100px; padding: 13px 32px; font-size: 0.9rem; font-weight: 700;
      text-decoration: none; transition: opacity 0.2s;
    }
    .btn-primary:hover { opacity: 0.88; }

    /* ── Cart layout ─────────────────────────────────────────────── */
    .cart-layout {
      display: grid;
      grid-template-columns: 1fr 360px;
      gap: 32px;
      align-items: start;
    }
    @media (max-width: 860px) {
      .cart-layout { grid-template-columns: 1fr; }
    }

    /* ── Cart items ──────────────────────────────────────────────── */
    .cart-items { display: flex; flex-direction: column; gap: 16px; }
    .cart-item {
      background: white; border-radius: 16px; padding: 20px 24px;
      display: flex; align-items: center; gap: 20px;
      box-shadow: 0 2px 16px rgba(224,0,122,0.06);
      transition: box-shadow 0.2s;
    }
    .cart-item:hover { box-shadow: 0 4px 24px rgba(224,0,122,0.1); }

    .item-img-wrap {
      width: 80px; height: 80px; flex-shrink: 0; border-radius: 12px;
      background: linear-gradient(135deg, #fce4f0, #fff0f7);
      position: relative; overflow: hidden;
      display: flex; align-items: center; justify-content: center;
    }
    .item-img { width: 100%; height: 100%; object-fit: cover; position: absolute; inset: 0; }
    .img-fallback { font-family: 'Playfair Display', serif; font-size: 2rem; font-weight: 700; color: #ff4fa3; opacity: 0.3; }

    .item-info { flex: 1; min-width: 0; }
    .item-brand { font-size: 0.7rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.8px; color: #ff4fa3; margin: 0 0 2px; }
    .item-name { font-size: 0.95rem; font-weight: 600; color: #1a1a2e; margin: 0 0 4px; line-height: 1.3; }
    .item-unit-price { font-size: 0.8rem; color: #9ca3af; margin: 0; }

    .item-controls { flex-shrink: 0; display: flex; flex-direction: column; align-items: center; gap: 6px; }
    .qty-row { display: flex; align-items: center; gap: 10px; }
    .qty-btn {
      width: 30px; height: 30px; border-radius: 50%;
      border: 1.5px solid #f0e6f0; background: #fdf2f8; color: #e0007a;
      font-size: 1.1rem; font-weight: 700; cursor: pointer; display: flex; align-items: center; justify-content: center;
      transition: background 0.18s, border-color 0.18s; line-height: 1;
    }
    .qty-btn:hover { background: #fff0f7; border-color: #ff4fa3; }
    .qty-val { font-size: 1rem; font-weight: 700; color: #1a1a2e; min-width: 20px; text-align: center; }
    .item-subtotal { font-family: 'Playfair Display', serif; font-size: 1rem; font-weight: 700; color: #1a1a2e; margin: 0; }

    .btn-remove {
      width: 32px; height: 32px; border-radius: 50%; border: 1px solid #fecdd3;
      background: #fff0f3; color: #e11d48; font-size: 0.75rem; font-weight: 700;
      cursor: pointer; display: flex; align-items: center; justify-content: center;
      transition: background 0.18s; flex-shrink: 0;
    }
    .btn-remove:hover { background: #ffe4e6; }

    /* ── Summary panel ───────────────────────────────────────────── */
    .summary-panel {
      background: white; border-radius: 20px; padding: 28px 28px 24px;
      box-shadow: 0 4px 32px rgba(224,0,122,0.08);
      position: sticky; top: 90px;
    }
    .summary-title { font-family: 'Playfair Display', serif; font-size: 1.3rem; font-weight: 700; color: #1a1a2e; margin: 0 0 20px; }
    .summary-rows { display: flex; flex-direction: column; gap: 10px; margin-bottom: 16px; }
    .summary-row { display: flex; justify-content: space-between; align-items: start; gap: 12px; font-size: 0.83rem; color: #6b7280; }
    .summary-item-name { flex: 1; }
    .summary-divider { height: 1px; background: #f0e6f0; margin: 4px 0 16px; }
    .summary-total-row { display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }
    .summary-total-label { font-size: 1rem; font-weight: 700; color: #1a1a2e; }
    .summary-total-val { font-family: 'Playfair Display', serif; font-size: 1.4rem; font-weight: 700; color: #e0007a; }

    .error-box {
      background: #fff0f3; border: 1px solid #fecdd3; border-radius: 10px;
      padding: 12px 16px; font-size: 0.83rem; color: #e11d48;
      margin-bottom: 14px; font-weight: 500;
    }

    .btn-checkout {
      width: 100%; background: linear-gradient(135deg, #ff4fa3, #e0007a); color: white;
      border: none; border-radius: 100px; padding: 15px; font-size: 0.95rem; font-weight: 700;
      cursor: pointer; font-family: inherit; transition: opacity 0.2s;
      display: flex; align-items: center; justify-content: center; gap: 8px;
    }
    .btn-checkout:hover:not(:disabled) { opacity: 0.88; }
    .btn-checkout:disabled { opacity: 0.6; cursor: not-allowed; }
    .spinner {
      width: 16px; height: 16px; border: 2px solid rgba(255,255,255,0.4); border-top-color: white;
      border-radius: 50%; animation: spin 0.7s linear infinite; flex-shrink: 0;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    .checkout-note { font-size: 0.75rem; color: #9ca3af; text-align: center; margin: 10px 0 16px; }

    .btn-clear {
      width: 100%; background: transparent; color: #9ca3af; border: 1.5px solid #f0e6f0;
      border-radius: 100px; padding: 10px; font-size: 0.82rem; font-weight: 600;
      cursor: pointer; font-family: inherit; transition: all 0.18s;
    }
    .btn-clear:hover { border-color: #fecdd3; color: #e11d48; }

    @media (max-width: 768px) {
      .page-header { padding: 14px 20px; }
      .content { padding: 24px 12px 48px; }
      .cart-item { flex-wrap: wrap; gap: 12px; }
    }
  `],
})
export class CartComponent {
  auth    = inject(AuthService);
  cart    = inject(CartService);
  #orders = inject(OrderService);
  #router = inject(Router);

  placing  = signal(false);
  errorMsg = signal('');

  async checkout(): Promise<void> {
    if (!this.auth.isLoggedIn()) {
      this.#router.navigate(['/login']);
      return;
    }
    this.placing.set(true);
    this.errorMsg.set('');
    try {
      const items = this.cart.items();
      for (const item of items) {
        await firstValueFrom(this.#orders.placeOrder(item.productId, item.quantity));
      }
      this.cart.clear();
      this.#router.navigate(['/orders']);
    } catch {
      this.errorMsg.set('Something went wrong placing your order. Please try again.');
    } finally {
      this.placing.set(false);
    }
  }

  onImgError(event: Event): void {
    (event.target as HTMLImageElement).style.display = 'none';
  }
}
