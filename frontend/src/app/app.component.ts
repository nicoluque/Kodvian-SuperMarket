import { NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { AuthJwtService } from './core/services/auth-jwt.service';
import { BrandingService } from './core/services/branding.service';
import { HealthService } from './core/services/health.service';
import { SyncService } from './core/services/sync.service';
import { BoStoreSelectorComponent } from './shared/components/bo-store-selector.component';
import { GlobalLoadingComponent } from './shared/components/global-loading.component';
import { GlobalDialogComponent } from './shared/components/global-dialog.component';
import { GlobalStatusBarComponent } from './shared/components/global-status-bar.component';
import { GlobalToastComponent } from './shared/components/global-toast.component';
import { PosCajaOnlineBannerComponent } from './shared/components/pos-caja-online-banner.component';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    NgIf,
    PosCajaOnlineBannerComponent,
    BoStoreSelectorComponent,
    GlobalStatusBarComponent,
    GlobalDialogComponent,
    GlobalToastComponent,
    GlobalLoadingComponent
  ],
  template: `
    <app-global-loading />
    <app-global-status-bar />
    <app-pos-caja-online-banner *ngIf="showCajaBanner" />
    <app-bo-store-selector *ngIf="showBoSelector" />
    <router-outlet />
    <app-global-dialog />
    <app-global-toast />
  `
})
export class AppComponent {
  showCajaBanner = false;
  showBoSelector = false;

  constructor(router: Router, health: HealthService, sync: SyncService, branding: BrandingService, auth: AuthJwtService) {
    health.startPolling(4000);
    sync.start();
    void branding.load().catch(() => undefined);

    this.showCajaBanner = router.url.startsWith('/pos/caja');
    this.showBoSelector = isBackofficeWithSession(router.url, auth.isTokenValid());
    router.events.pipe(filter((e) => e instanceof NavigationEnd)).subscribe(() => {
      this.showCajaBanner = router.url.startsWith('/pos/caja');
      this.showBoSelector = isBackofficeWithSession(router.url, auth.isTokenValid());
    });
  }
}

function isBackofficeWithSession(url: string, hasValidSession: boolean): boolean {
  if (!url.startsWith('/bo/')) return false;
  if (url.startsWith('/bo/login')) return false;
  return hasValidSession;
}
