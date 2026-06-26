import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../auth/services/auth.service';

@Component({
  selector: 'app-admin-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="page">
      <header class="page-header">
        <span class="brand">beautystore</span>
        <span class="admin-badge">Admin Portal</span>
      </header>

      <main class="card-wrap">
        <div class="card">
          <div class="card-icon">🔐</div>
          <h1>Admin Access</h1>

          <div class="tabs">
            <button
              class="tab"
              [class.active]="tab() === 'login'"
              (click)="tab.set('login')">
              Sign In
            </button>
            <button
              class="tab"
              [class.active]="tab() === 'setup'"
              (click)="tab.set('setup')">
              First Setup
            </button>
          </div>

          @if (error()) {
            <div class="error-banner">{{ error() }}</div>
          }
          @if (success()) {
            <div class="success-banner">{{ success() }}</div>
          }

          @if (tab() === 'login') {
            <form [formGroup]="loginForm" (ngSubmit)="submitLogin()">
              <div class="field">
                <label for="l-email">Email</label>
                <input
                  id="l-email"
                  type="email"
                  formControlName="email"
                  autocomplete="email"
                  placeholder="admin@example.com"
                  [class.invalid]="loginSubmitted && loginForm.controls.email.invalid"
                />
                @if (loginSubmitted && loginForm.controls.email.hasError('required')) {
                  <span class="field-error">Email is required.</span>
                }
              </div>
              <div class="field">
                <label for="l-password">Password</label>
                <input
                  id="l-password"
                  type="password"
                  formControlName="password"
                  autocomplete="current-password"
                  placeholder="••••••••"
                  [class.invalid]="loginSubmitted && loginForm.controls.password.invalid"
                />
                @if (loginSubmitted && loginForm.controls.password.hasError('required')) {
                  <span class="field-error">Password is required.</span>
                }
              </div>
              <button type="submit" class="btn-primary" [disabled]="loading()">
                {{ loading() ? 'Signing in…' : 'Sign In' }}
              </button>
            </form>
          }

          @if (tab() === 'setup') {
            <p class="setup-note">
              Create the first admin account. This form is disabled once an admin exists.
            </p>
            <form [formGroup]="setupForm" (ngSubmit)="submitSetup()">
              <div class="field">
                <label for="s-name">Full Name</label>
                <input
                  id="s-name"
                  type="text"
                  formControlName="fullName"
                  placeholder="Admin User"
                  [class.invalid]="setupSubmitted && setupForm.controls.fullName.invalid"
                />
                @if (setupSubmitted && setupForm.controls.fullName.hasError('required')) {
                  <span class="field-error">Full name is required.</span>
                }
              </div>
              <div class="field">
                <label for="s-email">Email</label>
                <input
                  id="s-email"
                  type="email"
                  formControlName="email"
                  placeholder="admin@example.com"
                  [class.invalid]="setupSubmitted && setupForm.controls.email.invalid"
                />
                @if (setupSubmitted && setupForm.controls.email.hasError('required')) {
                  <span class="field-error">Email is required.</span>
                }
              </div>
              <div class="field">
                <label for="s-password">Password</label>
                <input
                  id="s-password"
                  type="password"
                  formControlName="password"
                  placeholder="Min 8 chars, 1 digit"
                  [class.invalid]="setupSubmitted && setupForm.controls.password.invalid"
                />
                @if (setupSubmitted && setupForm.controls.password.hasError('required')) {
                  <span class="field-error">Password is required.</span>
                }
                @if (setupSubmitted && setupForm.controls.password.hasError('minlength')) {
                  <span class="field-error">Minimum 8 characters required.</span>
                }
                @if (setupSubmitted && setupForm.controls.password.hasError('pattern')) {
                  <span class="field-error">Must contain at least one digit (0–9).</span>
                }
              </div>
              <button type="submit" class="btn-primary" [disabled]="loading()">
                {{ loading() ? 'Creating…' : 'Create Admin Account' }}
              </button>
            </form>
          }
        </div>
      </main>
    </div>
  `,
  styles: [`
    .page {
      min-height: 100vh;
      background: #0f1117;
      display: flex;
      flex-direction: column;
    }
    .page-header {
      background: #1a1f2e;
      border-bottom: 1px solid #2a3050;
      padding: 18px 48px;
      display: flex;
      align-items: center;
      gap: 12px;
    }
    .brand {
      font-family: 'Playfair Display', serif;
      font-size: 1.4rem;
      font-weight: 700;
      color: #ff4fa3;
    }
    .admin-badge {
      background: #1e2a4a;
      color: #6b8cf5;
      border: 1px solid #2d3f70;
      border-radius: 6px;
      font-size: 0.75rem;
      font-weight: 600;
      letter-spacing: .06em;
      padding: 3px 10px;
      text-transform: uppercase;
    }
    .card-wrap {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 40px 16px;
    }
    .card {
      background: #1a1f2e;
      border: 1px solid #2a3050;
      border-radius: 16px;
      padding: 48px 40px;
      width: 100%;
      max-width: 420px;
      box-shadow: 0 8px 48px rgba(0,0,0,0.5);
    }
    .card-icon {
      font-size: 2rem;
      margin-bottom: 8px;
    }
    h1 {
      font-size: 1.75rem;
      font-weight: 700;
      color: #e8eeff;
      margin: 0 0 24px;
    }
    .tabs {
      display: flex;
      gap: 4px;
      background: #0f1117;
      border-radius: 8px;
      padding: 4px;
      margin-bottom: 24px;
    }
    .tab {
      flex: 1;
      padding: 8px;
      border: none;
      border-radius: 6px;
      background: none;
      color: #5d6b8f;
      font-size: 0.875rem;
      font-weight: 600;
      cursor: pointer;
      transition: all .15s;
    }
    .tab.active {
      background: #1e2a4a;
      color: #6b8cf5;
    }
    .setup-note {
      font-size: 0.85rem;
      color: #5d6b8f;
      background: #0f1117;
      border-radius: 8px;
      padding: 10px 14px;
      margin-bottom: 20px;
      line-height: 1.5;
    }
    .error-banner {
      background: #2a1020;
      border: 1px solid #6b1a3a;
      color: #f08;
      border-radius: 8px;
      padding: 12px 16px;
      font-size: 0.875rem;
      margin-bottom: 20px;
    }
    .success-banner {
      background: #0c2118;
      border: 1px solid #1a5c38;
      color: #2ec47a;
      border-radius: 8px;
      padding: 12px 16px;
      font-size: 0.875rem;
      margin-bottom: 20px;
    }
    .field {
      display: flex;
      flex-direction: column;
      gap: 6px;
      margin-bottom: 18px;
    }
    label {
      font-size: 0.8rem;
      font-weight: 600;
      color: #8896b8;
      letter-spacing: .04em;
      text-transform: uppercase;
    }
    input {
      padding: 12px 14px;
      border: 1.5px solid #2a3050;
      border-radius: 8px;
      font-size: 0.95rem;
      outline: none;
      background: #0f1117;
      color: #ccd4ee;
      transition: border-color .2s;
    }
    input::placeholder { color: #3a4560; }
    input:focus { border-color: #4d8cf5; }
    input.invalid { border-color: #c05050; }
    .field-error { font-size: 0.78rem; color: #f08080; }
    .btn-primary {
      width: 100%;
      padding: 13px;
      background: linear-gradient(135deg, #4d8cf5 0%, #2050c0 100%);
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 0.95rem;
      font-weight: 700;
      cursor: pointer;
      margin-top: 4px;
      transition: opacity .2s, transform .2s;
    }
    .btn-primary:hover:not(:disabled) { opacity: .9; transform: translateY(-1px); }
    .btn-primary:disabled { opacity: .5; cursor: not-allowed; }
  `],
})
export class AdminLoginComponent {
  private fb     = inject(FormBuilder);
  private auth   = inject(AuthService);
  private router = inject(Router);

  tab     = signal<'login' | 'setup'>('login');
  error   = signal('');
  success = signal('');
  loading = signal(false);

  loginSubmitted = false;
  setupSubmitted = false;

  loginForm = this.fb.nonNullable.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  setupForm = this.fb.nonNullable.group({
    fullName: ['', Validators.required],
    email:    ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.pattern(/\d/)]],
  });

  submitLogin() {
    this.loginSubmitted = true;
    if (this.loginForm.invalid) return;

    this.loading.set(true);
    this.error.set('');

    this.auth.adminLogin(this.loginForm.getRawValue()).subscribe({
      next: () => this.router.navigate(['/admin']),
      error: (err) => {
        const status = err?.status;
        this.error.set(
          status === 403
            ? 'This account does not have administrator privileges.'
            : 'Invalid email or password.'
        );
        this.loading.set(false);
      },
    });
  }

  submitSetup() {
    this.setupSubmitted = true;
    if (this.setupForm.invalid) return;

    this.loading.set(true);
    this.error.set('');
    this.success.set('');

    this.auth.adminRegister(this.setupForm.getRawValue()).subscribe({
      next: () => this.router.navigate(['/admin']),
      error: (err) => {
        const status = err?.status;
        this.error.set(
          status === 409
            ? 'An admin account already exists. Use the Sign In tab.'
            : 'Failed to create admin account. Check password requirements.'
        );
        this.loading.set(false);
      },
    });
  }
}
