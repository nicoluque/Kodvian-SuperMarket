import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { BoAuthService } from '../../core/services/bo-auth.service';
import { OperatingModeService } from '../../core/services/operating-mode.service';
import { OperatorSessionService } from '../../core/services/operator-session.service';

@Component({
  standalone: true,
  selector: 'app-pos-module-nav',
  imports: [CommonModule, RouterLink],
  template: `
    <nav class="pos-nav">
      <a routerLink="/inicio">Ir a inicio</a>
      <a [routerLink]="homeRoute">Inicio POS</a>
      <a *ngIf="canUseCajaFlow" routerLink="/pos/caja/inbox">Ventas</a>
      <a *ngIf="canUseCajaFlow" routerLink="/pos/caja/pendientes">Pendientes</a>
      <a *ngIf="canUseCajaFlow" routerLink="/pos/caja/movimientos">Movimientos</a>
      <a *ngIf="canUseCajaFlow" routerLink="/pos/caja/devoluciones">Devoluciones</a>
      <a *ngIf="canUseCajaFlow && modules.cuentaCorriente" routerLink="/pos/caja/pago-cuenta">Cuenta corriente</a>
      <a *ngIf="canUseCajaFlow && modules.envases" routerLink="/pos/caja/envases">Envases</a>
      <a *ngIf="canUseTabletFlow" routerLink="/pos/tablet/nueva">Totem</a>
      <button type="button" class="logout" (click)="logout()">Cerrar sesion</button>
    </nav>
  `,
  styles: [
    `:host{position:relative;z-index:3;display:block;max-width:1000px;margin:0 auto;padding:12px 16px 0}`,
    `.pos-nav{display:flex;gap:8px;flex-wrap:wrap}`,
    `.pos-nav a{text-decoration:none;color:#1f7f57;background:#e7f4ee;border:1px solid #c6e5d6;border-radius:999px;padding:6px 10px;font-size:13px}`,
    `.logout{margin-left:auto;border:1px solid #efc7c9;background:#fdf2f2;color:#8f1d22;border-radius:999px;padding:6px 10px;font-size:13px;font-weight:700;cursor:pointer;font-family:inherit}`,
    `.logout:hover{background:#fbdedf}`,
    `@media (max-width: 700px){.logout{margin-left:0;width:100%}}`
  ]
})
export class PosModuleNavComponent {
  homeRoute = '/pos/caja/apertura';
  deviceType = '';
  modules = {
    tablet: true,
    envases: true,
    cuentaCorriente: true,
    comprasSugeridas: true,
    reportes: true
  };

  constructor(
    private readonly operatingMode: OperatingModeService,
    private readonly boAuth: BoAuthService,
    private readonly operatorSession: OperatorSessionService,
    private readonly router: Router
  ) {
    this.modules = this.operatingMode.getConfig().modules;
    this.deviceType = this.operatingMode.getDeviceType();
    this.homeRoute = this.operatingMode.getPreferredPosRoute();
  }

  get canUseCajaFlow(): boolean {
    return this.deviceType === 'CashRegister';
  }

  get canUseTabletFlow(): boolean {
    return this.deviceType === 'Tablet' && this.modules.tablet;
  }

  logout(): void {
    this.operatorSession.clearSession();
    this.boAuth.logout();
    localStorage.removeItem('bo_active_store_id');
    localStorage.removeItem('bo_active_tenant_id');
    void this.router.navigateByUrl('/inicio/login');
  }
}
