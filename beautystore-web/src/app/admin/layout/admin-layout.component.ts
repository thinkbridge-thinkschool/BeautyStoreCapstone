import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { UpperCasePipe } from '@angular/common';
import { AuthService } from '../../auth/services/auth.service';

interface NavItem {
  label: string;
  icon:  string;
  path:  string;
}

@Component({
  selector: 'app-admin-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, UpperCasePipe],
  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.scss',
})
export class AdminLayoutComponent {
  #auth = inject(AuthService);

  readonly currentUser = this.#auth.currentUser;
  readonly sidebarOpen = signal(true);

  readonly navItems: NavItem[] = [
    { label: 'Dashboard',  icon: '📊', path: '/admin' },
    { label: 'Products',   icon: '🛍️',  path: '/admin/products' },
    { label: 'Categories', icon: '🗂️',  path: '/admin/categories' },
    { label: 'Orders',     icon: '📦', path: '/admin/orders' },
    { label: 'Users',      icon: '👥', path: '/admin/users' },
    { label: 'Analytics',  icon: '📈', path: '/admin/analytics' },
    { label: 'Inventory',  icon: '📦', path: '/admin/inventory' },
    { label: 'Settings',   icon: '⚙️',  path: '/admin/settings' },
  ];

  toggleSidebar() {
    this.sidebarOpen.update(v => !v);
  }

  logout() {
    this.#auth.logout();
  }
}
