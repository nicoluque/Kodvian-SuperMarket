import { AsyncPipe, NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HealthService } from '../../core/services/health.service';

@Component({
  standalone: true,
  selector: 'app-pos-caja-online-banner',
  imports: [NgIf, AsyncPipe, RouterLink],
  template: `
    <div class="banner" [class.offline]="!(health.isOnline$ | async)">
      <strong>{{ (health.isOnline$ | async) ? 'Online' : 'Offline' }}</strong>
      <span *ngIf="!(health.isOnline$ | async)">Caja en modo cash-only. Sync pendiente.</span>
      <a routerLink="/pos/caja/offline-queue">Offline queue</a>
    </div>
  `,
  styles: [
    `.banner{position:sticky;top:0;z-index:50;display:flex;gap:12px;align-items:center;padding:8px 12px;background:#e8f5e9;border-bottom:1px solid #c8e6c9;font-family:Arial,sans-serif}`,
    `.banner.offline{background:#fff3cd;border-bottom-color:#f5c06f}`,
    `a{margin-left:auto}`
  ]
})
export class PosCajaOnlineBannerComponent {
  constructor(public readonly health: HealthService) {}
}
