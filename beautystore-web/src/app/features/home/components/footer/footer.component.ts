import { Component } from '@angular/core';

@Component({
  selector: 'app-footer',
  template: `
    <footer class="footer">
      <div class="container">
        <div class="footer-grid">
          <div class="footer-brand">
            <div class="brand-name">beautystore</div>
            <p>India's premium beauty destination. Discover, explore, and shop from the world's best brands.</p>
            <div class="social-links">
              <a href="#" aria-label="Instagram">📸</a>
              <a href="#" aria-label="Twitter">🐦</a>
              <a href="#" aria-label="YouTube">▶️</a>
            </div>
          </div>
          <div class="footer-links">
            <h4>Shop</h4>
            <a href="#">Skincare</a>
            <a href="#">Makeup</a>
            <a href="#">Fragrance</a>
            <a href="#">Wellness</a>
          </div>
          <div class="footer-links">
            <h4>Help</h4>
            <a href="#">FAQs</a>
            <a href="#">Shipping Policy</a>
            <a href="#">Returns</a>
            <a href="#">Track Order</a>
          </div>
          <div class="footer-links">
            <h4>Company</h4>
            <a href="#">About Us</a>
            <a href="#">Careers</a>
            <a href="#">Press</a>
            <a href="#">Contact</a>
          </div>
        </div>
        <div class="footer-bottom">
          <p>© 2026 BeautyStore. All rights reserved.</p>
          <p class="built-with">Built with ❤️ using Angular + Azure</p>
        </div>
      </div>
    </footer>
  `,
  styles: [`
    .footer {
      background: #1a1a2e;
      color: rgba(255,255,255,0.8);
      padding: 72px 0 32px;
    }
    .container {
      max-width: 1280px;
      margin: 0 auto;
      padding: 0 24px;
    }
    .footer-grid {
      display: grid;
      grid-template-columns: 2fr 1fr 1fr 1fr;
      gap: 48px;
      margin-bottom: 56px;
    }
    @media (max-width: 900px) {
      .footer-grid { grid-template-columns: 1fr 1fr; }
    }
    @media (max-width: 560px) {
      .footer-grid { grid-template-columns: 1fr; }
    }
    .brand-name {
      font-family: 'Playfair Display', serif;
      font-size: 1.5rem;
      font-weight: 700;
      color: #fff;
      margin-bottom: 16px;
    }
    .footer-brand p {
      font-size: 0.875rem;
      line-height: 1.7;
      opacity: 0.7;
      margin-bottom: 24px;
    }
    .social-links {
      display: flex;
      gap: 16px;
    }
    .social-links a {
      font-size: 1.2rem;
      opacity: 0.7;
      transition: opacity 0.2s;
    }
    .social-links a:hover { opacity: 1; }
    .footer-links {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }
    .footer-links h4 {
      color: #fff;
      font-size: 0.875rem;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 1px;
      margin-bottom: 8px;
    }
    .footer-links a {
      font-size: 0.875rem;
      opacity: 0.65;
      transition: opacity 0.2s;
    }
    .footer-links a:hover { opacity: 1; }
    .footer-bottom {
      border-top: 1px solid rgba(255,255,255,0.1);
      padding-top: 32px;
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 0.8rem;
      opacity: 0.6;
    }
    @media (max-width: 560px) {
      .footer-bottom { flex-direction: column; gap: 8px; text-align: center; }
    }
  `]
})
export class FooterComponent {}
