import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="page">
      <header class="page-header">
        <a routerLink="/" class="brand">beautystore</a>
      </header>

      <main class="card-wrap">
        <div class="card">
          <h1>Create Account</h1>
          <p class="subtitle">Join the beautystore community</p>

          @if (error()) {
            <div class="error-banner">{{ error() }}</div>
          }

          <form [formGroup]="form" (ngSubmit)="submit()">
            <div class="field">
              <label for="fullName">Full Name</label>
              <input
                id="fullName"
                type="text"
                formControlName="fullName"
                autocomplete="name"
                placeholder="Jane Smith"
                [class.invalid]="submitted && form.controls.fullName.invalid"
              />
              @if (submitted && form.controls.fullName.hasError('required')) {
                <span class="field-error">Full name is required.</span>
              }
            </div>

            <div class="field">
              <label for="email">Email</label>
              <input
                id="email"
                type="email"
                formControlName="email"
                autocomplete="email"
                placeholder="you@example.com"
                [class.invalid]="submitted && form.controls.email.invalid"
              />
              @if (submitted && form.controls.email.hasError('required')) {
                <span class="field-error">Email is required.</span>
              }
              @if (submitted && form.controls.email.hasError('email')) {
                <span class="field-error">Enter a valid email address.</span>
              }
            </div>

            <div class="field">
              <label for="password">Password</label>
              <input
                id="password"
                type="password"
                formControlName="password"
                autocomplete="new-password"
                placeholder="At least 8 characters"
                [class.invalid]="submitted && form.controls.password.invalid"
              />
              @if (submitted && form.controls.password.hasError('required')) {
                <span class="field-error">Password is required.</span>
              }
              @if (submitted && form.controls.password.hasError('minlength')) {
                <span class="field-error">Password must be at least 8 characters.</span>
              }
            </div>

            <button type="submit" class="btn-primary" [disabled]="loading()">
              {{ loading() ? 'Creating account…' : 'Create Account' }}
            </button>
          </form>

          <p class="alt-link">
            Already have an account? <a routerLink="/login">Sign in</a>
          </p>
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
    }
    .brand {
      font-family: 'Playfair Display', serif;
      font-size: 1.5rem;
      font-weight: 700;
      color: white;
      text-decoration: none;
    }
    .card-wrap {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 40px 16px;
    }
    .card {
      background: white;
      border-radius: 16px;
      padding: 48px 40px;
      width: 100%;
      max-width: 420px;
      box-shadow: 0 4px 32px rgba(224,0,122,0.08);
    }
    h1 {
      font-family: 'Playfair Display', serif;
      font-size: 2rem;
      color: #1a1a2e;
      margin: 0 0 4px;
    }
    .subtitle {
      color: #888;
      font-size: 0.95rem;
      margin: 0 0 28px;
    }
    .error-banner {
      background: #fff0f3;
      border: 1px solid #ffb3c6;
      color: #c0006a;
      border-radius: 8px;
      padding: 12px 16px;
      font-size: 0.9rem;
      margin-bottom: 20px;
    }
    .field {
      display: flex;
      flex-direction: column;
      gap: 6px;
      margin-bottom: 20px;
    }
    label {
      font-size: 0.875rem;
      font-weight: 600;
      color: #1a1a2e;
    }
    input {
      padding: 12px 16px;
      border: 1.5px solid #e8e8f0;
      border-radius: 10px;
      font-size: 1rem;
      outline: none;
      transition: border-color 0.2s;
      background: #fafafa;
    }
    input:focus { border-color: #ff4fa3; background: white; }
    input.invalid { border-color: #f97316; }
    .field-error {
      font-size: 0.8rem;
      color: #f97316;
    }
    .btn-primary {
      width: 100%;
      padding: 14px;
      background: linear-gradient(135deg, #ff4fa3 0%, #c0006a 100%);
      color: white;
      border: none;
      border-radius: 100px;
      font-size: 1rem;
      font-weight: 700;
      cursor: pointer;
      margin-top: 8px;
      transition: opacity 0.2s, transform 0.2s;
    }
    .btn-primary:hover:not(:disabled) {
      opacity: 0.9;
      transform: translateY(-1px);
    }
    .btn-primary:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }
    .alt-link {
      text-align: center;
      margin-top: 24px;
      font-size: 0.9rem;
      color: #666;
    }
    .alt-link a {
      color: #e0007a;
      font-weight: 600;
      text-decoration: none;
    }
    .alt-link a:hover { text-decoration: underline; }
  `],
})
export class RegisterComponent {
  private fb     = inject(FormBuilder);
  private auth   = inject(AuthService);
  private router = inject(Router);

  form = this.fb.nonNullable.group({
    fullName: ['', Validators.required],
    email:    ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  error     = signal('');
  loading   = signal(false);
  submitted = false;

  submit() {
    this.submitted = true;
    if (this.form.invalid) return;

    this.loading.set(true);
    this.error.set('');

    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => this.router.navigate(['/']),
      error: (err) => {
        const msg = err?.error?.message ?? err?.error ?? 'Registration failed. Try a different email.';
        this.error.set(typeof msg === 'string' ? msg : 'Registration failed.');
        this.loading.set(false);
      },
    });
  }
}
