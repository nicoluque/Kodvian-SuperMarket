import { AsyncPipe, NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthJwtService } from '../../core/services/auth-jwt.service';
import { BrandingService } from '../../core/services/branding.service';
import { HealthService } from '../../core/services/health.service';
import { OperatingModeService } from '../../core/services/operating-mode.service';
import { OperatorSessionService } from '../../core/services/operator-session.service';

@Component({
  standalone: true,
  selector: 'app-global-status-bar',
  imports: [NgIf, AsyncPipe],
  template: `
    <div class="status">
      <img *ngIf="logoUrl" [src]="logoUrl" alt="logo" class="logo" />
      <strong>{{ brandName }}</strong>
      <span class="pill" [class.offline]="!(health.isOnline$ | async)">{{ (health.isOnline$ | async) ? 'En línea' : 'Sin conexión' }}</span>
      <span>tenant: {{ tenantLabel }}</span>
      <span>sucursal: {{ storeLabel }}</span>
      <span *ngIf="boUser">usuario: {{ boUser }}</span>
      <span *ngIf="boRole">rol: {{ boRole }}</span>
      <span *ngIf="operator">operadora: {{ operator }}</span>
      <span>modo: {{ operatingModeLabel }}</span>
    </div>
  `,
  styles: [
    `.status{position:sticky;top:0;z-index:60;display:flex;gap:10px;align-items:center;padding:6px 12px;background:#e4f1ed;border-bottom:1px solid #b6d8cc;font-family:Arial,sans-serif;font-size:12px;color:#0f3a40}`,
    `.pill{padding:2px 8px;border-radius:999px;background:#d8efe7;color:#0c8f6f;font-weight:700}`,
    `.pill.offline{background:#ffe5cf;color:#9e4a18}`,
    `.logo{height:18px;width:18px;object-fit:contain;border-radius:3px}`
  ]
})
export class GlobalStatusBarComponent {
  brandName = 'Kodvian';
  logoUrl = '';
  tenantLabel = '-';
  storeLabel = '-';
  boUser = '';
  boRole = '';
  operator = '';
  operatingModeLabel = 'MiniMarket Full';
  isAuthLogin = false;

  constructor(
    public readonly health: HealthService,
    private readonly authJwt: AuthJwtService,
    private readonly branding: BrandingService,
    private readonly operatingMode: OperatingModeService,
    private readonly operatorSession: OperatorSessionService,
    router: Router
  ) {
    this.refresh();
    router.events.subscribe(() => this.refresh());
  }

  private refresh(): void {
    const path = typeof window !== 'undefined' ? window.location.pathname : '';
    this.isAuthLogin = path.startsWith('/inicio/login') || path.startsWith('/bo/login');
    this.brandName = this.branding.current?.displayName || 'Kodvian';
    this.logoUrl = this.branding.current?.logoUrl || '';
    this.storeLabel = localStorage.getItem('bo_active_store_id') ?? '-';
    this.tenantLabel = localStorage.getItem('bo_active_tenant_id') ?? '-';
    this.boUser = this.isAuthLogin ? '' : (this.authJwt.getUsername() ?? '');
    this.boRole = this.isAuthLogin ? '' : (this.authJwt.getRole() ?? '');
    this.operator = this.isAuthLogin ? '' : (this.operatorSession.getSessionToken() ? this.operatorSession.getOperatorName() : '');
    this.operatingModeLabel = this.operatingMode.getModeLabel(this.operatingMode.getConfig().mode);
  }
}
