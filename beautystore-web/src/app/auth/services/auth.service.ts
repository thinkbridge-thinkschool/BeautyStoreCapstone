import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AuthResponse } from '../models/auth-response';
import { CurrentUser } from '../models/current-user';
import { LoginRequest } from '../models/login-request';
import { RegisterRequest } from '../models/register-request';

const TOKEN_KEY = 'bs_token';
const USER_KEY  = 'bs_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http   = inject(HttpClient);
  private router = inject(Router);

  private _currentUser = signal<CurrentUser | null>(this.loadStoredUser());

  readonly currentUser = this._currentUser.asReadonly();
  readonly isLoggedIn  = computed(() => !!this._currentUser());

  register(req: RegisterRequest) {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/auth/register`, req)
      .pipe(tap(res => this.persist(res)));
  }

  login(req: LoginRequest) {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/auth/login`, req)
      .pipe(tap(res => this.persist(res)));
  }

  adminLogin(req: LoginRequest) {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/auth/admin/login`, req)
      .pipe(tap(res => this.persist(res)));
  }

  adminRegister(req: RegisterRequest) {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/auth/admin/register`, req)
      .pipe(tap(res => this.persist(res)));
  }

  getCurrentUser() {
    return this.http
      .get<CurrentUser>(`${environment.apiUrl}/api/auth/me`)
      .pipe(tap(user => this._currentUser.set(user)));
  }

  logout(): void {
    this.clearSession();
    this.router.navigate(['/login']);
  }

  clearSession(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._currentUser.set(null);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  private persist(res: AuthResponse): void {
    const user: CurrentUser = {
      email:    res.email,
      fullName: res.fullName,
      roles:    res.roles,
    };
    localStorage.setItem(TOKEN_KEY, res.accessToken);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this._currentUser.set(user);
  }

  private loadStoredUser(): CurrentUser | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      return raw ? (JSON.parse(raw) as CurrentUser) : null;
    } catch {
      return null;
    }
  }
}
