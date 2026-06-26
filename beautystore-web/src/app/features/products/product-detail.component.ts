import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { AuthService } from '../../auth/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { CatalogService } from '../../core/services/catalog.service';
import { CatalogProduct } from '../../core/models/catalog.models';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [RouterLink, DecimalPipe],
  template: `
    <div class="page">

      <!-- ── Header ─────────────────────────────────────────────── -->
      <header class="page-header">
        <a routerLink="/" class="brand">beautystore</a>
        <nav class="header-nav">
          <a routerLink="/products" class="nav-link">Products</a>
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
            <button class="btn-signout" (click)="auth.logout()">Sign Out</button>
          } @else {
            <a routerLink="/login" class="btn-signin">Sign In</a>
          }
        </nav>
      </header>

      <main class="content">

        <!-- ── Breadcrumb ─────────────────────────────────────────── -->
        <nav class="breadcrumb">
          <a routerLink="/">Home</a>
          <span class="sep">›</span>
          <a routerLink="/products">Products</a>
          @if (productResource.hasValue()) {
            <span class="sep">›</span>
            <a routerLink="/products" [queryParams]="{category: productResource.value()!.categoryName}">
              {{ productResource.value()!.categoryName }}
            </a>
            <span class="sep">›</span>
            <span class="current">{{ productResource.value()!.name }}</span>
          }
        </nav>

        @if (productResource.isLoading()) {
          <!-- ── Skeleton ─────────────────────────────────────────── -->
          <div class="product-hero skeleton-hero">
            <div class="skeleton-img-lg shimmer"></div>
            <div class="skeleton-info">
              <div class="skeleton-line shimmer" style="width:40%;height:16px"></div>
              <div class="skeleton-line shimmer" style="width:75%;height:28px;margin-top:8px"></div>
              <div class="skeleton-line shimmer" style="width:55%;height:14px;margin-top:12px"></div>
              <div class="skeleton-line shimmer" style="width:30%;height:32px;margin-top:16px"></div>
              <div class="skeleton-line shimmer" style="width:90%;height:14px;margin-top:24px"></div>
              <div class="skeleton-line shimmer" style="width:80%;height:14px;margin-top:8px"></div>
              <div class="skeleton-line shimmer" style="width:65%;height:14px;margin-top:8px"></div>
              <div class="skeleton-line shimmer" style="width:100%;height:50px;border-radius:100px;margin-top:32px"></div>
            </div>
          </div>

        } @else if (productResource.error()) {
          <div class="state-box">
            <div class="state-icon">⚠️</div>
            <h2>Product not found</h2>
            <p>This product may have been removed or the link is invalid.</p>
            <a routerLink="/products" class="btn-primary">Back to Products</a>
          </div>

        } @else if (productResource.hasValue()) {
          @let p = productResource.value()!;

          <!-- ── Product hero ─────────────────────────────────────── -->
          <div class="product-hero">

            <!-- Image panel -->
            <div class="img-panel">
              <div class="main-img-wrap">
                @if (p.imageUrl) {
                  <img class="main-img" [src]="p.imageUrl" [alt]="p.name" (error)="onImgError($event)"/>
                }
                <div class="img-placeholder">{{ p.name.charAt(0) }}</div>
                @if (!p.stock) {
                  <div class="oos-overlay">Out of Stock</div>
                }
                @if (p.isFeatured) {
                  <div class="featured-ribbon">Featured</div>
                }
              </div>
            </div>

            <!-- Info panel -->
            <div class="info-panel">
              <span class="category-tag">{{ p.categoryName }}</span>
              <p class="brand">{{ p.brand }}</p>
              <h1 class="product-name">{{ p.name }}</h1>

              <!-- Rating -->
              <div class="rating-row">
                <div class="stars">
                  @for (i of [1,2,3,4,5]; track i) {
                    <span [class.filled]="i <= filledStars(p.rating)">★</span>
                  }
                </div>
                <span class="rating-num">{{ p.rating.toFixed(1) }}</span>
                <span class="rating-label">/ 5</span>
              </div>

              <!-- Price -->
              <div class="price-row">
                <span class="price">₹{{ p.price | number:'1.0-0' }}</span>
                <span class="stock-chip" [class.low]="p.stock > 0 && p.stock <= 5" [class.oos]="!p.stock">
                  @if (p.stock > 5) { ✓ In Stock }
                  @else if (p.stock > 0) { Only {{ p.stock }} left! }
                  @else { Out of Stock }
                </span>
              </div>

              <!-- Description -->
              @if (p.description) {
                <div class="description-section">
                  <h3 class="desc-title">About this product</h3>
                  <p class="desc-text">{{ p.description }}</p>
                </div>
              }

              <!-- Add to cart -->
              <div class="action-row">
                <button
                  class="btn-add-cart"
                  [class.added]="justAdded()"
                  [disabled]="!p.stock"
                  (click)="addToCart(p)">
                  @if (justAdded()) { ✓ Added to Cart! }
                  @else if (isInCart(p.id)) { + Add Again }
                  @else { Add to Cart }
                </button>
                @if (isInCart(p.id)) {
                  <a routerLink="/cart" class="btn-view-cart">View Cart →</a>
                }
              </div>

              <div class="meta-chips">
                <span class="meta-chip">🏷️ {{ p.categoryName }}</span>
                <span class="meta-chip">⭐ {{ p.rating.toFixed(1) }} rating</span>
                <span class="meta-chip">📦 {{ p.stock }} in stock</span>
              </div>
            </div>
          </div>

          <!-- ── Related Products ─────────────────────────────────── -->
          @if (p.relatedProducts.length > 0) {
            <section class="related-section">
              <div class="related-header">
                <h2 class="related-title">You Might Also Like</h2>
                <a routerLink="/products" class="related-see-all">See all {{ p.categoryName }} →</a>
              </div>
              <div class="related-grid">
                @for (rel of p.relatedProducts; track rel.id) {
                  <a class="rel-card" [routerLink]="['/products', rel.id]">
                    <div class="rel-img-wrap">
                      @if (rel.imageUrl) {
                        <img class="rel-img" [src]="rel.imageUrl" [alt]="rel.name" (error)="onImgError($event)"/>
                      }
                      <div class="rel-img-placeholder">{{ rel.name.charAt(0) }}</div>
                    </div>
                    <div class="rel-info">
                      <p class="rel-brand">{{ rel.brand }}</p>
                      <p class="rel-name">{{ rel.name }}</p>
                      <div class="rel-bottom">
                        <span class="rel-price">₹{{ rel.price | number:'1.0-0' }}</span>
                        <div class="rel-stars">
                          @for (i of [1,2,3,4,5]; track i) {
                            <span [class.filled]="i <= filledStars(rel.rating)">★</span>
                          }
                        </div>
                      </div>
                    </div>
                    <button class="rel-cart-btn" (click)="addRelated($event, rel)">
                      + Cart
                    </button>
                  </a>
                }
              </div>
            </section>
          }
        }

      </main>
    </div>
  `,
  styles: [`
    .page { min-height: 100vh; background: #fdf2f8; display: flex; flex-direction: column; }

    /* ── Header ─────────────────────────────────────────────────── */
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
    .cart-btn { position: relative; color: white; display: flex; align-items: center; text-decoration: none; }
    .cart-badge {
      position: absolute; top: -8px; right: -10px;
      background: white; color: #e0007a; border-radius: 100px;
      font-size: 0.65rem; font-weight: 800; min-width: 18px; height: 18px;
      display: flex; align-items: center; justify-content: center; padding: 0 4px;
    }
    .btn-signout {
      background: rgba(255,255,255,0.18); color: white; border: 1px solid rgba(255,255,255,0.4);
      border-radius: 100px; padding: 7px 18px; font-size: 0.85rem; font-weight: 600; cursor: pointer;
    }
    .btn-signin {
      background: white; color: #e0007a; border-radius: 100px;
      padding: 8px 20px; font-size: 0.85rem; font-weight: 700; text-decoration: none;
    }

    /* ── Breadcrumb ──────────────────────────────────────────────── */
    .breadcrumb {
      padding: 14px 40px;
      display: flex; align-items: center; gap: 6px;
      font-size: 0.82rem; background: white; border-bottom: 1px solid #f0e6f0;
    }
    .breadcrumb a { color: #ff4fa3; text-decoration: none; font-weight: 500; }
    .breadcrumb a:hover { text-decoration: underline; }
    .sep { color: #d0b8d0; }
    .current { color: #6b7280; font-weight: 500; }

    /* ── Content ─────────────────────────────────────────────────── */
    .content { flex: 1; padding: 0 0 60px; }

    /* ── Product hero ────────────────────────────────────────────── */
    .product-hero {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 0;
      max-width: 1200px;
      margin: 32px auto;
      background: white;
      border-radius: 24px;
      overflow: hidden;
      box-shadow: 0 4px 40px rgba(224,0,122,0.08);
      margin-left: 40px;
      margin-right: 40px;
    }
    @media (max-width: 900px) {
      .product-hero { grid-template-columns: 1fr; margin: 16px 20px; }
    }

    /* Image panel */
    .img-panel { background: linear-gradient(135deg, #fce4f0 0%, #fff0f7 100%); padding: 40px; display: flex; align-items: center; justify-content: center; min-height: 480px; }
    .main-img-wrap { position: relative; width: 100%; max-width: 400px; aspect-ratio: 1; border-radius: 16px; overflow: hidden; background: linear-gradient(135deg, #fce4f0, #fff0f7); display: flex; align-items: center; justify-content: center; }
    .main-img { width: 100%; height: 100%; object-fit: cover; position: absolute; inset: 0; transition: transform 0.4s; }
    .main-img-wrap:hover .main-img { transform: scale(1.04); }
    .img-placeholder { font-family: 'Playfair Display', serif; font-size: 6rem; font-weight: 700; color: #ff4fa3; opacity: 0.2; user-select: none; }
    .oos-overlay { position: absolute; inset: 0; background: rgba(0,0,0,0.42); color: white; display: flex; align-items: center; justify-content: center; font-weight: 700; font-size: 1.1rem; letter-spacing: 1px; }
    .featured-ribbon {
      position: absolute; top: 16px; left: -8px;
      background: linear-gradient(135deg, #ff4fa3, #e0007a); color: white;
      padding: 4px 16px 4px 12px; font-size: 0.72rem; font-weight: 700; letter-spacing: 0.5px;
      clip-path: polygon(0 0, 100% 0, 100% 100%, 8px 100%);
    }

    /* Info panel */
    .info-panel { padding: 48px 48px; display: flex; flex-direction: column; justify-content: center; }
    @media (max-width: 900px) { .info-panel { padding: 32px 28px; } }
    .category-tag {
      display: inline-block; background: #fff0f7; color: #e0007a; border-radius: 100px;
      padding: 3px 12px; font-size: 0.7rem; font-weight: 700; text-transform: uppercase;
      letter-spacing: 0.6px; margin-bottom: 8px; width: fit-content;
    }
    .brand { font-size: 0.8rem; font-weight: 700; text-transform: uppercase; letter-spacing: 1px; color: #ff4fa3; margin: 0 0 6px; }
    .product-name { font-family: 'Playfair Display', serif; font-size: 2rem; font-weight: 700; color: #1a1a2e; margin: 0 0 16px; line-height: 1.25; }
    @media (max-width: 900px) { .product-name { font-size: 1.5rem; } }

    .rating-row { display: flex; align-items: center; gap: 8px; margin-bottom: 20px; }
    .stars span { color: #e5e7eb; font-size: 1rem; }
    .stars span.filled { color: #f59e0b; }
    .rating-num { font-size: 0.95rem; font-weight: 700; color: #1a1a2e; }
    .rating-label { font-size: 0.85rem; color: #9ca3af; }

    .price-row { display: flex; align-items: center; gap: 16px; margin-bottom: 24px; }
    .price { font-family: 'Playfair Display', serif; font-size: 2.2rem; font-weight: 700; color: #1a1a2e; }
    .stock-chip {
      padding: 5px 14px; border-radius: 100px; font-size: 0.78rem; font-weight: 700;
      background: #f0fdf4; color: #16a34a; border: 1px solid #bbf7d0;
    }
    .stock-chip.low { background: #fffbeb; color: #d97706; border-color: #fde68a; }
    .stock-chip.oos { background: #fef2f2; color: #dc2626; border-color: #fecaca; }

    .description-section { margin-bottom: 28px; }
    .desc-title { font-size: 0.85rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px; color: #9ca3af; margin: 0 0 8px; }
    .desc-text { font-size: 0.9rem; line-height: 1.65; color: #4b5563; margin: 0; }

    .action-row { display: flex; align-items: center; gap: 12px; margin-bottom: 24px; flex-wrap: wrap; }
    .btn-add-cart {
      flex: 1; min-width: 180px; background: linear-gradient(135deg, #ff4fa3, #e0007a); color: white;
      border: none; border-radius: 100px; padding: 16px 24px;
      font-size: 1rem; font-weight: 700; cursor: pointer; font-family: inherit;
      transition: opacity 0.2s, transform 0.18s; white-space: nowrap;
    }
    .btn-add-cart:hover:not(:disabled) { opacity: 0.88; transform: scale(1.02); }
    .btn-add-cart:disabled { opacity: 0.45; cursor: not-allowed; }
    .btn-add-cart.added { background: linear-gradient(135deg, #16a34a, #15803d); }
    .btn-view-cart {
      padding: 14px 20px; border: 2px solid #ff4fa3; border-radius: 100px;
      color: #ff4fa3; font-weight: 700; font-size: 0.9rem; text-decoration: none;
      transition: all 0.18s; white-space: nowrap;
    }
    .btn-view-cart:hover { background: #fff0f7; }

    .meta-chips { display: flex; gap: 8px; flex-wrap: wrap; }
    .meta-chip {
      background: #fdf2f8; border: 1px solid #f0e6f0; border-radius: 100px;
      padding: 5px 12px; font-size: 0.78rem; color: #6b7280; font-weight: 500;
    }

    /* ── Skeletons ────────────────────────────────────────────────── */
    .skeleton-hero {
      background: white; margin-left: 40px; margin-right: 40px;
      display: grid; grid-template-columns: 1fr 1fr;
    }
    .skeleton-img-lg { height: 480px; background: #f3f4f6; }
    .skeleton-info { padding: 48px; display: flex; flex-direction: column; gap: 0; }
    .skeleton-line { border-radius: 6px; background: #f3f4f6; }
    .shimmer {
      background: linear-gradient(90deg, #f3f4f6 25%, #e9ecef 50%, #f3f4f6 75%);
      background-size: 200% 100%;
      animation: shimmer 1.4s infinite;
    }
    @keyframes shimmer { 0% { background-position: 200% 0; } 100% { background-position: -200% 0; } }

    /* ── State box ────────────────────────────────────────────────── */
    .state-box {
      background: white; border-radius: 20px; padding: 64px 40px;
      text-align: center; max-width: 480px; margin: 48px auto;
      box-shadow: 0 4px 32px rgba(224,0,122,0.06);
    }
    .state-icon { font-size: 3rem; margin-bottom: 12px; }
    .state-box h2 { font-family: 'Playfair Display', serif; font-size: 1.4rem; color: #1a1a2e; margin: 0 0 8px; }
    .state-box p { color: #6b7280; margin: 0 0 24px; }
    .btn-primary {
      display: inline-block; background: linear-gradient(135deg, #ff4fa3, #e0007a); color: white;
      border-radius: 100px; padding: 13px 28px; font-size: 0.9rem; font-weight: 700; text-decoration: none;
    }

    /* ── Related products ─────────────────────────────────────────── */
    .related-section { max-width: 1200px; margin: 0 40px; padding: 40px 0 0; }
    .related-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 24px; }
    .related-title { font-family: 'Playfair Display', serif; font-size: 1.5rem; font-weight: 700; color: #1a1a2e; margin: 0; }
    .related-see-all { font-size: 0.85rem; font-weight: 600; color: #ff4fa3; text-decoration: none; }
    .related-see-all:hover { text-decoration: underline; }
    .related-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 20px; }
    @media (max-width: 900px) { .related-grid { grid-template-columns: repeat(2, 1fr); } }
    @media (max-width: 540px) { .related-grid { grid-template-columns: 1fr; } }

    .rel-card {
      background: white; border-radius: 16px; overflow: hidden;
      box-shadow: 0 2px 16px rgba(255,79,163,0.06);
      transition: transform 0.25s, box-shadow 0.25s;
      display: flex; flex-direction: column;
      text-decoration: none; cursor: pointer; position: relative;
    }
    .rel-card:hover { transform: translateY(-5px); box-shadow: 0 10px 30px rgba(255,79,163,0.14); }
    .rel-img-wrap {
      height: 180px; background: linear-gradient(135deg, #fce4f0, #fff0f7);
      position: relative; display: flex; align-items: center; justify-content: center; overflow: hidden;
    }
    .rel-img { width: 100%; height: 100%; object-fit: cover; position: absolute; inset: 0; transition: transform 0.4s; }
    .rel-card:hover .rel-img { transform: scale(1.05); }
    .rel-img-placeholder { font-family: 'Playfair Display', serif; font-size: 3rem; font-weight: 700; color: #ff4fa3; opacity: 0.2; }
    .rel-info { padding: 14px 16px 8px; flex: 1; }
    .rel-brand { font-size: 0.68rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.7px; color: #ff4fa3; margin: 0 0 3px; }
    .rel-name { font-size: 0.85rem; font-weight: 600; color: #1a1a2e; margin: 0 0 10px; line-height: 1.3; }
    .rel-bottom { display: flex; align-items: center; justify-content: space-between; }
    .rel-price { font-family: 'Playfair Display', serif; font-size: 1rem; font-weight: 700; color: #1a1a2e; }
    .rel-stars span { font-size: 0.75rem; color: #e5e7eb; }
    .rel-stars span.filled { color: #f59e0b; }
    .rel-cart-btn {
      width: calc(100% - 32px); margin: 0 16px 14px;
      background: linear-gradient(135deg, #ff4fa3, #e0007a); color: white;
      border: none; border-radius: 100px; padding: 9px; font-size: 0.78rem; font-weight: 700;
      cursor: pointer; font-family: inherit; transition: opacity 0.2s;
    }
    .rel-cart-btn:hover { opacity: 0.88; }

    @media (max-width: 768px) {
      .page-header { padding: 14px 20px; }
      .breadcrumb { padding: 12px 20px; }
      .skeleton-hero { margin: 12px 20px; grid-template-columns: 1fr; }
      .skeleton-img-lg { height: 240px; }
      .related-section { margin: 0 20px; }
    }
  `],
})
export class ProductDetailComponent {
  auth    = inject(AuthService);
  #cart   = inject(CartService);
  #catalog = inject(CatalogService);
  #router  = inject(Router);

  #route   = inject(ActivatedRoute);

  #productId = toSignal(
    this.#route.params.pipe(map(p => +p['id']))
  );

  productResource = rxResource({
    stream: () => this.#catalog.getProductDetail(this.#productId() ?? 0),
  });

  cartCount = computed(() => this.#cart.totalItems());
  justAdded = signal(false);

  filledStars(rating: number): number {
    return Math.round(rating);
  }

  isInCart(productId: number): boolean {
    return this.#cart.items().some(i => i.productId === productId);
  }

  addToCart(product: { id: number; name: string; brand: string; price: number; imageUrl: string | null; stock: number }): void {
    if (!product.stock) return;
    this.#cart.add({
      productId: product.id,
      name:      product.name,
      brand:     product.brand,
      price:     product.price,
      imageUrl:  product.imageUrl,
    });
    this.justAdded.set(true);
    setTimeout(() => this.justAdded.set(false), 1500);
  }

  addRelated(event: Event, product: CatalogProduct): void {
    event.preventDefault();
    event.stopPropagation();
    if (!product.stock) return;
    this.#cart.add({
      productId: product.id,
      name:      product.name,
      brand:     product.brand,
      price:     product.price,
      imageUrl:  product.imageUrl,
    });
    this.#router.navigate(['/products', product.id]);
  }

  onImgError(event: Event): void {
    (event.target as HTMLImageElement).style.display = 'none';
  }
}
