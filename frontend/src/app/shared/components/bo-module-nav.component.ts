import { CommonModule } from '@angular/common';
import { Component, ElementRef, HostListener } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthJwtService } from '../../core/services/auth-jwt.service';
import { BoAuthService } from '../../core/services/bo-auth.service';
import { OperatingModeService } from '../../core/services/operating-mode.service';
import { OperatorSessionService } from '../../core/services/operator-session.service';

type GroupKey = 'operacion' | 'stock' | 'administracion' | 'mas';

type NavItem = {
  label: string;
  link: string;
  show: boolean;
  exact?: boolean;
};

@Component({
  standalone: true,
  selector: 'app-bo-module-nav',
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <nav class="bo-nav">
      <div class="toolbar">
        <div class="left-zone">
          <div class="primary-toolbar">
            <a
              *ngFor="let item of primaryItems"
              [routerLink]="item.link"
              class="tool-link"
              routerLinkActive="active-tool-link"
              [routerLinkActiveOptions]="{ exact: item.exact ?? false }"
              (click)="closeGroups()"
              >{{ item.label }}</a
            >
          </div>

          <span class="divider" aria-hidden="true"></span>

          <div class="group-toolbar">
            <div class="menu" *ngIf="operacionItems.length">
              <button
                type="button"
                class="group-trigger"
                (click)="toggleGroup('operacion')"
                [class.open]="isOpen('operacion')"
                [attr.aria-expanded]="isOpen('operacion')"
                aria-haspopup="menu"
              >
                Operación <span class="chevron">v</span>
              </button>
              <div class="menu-panel" *ngIf="isOpen('operacion')" role="menu">
                <a
                  *ngFor="let item of operacionItems"
                  [routerLink]="item.link"
                  routerLinkActive="active-link"
                  [routerLinkActiveOptions]="{ exact: item.exact ?? false }"
                  (click)="closeGroups()"
                  >{{ item.label }}</a
                >
              </div>
            </div>

            <div class="menu" *ngIf="stockItems.length">
              <button
                type="button"
                class="group-trigger"
                (click)="toggleGroup('stock')"
                [class.open]="isOpen('stock')"
                [attr.aria-expanded]="isOpen('stock')"
                aria-haspopup="menu"
              >
                Stock <span class="chevron">v</span>
              </button>
              <div class="menu-panel" *ngIf="isOpen('stock')" role="menu">
                <a
                  *ngFor="let item of stockItems"
                  [routerLink]="item.link"
                  routerLinkActive="active-link"
                  [routerLinkActiveOptions]="{ exact: item.exact ?? false }"
                  (click)="closeGroups()"
                  >{{ item.label }}</a
                >
              </div>
            </div>

            <div class="menu" *ngIf="administracionItems.length">
              <button
                type="button"
                class="group-trigger"
                (click)="toggleGroup('administracion')"
                [class.open]="isOpen('administracion')"
                [attr.aria-expanded]="isOpen('administracion')"
                aria-haspopup="menu"
              >
                Administración <span class="chevron">v</span>
              </button>
              <div class="menu-panel" *ngIf="isOpen('administracion')" role="menu">
                <a
                  *ngFor="let item of administracionItems"
                  [routerLink]="item.link"
                  routerLinkActive="active-link"
                  [routerLinkActiveOptions]="{ exact: item.exact ?? false }"
                  (click)="closeGroups()"
                  >{{ item.label }}</a
                >
              </div>
            </div>

            <div class="menu" *ngIf="masItems.length">
              <button
                type="button"
                class="group-trigger"
                (click)="toggleGroup('mas')"
                [class.open]="isOpen('mas')"
                [attr.aria-expanded]="isOpen('mas')"
                aria-haspopup="menu"
              >
                Más <span class="chevron">v</span>
              </button>
              <div class="menu-panel" *ngIf="isOpen('mas')" role="menu">
                <a
                  *ngFor="let item of masItems"
                  [routerLink]="item.link"
                  routerLinkActive="active-link"
                  [routerLinkActiveOptions]="{ exact: item.exact ?? false }"
                  (click)="closeGroups()"
                  >{{ item.label }}</a
                >
              </div>
            </div>
          </div>
        </div>

        <button type="button" class="logout" (click)="logout()">Cerrar sesión</button>
      </div>
    </nav>
  `,
  styles: [
    `.bo-nav{position:relative;z-index:4}`,
    `.toolbar{display:flex;justify-content:space-between;align-items:center;gap:12px;padding:10px 12px;border:1px solid #cfe3d8;border-radius:14px;background:rgba(248,253,250,.92);box-shadow:0 8px 18px rgba(31,79,58,.08)}`,
    `.left-zone{display:flex;gap:10px;align-items:center;flex:1;min-width:0}`,
    `.primary-toolbar{display:flex;gap:8px;flex-wrap:wrap}`,
    `.tool-link{text-decoration:none;color:#1c5d45;background:#e8f5ef;border:1px solid #c3dfd0;border-radius:999px;padding:7px 11px;font-size:13px;font-weight:700;transition:all .18s ease}`,
    `.tool-link:hover{background:#dff2e9}`,
    `.active-tool-link{background:#cfeadb;border-color:#8ac2a8;color:#144d37}`,
    `.divider{width:1px;align-self:stretch;background:#d3e6db}`,
    `.group-toolbar{display:flex;gap:8px;flex-wrap:wrap;align-items:center}`,
    `.menu{position:relative}`,
    `.group-trigger{cursor:pointer;color:#225f48;background:#f0f7f3;border:1px solid #cfe3d8;border-radius:999px;padding:7px 11px;font-size:13px;font-weight:700;font-family:inherit;min-height:34px;transition:all .18s ease;display:inline-flex;gap:6px;align-items:center}`,
    `.group-trigger:hover{background:#e2f2ea}`,
    `.group-trigger.open{background:#dff2e9;border-color:#8ac2a8;color:#144d37}`,
    `.group-trigger:focus-visible{outline:none;box-shadow:0 0 0 3px rgba(47,142,103,.2)}`,
    `.chevron{font-size:11px;line-height:1}`,
    `.menu-panel{position:absolute;top:calc(100% + 6px);left:0;min-width:220px;display:flex;flex-direction:column;gap:2px;padding:6px;background:#fff;border:1px solid #d8e8df;border-radius:12px;box-shadow:0 12px 24px rgba(20,62,46,.16)}`,
    `.menu-panel a{text-decoration:none;color:#1e5d45;padding:8px 10px;border-radius:8px;font-size:13px;font-weight:600}`,
    `.menu-panel a:hover{background:#eff8f3}`,
    `.menu-panel a.active-link{background:#dff2e9;color:#16543d}`,
    `.logout{cursor:pointer;color:#8f1d22;background:#fdf2f2;border:1px solid #efc7c9;border-radius:999px;padding:7px 11px;font-size:13px;font-family:inherit;font-weight:700;min-height:34px;white-space:nowrap}`,
    `.logout:hover{background:#fbdedf}`,
    `@media (max-width: 1200px){.left-zone{overflow-x:auto;padding-bottom:2px}.left-zone::-webkit-scrollbar{height:6px}.left-zone::-webkit-scrollbar-thumb{background:#cfe3d8;border-radius:999px}}`,
    `@media (max-width: 900px){.toolbar{align-items:flex-start;flex-direction:column}.left-zone{width:100%;overflow-x:auto}.divider{display:none}.menu-panel{position:fixed;left:16px;right:16px;top:88px;min-width:auto}.logout{align-self:flex-end}}`
  ]
})
export class BoModuleNavComponent {
  modules = {
    tablet: true,
    envases: true,
    cuentaCorriente: true,
    comprasSugeridas: true,
    reportes: true
  };
  canSeeProducts = false;
  canSeeDataFlows = false;
  canSeeOpsFlows = false;
  canSeeAdminFlows = false;
  openGroup: GroupKey | null = null;

  primaryItems: NavItem[] = [];
  operacionItems: NavItem[] = [];
  stockItems: NavItem[] = [];
  administracionItems: NavItem[] = [];
  masItems: NavItem[] = [];

  private buildNavigation(): void {
    this.primaryItems = this.filterVisible([
      { label: 'Lo diario', link: '/bo/dashboard-gerencial', show: true, exact: true },
      { label: 'Reportes', link: '/bo/exportaciones', show: this.modules.reportes },
      { label: 'Importaciones', link: '/bo/importaciones/unificada', show: this.canSeeDataFlows },
      { label: 'Checklist', link: '/bo/operacion/checklist', show: this.canSeeOpsFlows }
    ]);

    this.operacionItems = this.filterVisible([
      { label: 'Checklist', link: '/bo/operacion/checklist', show: this.canSeeOpsFlows },
      { label: 'Puesta en marcha', link: '/bo/operacion/puesta-en-marcha', show: this.canSeeOpsFlows },
      { label: 'Descargas', link: '/bo/operacion/descargas', show: this.canSeeOpsFlows },
      { label: 'Onboarding', link: '/bo/onboarding', show: this.canSeeOpsFlows }
    ]);

    this.stockItems = this.filterVisible([
      { label: 'Panel de stock', link: '/bo/stock', show: true },
      { label: 'Importacion unificada', link: '/bo/importaciones/unificada', show: this.canSeeDataFlows },
      { label: 'Ajuste masivo stock', link: '/bo/importaciones/ajuste-masivo-stock', show: this.canSeeDataFlows },
      { label: 'Importaciones', link: '/bo/importaciones', show: this.canSeeDataFlows },
      { label: 'Stock inicial', link: '/bo/importaciones/stock-inicial', show: this.canSeeDataFlows },
      { label: 'Compras manuales', link: '/bo/compras/manuales', show: this.modules.comprasSugeridas },
      { label: 'Compras sugeridas', link: '/bo/compras/sugeridas', show: this.modules.comprasSugeridas },
      { label: 'Productos', link: '/bo/productos', show: this.canSeeProducts },
      { label: 'Totem transición', link: '/bo/totem/transiciones', show: this.canSeeProducts }
    ]);

    this.administracionItems = this.filterVisible([
      { label: 'Empresa', link: '/bo/admin/empresa', show: this.canSeeAdminFlows },
      { label: 'Locales', link: '/bo/admin/locales', show: this.canSeeAdminFlows },
      { label: 'Branding', link: '/bo/admin/branding', show: this.canSeeAdminFlows }
    ]);

    this.masItems = this.filterVisible([
      { label: 'Mapa módulos', link: '/bo/modulos', show: true },
      { label: 'Capacitación', link: '/bo/capacitacion', show: true },
      { label: 'Demo', link: '/bo/admin/demo', show: true }
    ]);
  }

  private filterVisible(items: NavItem[]): NavItem[] {
    return items.filter(item => item.show);
  }

  toggleGroup(group: GroupKey): void {
    this.openGroup = this.openGroup === group ? null : group;
  }

  isOpen(group: GroupKey): boolean {
    return this.openGroup === group;
  }

  closeGroups(): void {
    this.openGroup = null;
  }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    this.closeGroups();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    if (!this.elementRef.nativeElement.contains(event.target as Node)) {
      this.closeGroups();
    }
  }

  logout(): void {
    this.boAuth.logout();
    this.operatorSession.clearSession();
    localStorage.removeItem('bo_active_store_id');
    localStorage.removeItem('bo_active_tenant_id');
    void this.router.navigateByUrl('/inicio/login');
  }

  constructor(
    private readonly operatingMode: OperatingModeService,
    private readonly boAuth: BoAuthService,
    private readonly operatorSession: OperatorSessionService,
    private readonly authJwt: AuthJwtService,
    private readonly router: Router,
    private readonly elementRef: ElementRef<HTMLElement>
  ) {
    this.modules = this.operatingMode.getConfig().modules;
    this.canSeeProducts = this.authJwt.hasAnyRole(['Admin', 'Supervisor']);
    this.canSeeDataFlows = this.authJwt.hasAnyRole(['Admin', 'Supervisor', 'Manager']);
    this.canSeeOpsFlows = this.authJwt.hasAnyRole(['Admin', 'Supervisor', 'Manager']);
    this.canSeeAdminFlows = this.authJwt.hasAnyRole(['Admin', 'Supervisor']);
    this.buildNavigation();
  }
}
