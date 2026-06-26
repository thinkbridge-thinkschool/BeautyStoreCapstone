import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { rxResource } from '@angular/core/rxjs-interop';
import { AuthService } from '../../auth/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { CatalogService } from '../../core/services/catalog.service';
import { CatalogProduct } from '../../core/models/catalog.models';

interface CategoryGroup {
  name: string;
  products: CatalogProduct[];
}

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [RouterLink, DecimalPipe],
  template: `
    <div class="page">

      <!-- ── Header ─────────────────────────────────────────────── -->
      <header class="page-header">
        <a routerLink="/" class="brand">beautystore</a>
        <nav class="header-nav">
          <a routerLink="/cart" class="cart-btn" title="Cart">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <circle cx="9" cy="21" r="1"/><circle cx="20" cy="21" r="1"/>
              <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"/>
            </svg>
            @if (cartCount() > 0) {
              <span class="cart-badge">{{ cartCount() }}</span>
            }
          </a>
          @if (auth.isLoggedIn()) {
            <a routerLink="/orders" class="nav-link">My Orders</a>
            <a routerLink="/profile" class="nav-link">{{ auth.currentUser()?.fullName }}</a>
            <button class="btn-signout" (click)="auth.logout()">Sign Out</button>
          } @else {
            <a routerLink="/login" class="btn-signin">Sign In</a>
          }
        </nav>
      </header>

      <!-- ── Page hero ──────────────────────────────────────────── -->
      <div class="page-hero">
        <h1>All Products</h1>
        <p>Discover beauty essentials curated for you</p>
      </div>

      <!-- ── Filters bar ────────────────────────────────────────── -->
      <div class="filters-bar">
        <div class="search-wrap">
          <svg class="search-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="11" cy="11" r="8"/><path d="M21 21l-4.35-4.35"/>
          </svg>
          <input
            class="search-input"
            type="search"
            placeholder="Search by name or brand…"
            [value]="searchQuery()"
            (input)="searchQuery.set($any($event.target).value)"
          />
        </div>

        <div class="category-pills">
          <button
            class="pill"
            [class.active]="selectedCategory() === 'all'"
            (click)="selectedCategory.set('all')">
            All
          </button>
          @for (cat of categoriesResource.value() ?? []; track cat.id) {
            <button
              class="pill"
              [class.active]="selectedCategory() === cat.name"
              (click)="selectedCategory.set(cat.name)">
              {{ cat.name }}
              <span class="pill-count">{{ cat.productCount }}</span>
            </button>
          }
        </div>
      </div>

      <!-- ── Main content ───────────────────────────────────────── -->
      <main class="content">

        @if (productsResource.isLoading()) {
          <div class="product-grid">
            @for (s of skeletons; track $index) {
              <div class="skeleton-card">
                <div class="skeleton-img shimmer"></div>
                <div class="skeleton-body">
                  <div class="skeleton-line shimmer" style="width:45%"></div>
                  <div class="skeleton-line shimmer" style="width:80%"></div>
                  <div class="skeleton-line shimmer" style="width:55%"></div>
                  <div class="skeleton-line shimmer" style="width:70%; height: 36px; border-radius: 100px;"></div>
                </div>
              </div>
            }
          </div>

        } @else if (productsResource.error()) {
          <div class="state-box">
            <div class="state-icon">⚠️</div>
            <h2>Could not load products</h2>
            <p>Please check your connection and try again.</p>
            <button class="btn-primary" (click)="productsResource.reload()">Retry</button>
          </div>

        } @else if (filteredProducts().length === 0) {
          <div class="state-box">
            <div class="state-icon">🔍</div>
            <h2>No products found</h2>
            <p>Try a different search term or category.</p>
            <button class="btn-outline" (click)="clearFilters()">Clear Filters</button>
          </div>

        } @else if (showGrouped()) {
          <!-- Products grouped by category -->
          @for (group of productsByCategory(); track group.name) {
            <section class="category-section">
              <div class="category-header">
                <h2 class="category-name">{{ group.name }}</h2>
                <span class="category-count">{{ group.products.length }} products</span>
              </div>
              <div class="product-grid">
                @for (product of group.products; track product.id) {
                  <div class="product-card">
                    <div class="card-img-wrap">
                      @if (product.imageUrl) {
                        <img class="card-img" [src]="product.imageUrl" [alt]="product.name" loading="lazy" (error)="onImgError($event)"/>
                      }
                      <div class="img-placeholder-letter">{{ product.name.charAt(0) }}</div>
                      <span class="category-badge">{{ product.categoryName }}</span>
                      @if (!product.stock) { <span class="out-of-stock-badge">Out of Stock</span> }
                    </div>
                    <div class="card-body">
                      <p class="card-brand">{{ product.brand }}</p>
                      <h3 class="card-name">{{ product.name }}</h3>
                      <div class="stars">
                        @for (i of [1,2,3,4,5]; track i) { <span [class.filled]="i <= filledStars(product.rating)">★</span> }
                        <span class="rating-val">{{ product.rating.toFixed(1) }}</span>
                      </div>
                      <div class="card-meta">
                        <span class="price">₹{{ product.price | number:'1.0-0' }}</span>
                        <span class="stock-pill" [class.low]="product.stock > 0 && product.stock <= 5">
                          @if (product.stock > 5) { In Stock }
                          @else if (product.stock > 0) { Only {{ product.stock }} left }
                          @else { Out of Stock }
                        </span>
                      </div>
                      <div class="card-actions">
                        <button class="btn-cart" [class.added]="justAdded() === product.id" [disabled]="!product.stock" (click)="addToCart(product)">
                          @if (justAdded() === product.id) { ✓ Added! }
                          @else if (isInCart(product.id)) { + Add Again }
                          @else { Add to Cart }
                        </button>
                        <a class="btn-details" [routerLink]="['/products', product.id]">Details</a>
                      </div>
                    </div>
                  </div>
                }
              </div>
            </section>
          }

        } @else {
          <!-- Flat filtered list -->
          <div class="result-bar">
            <span class="result-count">{{ filteredProducts().length }} product{{ filteredProducts().length !== 1 ? 's' : '' }}</span>
          </div>
          <div class="product-grid">
            @for (product of filteredProducts(); track product.id) {
              <div class="product-card">
                <div class="card-img-wrap">
                  @if (product.imageUrl) {
                    <img class="card-img" [src]="product.imageUrl" [alt]="product.name" loading="lazy" (error)="onImgError($event)"/>
                  }
                  <div class="img-placeholder-letter">{{ product.name.charAt(0) }}</div>
                  <span class="category-badge">{{ product.categoryName }}</span>
                  @if (!product.stock) { <span class="out-of-stock-badge">Out of Stock</span> }
                </div>
                <div class="card-body">
                  <p class="card-brand">{{ product.brand }}</p>
                  <h3 class="card-name">{{ product.name }}</h3>
                  <div class="stars">
                    @for (i of [1,2,3,4,5]; track i) { <span [class.filled]="i <= filledStars(product.rating)">★</span> }
                    <span class="rating-val">{{ product.rating.toFixed(1) }}</span>
                  </div>
                  <div class="card-meta">
                    <span class="price">₹{{ product.price | number:'1.0-0' }}</span>
                    <span class="stock-pill" [class.low]="product.stock > 0 && product.stock <= 5">
                      @if (product.stock > 5) { In Stock }
                      @else if (product.stock > 0) { Only {{ product.stock }} left }
                      @else { Out of Stock }
                    </span>
                  </div>
                  <div class="card-actions">
                    <button class="btn-cart" [class.added]="justAdded() === product.id" [disabled]="!product.stock" (click)="addToCart(product)">
                      @if (justAdded() === product.id) { ✓ Added! }
                      @else if (isInCart(product.id)) { + Add Again }
                      @else { Add to Cart }
                    </button>
                    <a class="btn-details" [routerLink]="['/products', product.id]">Details</a>
                  </div>
                </div>
              </div>
            }
          </div>
        }

      </main>
    </div>
  `,
  styles: [`
    .page { min-height: 100vh; background: #fdf2f8; display: flex; flex-direction: column; }

    .page-header {
      background: linear-gradient(135deg, #ff4fa3 0%, #c0006a 100%);
      padding: 18px 40px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      position: sticky;
      top: 0;
      z-index: 100;
      box-shadow: 0 2px 16px rgba(192,0,106,0.18);
    }
    .brand { font-family: 'Playfair Display', serif; font-size: 1.5rem; font-weight: 700; color: white; text-decoration: none; }
    .header-nav { display: flex; align-items: center; gap: 20px; }
    .nav-link { color: rgba(255,255,255,0.9); font-size: 0.9rem; font-weight: 500; text-decoration: none; transition: opacity 0.2s; }
    .nav-link:hover { opacity: 0.75; }
    .cart-btn { position: relative; color: white; display: flex; align-items: center; text-decoration: none; transition: opacity 0.2s; }
    .cart-btn:hover { opacity: 0.8; }
    .cart-badge {
      position: absolute; top: -8px; right: -10px;
      background: white; color: #e0007a;
      border-radius: 100px; font-size: 0.65rem; font-weight: 800;
      min-width: 18px; height: 18px;
      display: flex; align-items: center; justify-content: center; padding: 0 4px;
    }
    .btn-signout {
      background: rgba(255,255,255,0.18); color: white;
      border: 1px solid rgba(255,255,255,0.4); border-radius: 100px;
      padding: 7px 18px; font-size: 0.85rem; font-weight: 600; cursor: pointer;
      transition: background 0.2s;
    }
    .btn-signout:hover { background: rgba(255,255,255,0.32); }
    .btn-signin {
      background: white; color: #e0007a; border-radius: 100px;
      padding: 8px 20px; font-size: 0.85rem; font-weight: 700;
      text-decoration: none; transition: opacity 0.2s;
    }
    .btn-signin:hover { opacity: 0.9; }

    .page-hero { background: white; border-bottom: 1px solid #f0e6f0; padding: 32px 40px 28px; }
    .page-hero h1 { font-family: 'Playfair Display', serif; font-size: 2.2rem; font-weight: 700; color: #1a1a2e; margin: 0 0 6px; }
    .page-hero p { color: #6b7280; font-size: 0.95rem; margin: 0; }

    .filters-bar {
      background: white; border-bottom: 1px solid #f0e6f0;
      padding: 16px 40px; display: flex; flex-direction: column; gap: 14px;
      position: sticky; top: 64px; z-index: 90;
    }
    .search-wrap { position: relative; max-width: 420px; }
    .search-icon { position: absolute; left: 14px; top: 50%; transform: translateY(-50%); color: #9ca3af; pointer-events: none; }
    .search-input {
      width: 100%; padding: 10px 16px 10px 42px;
      border: 1.5px solid #f0e6f0; border-radius: 100px;
      font-size: 0.9rem; font-family: inherit; color: #1a1a2e; background: #fff;
      outline: none; transition: border-color 0.2s; box-sizing: border-box;
    }
    .search-input:focus { border-color: #ff4fa3; }
    .search-input::placeholder { color: #b8a8b8; }
    .category-pills { display: flex; gap: 8px; overflow-x: auto; padding-bottom: 2px; scrollbar-width: none; }
    .category-pills::-webkit-scrollbar { display: none; }
    .pill {
      flex-shrink: 0; background: #fdf2f8; border: 1.5px solid #f0e6f0; border-radius: 100px;
      padding: 6px 16px; font-size: 0.85rem; font-weight: 500; color: #6b7280;
      cursor: pointer; transition: all 0.18s; display: flex; align-items: center; gap: 6px;
    }
    .pill:hover { border-color: #ff4fa3; color: #ff4fa3; }
    .pill.active { background: linear-gradient(135deg, #ff4fa3, #e0007a); border-color: transparent; color: white; }
    .pill-count { font-size: 0.75rem; opacity: 0.8; font-weight: 600; }

    .content { flex: 1; padding: 32px 40px 60px; }
    .category-section { margin-bottom: 48px; }
    .category-header { display: flex; align-items: baseline; gap: 12px; margin-bottom: 20px; padding-bottom: 12px; border-bottom: 2px solid #f0e6f0; }
    .category-name { font-family: 'Playfair Display', serif; font-size: 1.5rem; font-weight: 700; color: #1a1a2e; margin: 0; }
    .category-count { font-size: 0.8rem; color: #9ca3af; font-weight: 500; }
    .result-bar { margin-bottom: 20px; }
    .result-count { font-size: 0.9rem; color: #6b7280; font-weight: 500; }

    .product-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 24px; }
    @media (max-width: 1200px) { .product-grid { grid-template-columns: repeat(3, 1fr); } }
    @media (max-width: 860px)  { .product-grid { grid-template-columns: repeat(2, 1fr); } }
    @media (max-width: 540px)  { .product-grid { grid-template-columns: 1fr; } }

    .product-card {
      background: white; border-radius: 16px; overflow: hidden;
      box-shadow: 0 2px 16px rgba(255,79,163,0.06);
      transition: transform 0.25s, box-shadow 0.25s;
      display: flex; flex-direction: column;
    }
    .product-card:hover { transform: translateY(-6px); box-shadow: 0 12px 36px rgba(255,79,163,0.14); }

    .card-img-wrap {
      position: relative; height: 220px;
      background: linear-gradient(135deg, #fce4f0 0%, #fff0f7 100%);
      display: flex; align-items: center; justify-content: center; overflow: hidden;
    }
    .card-img { width: 100%; height: 100%; object-fit: cover; position: absolute; inset: 0; transition: transform 0.4s; }
    .product-card:hover .card-img { transform: scale(1.05); }
    .img-placeholder-letter { font-family: 'Playfair Display', serif; font-size: 4rem; font-weight: 700; color: #ff4fa3; opacity: 0.25; user-select: none; }
    .category-badge {
      position: absolute; top: 10px; left: 10px;
      background: rgba(255,255,255,0.92); backdrop-filter: blur(8px);
      padding: 3px 10px; border-radius: 100px;
      font-size: 0.68rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px; color: #e0007a;
    }
    .out-of-stock-badge {
      position: absolute; top: 10px; right: 10px;
      background: rgba(220,38,38,0.9); color: white;
      padding: 3px 10px; border-radius: 100px; font-size: 0.68rem; font-weight: 700;
    }

    .card-body { padding: 18px 20px 20px; flex: 1; display: flex; flex-direction: column; }
    .card-brand { font-size: 0.72rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.8px; color: #ff4fa3; margin-bottom: 4px; }
    .card-name { font-size: 0.95rem; font-weight: 600; color: #1a1a2e; line-height: 1.35; margin-bottom: 10px; flex: 1; }
    .stars { display: flex; align-items: center; gap: 1px; margin-bottom: 12px; }
    .stars span { color: #e5e7eb; font-size: 0.9rem; }
    .stars span.filled { color: #f59e0b; }
    .rating-val { color: #6b7280; font-size: 0.78rem; margin-left: 4px; }
    .card-meta { display: flex; align-items: center; justify-content: space-between; margin-bottom: 14px; }
    .price { font-family: 'Playfair Display', serif; font-size: 1.15rem; font-weight: 700; color: #1a1a2e; }
    .stock-pill {
      font-size: 0.72rem; font-weight: 600;
      background: #f0fdf4; color: #16a34a; border: 1px solid #bbf7d0;
      border-radius: 100px; padding: 2px 9px;
    }
    .stock-pill.low { background: #fffbeb; color: #d97706; border-color: #fde68a; }
    .card-actions { display: flex; gap: 8px; }
    .btn-cart {
      flex: 1; background: linear-gradient(135deg, #ff4fa3, #e0007a); color: white;
      border: none; border-radius: 100px; padding: 10px 12px;
      font-size: 0.82rem; font-weight: 700; cursor: pointer;
      transition: opacity 0.18s, transform 0.18s; white-space: nowrap; font-family: inherit;
    }
    .btn-cart:hover:not(:disabled) { opacity: 0.88; transform: scale(1.02); }
    .btn-cart:disabled { opacity: 0.45; cursor: not-allowed; }
    .btn-cart.added { background: linear-gradient(135deg, #16a34a, #15803d); }
    .btn-details {
      flex-shrink: 0; border: 1.5px solid #f0e6f0; border-radius: 100px;
      padding: 10px 14px; font-size: 0.82rem; font-weight: 600; color: #6b7280;
      text-decoration: none; transition: border-color 0.18s, color 0.18s;
      white-space: nowrap; display: flex; align-items: center;
    }
    .btn-details:hover { border-color: #ff4fa3; color: #ff4fa3; }

    .skeleton-card { background: white; border-radius: 16px; overflow: hidden; box-shadow: 0 2px 12px rgba(255,79,163,0.04); }
    .skeleton-img { height: 220px; background: #f3f4f6; }
    .skeleton-body { padding: 18px 20px; display: flex; flex-direction: column; gap: 10px; }
    .skeleton-line { height: 14px; border-radius: 6px; background: #f3f4f6; }
    .shimmer {
      background: linear-gradient(90deg, #f3f4f6 25%, #e9ecef 50%, #f3f4f6 75%);
      background-size: 200% 100%;
      animation: shimmer 1.4s infinite;
    }
    @keyframes shimmer { 0% { background-position: 200% 0; } 100% { background-position: -200% 0; } }

    .state-box {
      background: white; border-radius: 20px; padding: 64px 40px;
      text-align: center; max-width: 480px; margin: 40px auto;
      box-shadow: 0 4px 32px rgba(224,0,122,0.06);
    }
    .state-icon { font-size: 3rem; margin-bottom: 12px; }
    .state-box h2 { font-family: 'Playfair Display', serif; font-size: 1.4rem; color: #1a1a2e; margin: 0 0 8px; }
    .state-box p { color: #6b7280; font-size: 0.9rem; margin: 0 0 20px; }
    .btn-primary {
      background: linear-gradient(135deg, #ff4fa3, #e0007a); color: white;
      border: none; border-radius: 100px; padding: 12px 28px;
      font-size: 0.9rem; font-weight: 700; cursor: pointer; font-family: inherit; transition: opacity 0.2s;
    }
    .btn-primary:hover { opacity: 0.88; }
    .btn-outline {
      background: transparent; color: #ff4fa3; border: 2px solid #ff4fa3; border-radius: 100px;
      padding: 11px 28px; font-size: 0.9rem; font-weight: 700; cursor: pointer; font-family: inherit; transition: all 0.2s;
    }
    .btn-outline:hover { background: #fff0f7; }

    @media (max-width: 768px) {
      .page-header { padding: 14px 20px; }
      .page-hero { padding: 24px 20px 20px; }
      .page-hero h1 { font-size: 1.75rem; }
      .filters-bar { padding: 12px 20px; top: 56px; }
      .content { padding: 24px 16px 48px; }
    }
  `],
})
export class ProductsComponent {
  auth     = inject(AuthService);
  #cart    = inject(CartService);
  #catalog = inject(CatalogService);

  selectedCategory = signal<string>('all');
  searchQuery      = signal<string>('');
  justAdded        = signal<number | null>(null);

  cartCount = computed(() => this.#cart.totalItems());

  categoriesResource = rxResource({ stream: () => this.#catalog.getCategories() });
  productsResource   = rxResource({ stream: () => this.#catalog.getProductsListing(200) });

  showGrouped = computed(() =>
    this.selectedCategory() === 'all' && !this.searchQuery().trim()
  );

  filteredProducts = computed(() => {
    const all = this.productsResource.value()?.items ?? [];
    const cat = this.selectedCategory();
    const q   = this.searchQuery().toLowerCase().trim();
    let result = cat === 'all' ? all : all.filter(p => p.categoryName === cat);
    if (q) result = result.filter(p =>
      p.name.toLowerCase().includes(q) || p.brand.toLowerCase().includes(q)
    );
    return result;
  });

  productsByCategory = computed<CategoryGroup[]>(() => {
    const products = this.productsResource.value()?.items ?? [];
    const seen = new Map<string, CatalogProduct[]>();
    for (const p of products) {
      if (!seen.has(p.categoryName)) seen.set(p.categoryName, []);
      seen.get(p.categoryName)!.push(p);
    }
    return Array.from(seen.entries()).map(([name, prods]) => ({ name, products: prods }));
  });

  skeletons = Array.from({ length: 8 });

  filledStars(rating: number): number {
    return Math.round(rating);
  }

  isInCart(productId: number): boolean {
    return this.#cart.items().some(i => i.productId === productId);
  }

  addToCart(product: CatalogProduct): void {
    this.#cart.add({
      productId: product.id,
      name:      product.name,
      brand:     product.brand,
      price:     product.price,
      imageUrl:  product.imageUrl,
    });
    this.justAdded.set(product.id);
    setTimeout(() => {
      if (this.justAdded() === product.id) this.justAdded.set(null);
    }, 1400);
  }

  clearFilters(): void {
    this.selectedCategory.set('all');
    this.searchQuery.set('');
  }

  onImgError(event: Event): void {
    (event.target as HTMLImageElement).style.display = 'none';
  }
}
