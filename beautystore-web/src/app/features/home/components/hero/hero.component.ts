import { Component } from '@angular/core';

@Component({
  selector: 'app-hero',
  template: `
    <section class="hero">
      <nav class="nav">
        <div class="nav-brand">beautystore</div>
        <div class="nav-links">
          <a href="#">Shop</a>
          <a href="#">About</a>
          <button class="btn-signin">Sign In</button>
        </div>
      </nav>
      <div class="hero-content">
        <div class="badge">New Arrivals</div>
        <h1>Your Beauty,<br>Elevated.</h1>
        <p>Discover premium skincare, makeup, and wellness products from 200+ global brands — delivered to your door.</p>
        <div class="hero-cta">
          <button class="btn-primary">Shop Now</button>
          <button class="btn-secondary">Explore Brands</button>
        </div>
        <div class="hero-stats">
          <div class="stat">
            <span class="stat-number">50K+</span>
            <span class="stat-label">Happy Customers</span>
          </div>
          <div class="stat">
            <span class="stat-number">200+</span>
            <span class="stat-label">Premium Brands</span>
          </div>
          <div class="stat">
            <span class="stat-number">100%</span>
            <span class="stat-label">Authentic</span>
          </div>
        </div>
      </div>
      <div class="hero-decoration">
        <div class="circle circle-1"></div>
        <div class="circle circle-2"></div>
        <div class="circle circle-3"></div>
      </div>
    </section>
  `,
  styles: [`
    .hero {
      background: linear-gradient(135deg, #ff4fa3 0%, #e0007a 55%, #c0006a 100%);
      color: white;
      min-height: 100vh;
      position: relative;
      overflow: hidden;
      display: flex;
      flex-direction: column;
    }
    .nav {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 24px 48px;
      position: relative;
      z-index: 10;
    }
    .nav-brand {
      font-family: 'Playfair Display', serif;
      font-size: 1.75rem;
      font-weight: 700;
      letter-spacing: -0.5px;
    }
    .nav-links {
      display: flex;
      align-items: center;
      gap: 32px;
    }
    .nav-links a {
      font-size: 0.95rem;
      font-weight: 500;
      opacity: 0.9;
      transition: opacity 0.2s;
    }
    .nav-links a:hover { opacity: 1; }
    .btn-signin {
      background: rgba(255,255,255,0.15);
      color: white;
      padding: 10px 24px;
      border-radius: 100px;
      font-size: 0.9rem;
      font-weight: 600;
      border: 1px solid rgba(255,255,255,0.3);
      backdrop-filter: blur(10px);
      transition: background 0.2s;
      cursor: pointer;
    }
    .btn-signin:hover { background: rgba(255,255,255,0.25); }
    .hero-content {
      flex: 1;
      display: flex;
      flex-direction: column;
      justify-content: center;
      padding: 60px 48px;
      max-width: 640px;
      position: relative;
      z-index: 10;
    }
    .badge {
      display: inline-block;
      background: rgba(255,255,255,0.2);
      backdrop-filter: blur(10px);
      padding: 6px 16px;
      border-radius: 100px;
      font-size: 0.8rem;
      font-weight: 600;
      letter-spacing: 1px;
      text-transform: uppercase;
      margin-bottom: 24px;
      width: fit-content;
    }
    h1 {
      font-family: 'Playfair Display', serif;
      font-size: clamp(3rem, 6vw, 5rem);
      font-weight: 700;
      line-height: 1.1;
      margin-bottom: 24px;
    }
    p {
      font-size: 1.1rem;
      line-height: 1.7;
      opacity: 0.9;
      margin-bottom: 36px;
      max-width: 480px;
    }
    .hero-cta {
      display: flex;
      gap: 16px;
      margin-bottom: 56px;
      flex-wrap: wrap;
    }
    .btn-primary {
      background: white;
      color: #e0007a;
      padding: 16px 36px;
      border-radius: 100px;
      font-size: 1rem;
      font-weight: 700;
      border: none;
      cursor: pointer;
      transition: transform 0.2s, box-shadow 0.2s;
    }
    .btn-primary:hover {
      transform: translateY(-2px);
      box-shadow: 0 8px 32px rgba(0,0,0,0.2);
    }
    .btn-secondary {
      background: transparent;
      color: white;
      padding: 16px 36px;
      border-radius: 100px;
      font-size: 1rem;
      font-weight: 600;
      border: 2px solid rgba(255,255,255,0.6);
      cursor: pointer;
      transition: border-color 0.2s, background 0.2s;
    }
    .btn-secondary:hover {
      border-color: white;
      background: rgba(255,255,255,0.1);
    }
    .hero-stats {
      display: flex;
      gap: 48px;
      flex-wrap: wrap;
    }
    .stat {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }
    .stat-number {
      font-family: 'Playfair Display', serif;
      font-size: 1.75rem;
      font-weight: 700;
    }
    .stat-label {
      font-size: 0.8rem;
      opacity: 0.75;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }
    .hero-decoration {
      position: absolute;
      inset: 0;
      pointer-events: none;
    }
    .circle {
      position: absolute;
      border-radius: 50%;
      background: rgba(255,255,255,0.06);
    }
    .circle-1 { width: 600px; height: 600px; right: -150px; top: -100px; }
    .circle-2 { width: 400px; height: 400px; right: 100px; bottom: -100px; background: rgba(255,255,255,0.04); }
    .circle-3 { width: 200px; height: 200px; right: 400px; top: 50%; background: rgba(255,255,255,0.05); }
  `]
})
export class HeroComponent {}
