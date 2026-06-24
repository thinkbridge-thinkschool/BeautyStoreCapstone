import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../auth/services/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="page">
      <header class="page-header">
        <a routerLink="/" class="brand">beautystore</a>
        <button class="btn-logout" (click)="auth.logout()">Sign Out</button>
      </header>

      <main class="content">
        @if (user(); as u) {
          <div class="profile-card">
            <div class="avatar">{{ initial() }}</div>
            <h1>{{ u.fullName }}</h1>
            <p class="email">{{ u.email }}</p>
            <div class="roles">
              @for (role of u.roles; track role) {
                <span class="role-badge">{{ role }}</span>
              }
            </div>
          </div>
        }
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
    .content {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 40px 16px;
    }
    .profile-card {
      background: white;
      border-radius: 16px;
      padding: 48px 40px;
      text-align: center;
      max-width: 360px;
      width: 100%;
      box-shadow: 0 4px 32px rgba(224,0,122,0.08);
    }
    .avatar {
      width: 80px;
      height: 80px;
      border-radius: 50%;
      background: linear-gradient(135deg, #ff4fa3 0%, #c0006a 100%);
      color: white;
      font-size: 2rem;
      font-weight: 700;
      display: flex;
      align-items: center;
      justify-content: center;
      margin: 0 auto 20px;
    }
    h1 {
      font-family: 'Playfair Display', serif;
      font-size: 1.75rem;
      color: #1a1a2e;
      margin: 0 0 6px;
    }
    .email {
      color: #888;
      font-size: 0.95rem;
      margin: 0 0 20px;
    }
    .roles {
      display: flex;
      gap: 8px;
      justify-content: center;
      flex-wrap: wrap;
    }
    .role-badge {
      background: #fdf2f8;
      border: 1px solid #ffb3c6;
      color: #c0006a;
      border-radius: 100px;
      padding: 4px 14px;
      font-size: 0.8rem;
      font-weight: 600;
    }
  `],
})
export class ProfileComponent {
  auth = inject(AuthService);
  user = this.auth.currentUser;
  initial = () => this.user()?.fullName?.[0]?.toUpperCase() ?? '?';
}
