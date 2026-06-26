import { Routes } from '@angular/router';
import { authGuard }  from './auth/guards/auth.guard';
import { adminGuard } from './admin/guards/admin.guard';

export const routes: Routes = [
  // ── Customer routes ──────────────────────────────────────────────────────────
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent)
  },
  {
    path: 'login',
    loadComponent: () => import('./auth/pages/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./auth/pages/register/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'profile',
    loadComponent: () => import('./features/profile/profile.component').then(m => m.ProfileComponent),
    canActivate: [authGuard]
  },
  {
    path: 'products',
    loadComponent: () => import('./features/products/products.component').then(m => m.ProductsComponent)
  },
  {
    path: 'products/:id',
    loadComponent: () => import('./features/products/product-detail.component').then(m => m.ProductDetailComponent)
  },
  {
    path: 'cart',
    loadComponent: () => import('./features/cart/cart.component').then(m => m.CartComponent)
  },
  {
    path: 'orders',
    loadComponent: () => import('./features/orders/my-orders.component').then(m => m.MyOrdersComponent),
    canActivate: [authGuard]
  },
  {
    path: 'profile/orders',
    loadComponent: () => import('./features/orders/my-orders.component').then(m => m.MyOrdersComponent),
    canActivate: [authGuard]
  },

  // ── Admin login (no guard — public entry point for admins) ───────────────────
  {
    path: 'admin/login',
    loadComponent: () => import('./admin/pages/admin-login/admin-login.component').then(m => m.AdminLoginComponent)
  },

  // ── Admin routes (AdminGuard: must be logged in + Admin role) ─────────────────
  {
    path: 'admin',
    loadComponent: () => import('./admin/layout/admin-layout.component').then(m => m.AdminLayoutComponent),
    canActivate: [adminGuard],
    children: [
      {
        path: '',
        loadComponent: () => import('./admin/pages/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'products',
        loadComponent: () => import('./admin/pages/products/products.component').then(m => m.ProductsComponent)
      },
      {
        path: 'categories',
        loadComponent: () => import('./admin/pages/categories/categories.component').then(m => m.CategoriesComponent)
      },
      {
        path: 'orders',
        loadComponent: () => import('./admin/pages/orders/orders.component').then(m => m.OrdersComponent)
      },
      {
        path: 'users',
        loadComponent: () => import('./admin/pages/users/users.component').then(m => m.UsersComponent)
      },
      {
        path: 'analytics',
        loadComponent: () => import('./admin/pages/analytics/analytics.component').then(m => m.AnalyticsComponent)
      },
      {
        path: 'inventory',
        loadComponent: () => import('./admin/pages/inventory/inventory.component').then(m => m.InventoryComponent)
      },
      {
        path: 'settings',
        loadComponent: () => import('./admin/pages/settings/settings.component').then(m => m.SettingsComponent)
      },
    ]
  },
];
