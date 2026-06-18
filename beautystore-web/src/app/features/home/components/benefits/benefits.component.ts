import { Component } from '@angular/core';

interface Benefit {
  icon: string;
  title: string;
  description: string;
}

@Component({
  selector: 'app-benefits',
  template: `
    <section class="benefits">
      <div class="container">
        <div class="benefits-grid">
          @for (benefit of benefits; track benefit.title) {
            <div class="benefit-card">
              <span class="benefit-icon">{{ benefit.icon }}</span>
              <h3>{{ benefit.title }}</h3>
              <p>{{ benefit.description }}</p>
            </div>
          }
        </div>
      </div>
    </section>
  `,
  styles: [`
    .benefits {
      background: #fff0f7;
      padding: 80px 0;
    }
    .container {
      max-width: 1280px;
      margin: 0 auto;
      padding: 0 24px;
    }
    .benefits-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 24px;
    }
    @media (max-width: 900px) {
      .benefits-grid { grid-template-columns: repeat(2, 1fr); }
    }
    @media (max-width: 560px) {
      .benefits-grid { grid-template-columns: 1fr; }
    }
    .benefit-card {
      text-align: center;
      padding: 40px 24px;
      background: #fff;
      border-radius: 16px;
      box-shadow: 0 2px 12px rgba(255, 79, 163, 0.06);
    }
    .benefit-icon {
      font-size: 2.5rem;
      display: block;
      margin-bottom: 16px;
    }
    h3 {
      font-size: 1rem;
      font-weight: 700;
      color: #1a1a2e;
      margin-bottom: 8px;
    }
    p {
      font-size: 0.875rem;
      color: #6b7280;
      line-height: 1.6;
    }
  `]
})
export class BenefitsComponent {
  benefits: Benefit[] = [
    { icon: '🚚', title: 'Free Shipping',    description: 'On all orders over ₹999. Delivered in 2–4 business days.' },
    { icon: '✅', title: '100% Authentic',   description: 'Every product is sourced directly from authorized distributors.' },
    { icon: '↩️', title: 'Easy Returns',     description: '30-day hassle-free returns. No questions asked.' },
    { icon: '💬', title: 'Beauty Experts',   description: 'Chat with our certified beauty advisors 7 days a week.' },
  ];
}
