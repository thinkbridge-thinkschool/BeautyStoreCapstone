import { Injectable, computed, signal } from '@angular/core';
import { CartItem } from '../models/cart.model';

const CART_KEY = 'bs_cart';

@Injectable({ providedIn: 'root' })
export class CartService {
  readonly items = signal<CartItem[]>(this.#load());

  readonly totalItems  = computed(() => this.items().reduce((sum, i) => sum + i.quantity, 0));
  readonly grandTotal  = computed(() => this.items().reduce((sum, i) => sum + i.price * i.quantity, 0));

  add(item: Omit<CartItem, 'quantity'>): void {
    this.items.update(cart => {
      const idx = cart.findIndex(i => i.productId === item.productId);
      if (idx !== -1) {
        return cart.map((i, n) => n === idx ? { ...i, quantity: i.quantity + 1 } : i);
      }
      return [...cart, { ...item, quantity: 1 }];
    });
    this.#save();
  }

  remove(productId: number): void {
    this.items.update(cart => cart.filter(i => i.productId !== productId));
    this.#save();
  }

  increaseQty(productId: number): void {
    this.items.update(cart =>
      cart.map(i => i.productId === productId ? { ...i, quantity: i.quantity + 1 } : i)
    );
    this.#save();
  }

  decreaseQty(productId: number): void {
    this.items.update(cart => {
      const item = cart.find(i => i.productId === productId);
      if (!item || item.quantity <= 1) return cart.filter(i => i.productId !== productId);
      return cart.map(i => i.productId === productId ? { ...i, quantity: i.quantity - 1 } : i);
    });
    this.#save();
  }

  clear(): void {
    this.items.set([]);
    this.#save();
  }

  #save(): void {
    try {
      localStorage.setItem(CART_KEY, JSON.stringify(this.items()));
    } catch { /* storage full or unavailable */ }
  }

  #load(): CartItem[] {
    try {
      const raw = localStorage.getItem(CART_KEY);
      return raw ? (JSON.parse(raw) as CartItem[]) : [];
    } catch {
      return [];
    }
  }
}
