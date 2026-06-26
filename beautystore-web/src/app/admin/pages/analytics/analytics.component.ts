import { Component, inject, computed } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { CurrencyPipe, DecimalPipe, DatePipe } from '@angular/common';
import { AdminService } from '../../services/admin.service';

@Component({
  selector: 'app-admin-analytics',
  imports: [CurrencyPipe, DecimalPipe, DatePipe],
  templateUrl: './analytics.component.html',
  styleUrl:    './analytics.component.scss',
})
export class AnalyticsComponent {
  #admin = inject(AdminService);

  analyticsResource = rxResource({ stream: () => this.#admin.getAnalytics() });
  data = computed(() => this.analyticsResource.value());

  // ── SVG chart constants ───────────────────────────────────────────────────
  private readonly W  = 600;
  private readonly H  = 140;
  private readonly PX = 8;
  private readonly PY = 10;

  trendPoints = computed(() => {
    const trend = this.data()?.revenueTrend ?? [];
    if (trend.length === 0) return [];
    const maxRev = Math.max(...trend.map(p => p.revenue), 1);
    const iW = this.W - this.PX * 2;
    const iH = this.H - this.PY * 2;
    return trend.map((p, i) => ({
      x: this.PX + (trend.length === 1 ? iW / 2 : (i / (trend.length - 1)) * iW),
      y: this.PY + iH - (p.revenue / maxRev) * iH,
      date:     p.date,
      revenue:  p.revenue,
      orders:   p.orderCount,
    }));
  });

  polyline = computed(() =>
    this.trendPoints().map(p => `${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ')
  );

  fillArea = computed(() => {
    const pts = this.trendPoints();
    if (pts.length === 0) return '';
    const bottom = (this.H - this.PY).toFixed(1);
    return [
      ...pts.map(p => `${p.x.toFixed(1)},${p.y.toFixed(1)}`),
      `${pts.at(-1)!.x.toFixed(1)},${bottom}`,
      `${pts[0].x.toFixed(1)},${bottom}`,
    ].join(' ');
  });

  firstDate = computed(() => this.trendPoints().at(0)?.date ?? '');
  lastDate  = computed(() => this.trendPoints().at(-1)?.date ?? '');
}
