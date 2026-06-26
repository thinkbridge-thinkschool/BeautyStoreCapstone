import { Component, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { DecimalPipe, DatePipe, CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../services/admin.service';

@Component({
  selector: 'app-admin-dashboard',
  imports: [DecimalPipe, DatePipe, CurrencyPipe, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {
  #admin = inject(AdminService);

  dashboard = rxResource({ stream: () => this.#admin.getDashboard() });

  readonly statusClass: Partial<Record<string, string>> = {
    Created:   'badge-blue',
    Confirmed: 'badge-yellow',
    Shipped:   'badge-purple',
    Delivered: 'badge-green',
    Cancelled: 'badge-red',
  };
}
