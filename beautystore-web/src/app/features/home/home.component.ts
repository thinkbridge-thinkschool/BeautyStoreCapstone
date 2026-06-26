import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { rxResource } from '@angular/core/rxjs-interop';
import { Product } from '../../core/models/product.model';
import { CatalogService } from '../../core/services/catalog.service';
import { ProductCardComponent } from './components/product-card/product-card.component';
import { HeroComponent } from './components/hero/hero.component';
import { BenefitsComponent } from './components/benefits/benefits.component';
import { FooterComponent } from './components/footer/footer.component';

@Component({
  selector: 'app-home',
  imports: [RouterLink, ProductCardComponent, HeroComponent, BenefitsComponent, FooterComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent {
  #catalog = inject(CatalogService);

  productsResource = rxResource<Product[], undefined>({
    stream: () => this.#catalog.getProducts()
  });

  skeletons = Array.from({ length: 6 });
}
