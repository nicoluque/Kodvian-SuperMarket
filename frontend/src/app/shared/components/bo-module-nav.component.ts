import { CommonModule } from '@angular/common';
import { Component, HostListener, OnDestroy } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { AuthJwtService } from '../../core/services/auth-jwt.service';
import { BoAuthService } from '../../core/services/bo-auth.service';
import { OperatingModeService } from '../../core/services/operating-mode.service';
import { OperatorSessionService } from '../../core/services/operator-session.service';

type GroupKey = 'inicio' | 'comercial' | 'inventario' | 'operacion' | 'administracion' | 'analitica';

type NavItem = {
  label: string;
  link: string;
  fragment?: string;
  show: boolean;
  icon: string;
  exact?: boolean;
};

type NavGroup = {
  key: GroupKey;
  label: string;
  icon: string;
  show: boolean;
  items: NavItem[];
};

@Component({
  standalone: true,
  selector: 'app-bo-module-nav',
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <button
      type="button"
      class="mobile-toggle"
      (click)="toggleSidebar()"
      aria-label="Abrir menú"
      title="Abrir menú"
    >☰</button>

    <button type="button" class="sidebar-backdrop" *ngIf="mobileOpen" (click)="closeMobileMenu()" aria-label="Cerrar menú"></button>

    <aside class="bo-sidebar" [class.collapsed]="sidebarCollapsed" [class.mobile-open]="mobileOpen">
      <header class="sidebar-head">
        <strong class="brand" *ngIf="!sidebarCollapsed">Administración</strong>
        <span class="brand" *ngIf="sidebarCollapsed">BO</span>
        <button
          type="button"
          class="collapse-toggle"
          (click)="toggleSidebar()"
          [attr.aria-label]="sidebarCollapsed ? 'Expandir menú' : 'Ocultar menú'"
          [title]="sidebarCollapsed ? 'Expandir menú' : 'Ocultar menú'"
        >
          {{ sidebarCollapsed ? '▸' : '◂' }}
        </button>
      </header>

      <nav class="sidebar-menu" aria-label="Navegación administración">
        <section class="menu-group" *ngFor="let group of navGroups" [class.hidden]="!group.show">
          <button type="button" class="group-trigger" (click)="toggleGroup(group.key)" [class.open]="isOpen(group.key)">
            <span class="group-label">
              <span class="icon">{{ group.icon }}</span>
              <span class="text" *ngIf="!sidebarCollapsed">{{ group.label }}</span>
            </span>
            <span class="chevron" *ngIf="!sidebarCollapsed">{{ isOpen(group.key) ? '▾' : '▸' }}</span>
          </button>

          <div class="group-items" *ngIf="isOpen(group.key) && !sidebarCollapsed">
            <a
              *ngFor="let item of group.items"
              [routerLink]="item.link"
              [fragment]="item.fragment"
              class="item-link"
              routerLinkActive="active-link"
              [routerLinkActiveOptions]="{ exact: item.exact ?? false }"
              (click)="onNavigate()"
            >
              <span class="icon">{{ item.icon }}</span>
              <span>{{ item.label }}</span>
            </a>
          </div>
        </section>
      </nav>

      <footer class="sidebar-foot">
        <button type="button" class="logout" (click)="logout()">
          <span class="icon">↪</span>
          <span *ngIf="!sidebarCollapsed">Cerrar sesión</span>
        </button>
      </footer>
    </aside>
  `,
  styles: [
    `:host{display:block}`,
    `.mobile-toggle{display:none}`,
    `.sidebar-backdrop{display:none}`,
    `.bo-sidebar{position:fixed;top:var(--bo-top-offset,31px);left:0;bottom:0;width:252px;z-index:43;background:linear-gradient(180deg,var(--bo-sidebar-bg-alt,#01262a) 0%,var(--bo-sidebar-bg,#001c24) 100%);border-right:1px solid var(--bo-sidebar-border,rgba(99,175,152,.32));display:flex;flex-direction:column;transition:width .2s ease,transform .2s ease}`,
    `.bo-sidebar.collapsed{width:76px}`,
    `.sidebar-head{padding:10px 10px;border-bottom:1px solid var(--bo-sidebar-border,rgba(99,175,152,.32));color:var(--bo-sidebar-text,#f2fffb);letter-spacing:.02em;display:flex;align-items:center;justify-content:space-between;min-height:52px}`,
    `.brand{font-weight:700}`,
    `.collapse-toggle{width:30px;height:30px;border-radius:8px;border:1px solid var(--bo-sidebar-border,rgba(99,175,152,.32));background:var(--bo-sidebar-surface,#05343a);color:var(--bo-sidebar-text,#f2fffb);cursor:pointer}`,
    `.collapse-toggle:hover{background:var(--bo-hero-start,#0e4a44)}`,
    `.sidebar-menu{padding:10px 8px;display:flex;flex-direction:column;gap:8px;overflow:auto}`,
    `.menu-group.hidden{display:none}`,
    `.group-trigger{width:100%;border:1px solid transparent;background:transparent;color:var(--bo-sidebar-text-muted,#d5efe6);font-family:inherit;border-radius:10px;cursor:pointer;padding:9px 10px;display:flex;align-items:center;justify-content:space-between}`,
    `.group-trigger:hover{background:var(--bo-sidebar-hover,rgba(32,150,118,.26))}`,
    `.group-trigger.open{background:rgba(15,164,127,.24);border-color:var(--bo-sidebar-border,rgba(99,175,152,.32))}`,
    `.group-label{display:flex;align-items:center;gap:10px;font-weight:700}`,
    `.icon{display:inline-flex;min-width:18px;justify-content:center}`,
    `.group-items{display:flex;flex-direction:column;gap:4px;padding:4px 0 2px 8px}`,
    `.item-link{text-decoration:none;color:var(--bo-sidebar-text,#e6f5f0);padding:8px 10px;border-radius:8px;display:flex;gap:8px;align-items:center;font-size:13px}`,
    `.item-link:hover{background:var(--bo-sidebar-hover,rgba(32,150,118,.26))}`,
    `.item-link.active-link{background:var(--bo-sidebar-accent,#0fa47f);color:#fff}`,
    `.sidebar-foot{margin-top:auto;padding:10px 8px;border-top:1px solid var(--bo-sidebar-border,rgba(99,175,152,.32))}`,
    `.logout{width:100%;border:1px solid var(--bo-sidebar-border,rgba(99,175,152,.32));background:var(--bo-sidebar-surface,#052f34);color:var(--bo-sidebar-text,#f1faf8);border-radius:10px;min-height:38px;cursor:pointer;display:flex;align-items:center;justify-content:center;gap:8px}`,
    `.logout:hover{background:var(--bo-hero-end,#0c433f)}`,
    `@media (max-width: 1024px){.mobile-toggle{display:block;position:fixed;left:12px;top:calc(var(--bo-top-offset,31px) + 8px);z-index:44;width:34px;height:34px;border-radius:8px;border:1px solid var(--bo-sidebar-border,rgba(99,175,152,.32));background:var(--bo-sidebar-surface,#0b4a3d);color:#fff;cursor:pointer}.bo-sidebar{transform:translateX(-100%);width:252px}.bo-sidebar.mobile-open{transform:translateX(0)}.bo-sidebar.collapsed{width:252px}.bo-sidebar .collapse-toggle{display:none}.sidebar-backdrop{display:block;position:fixed;inset:0;background:rgba(6,20,24,.45);z-index:42}}`
  ]
})
export class BoModuleNavComponent implements OnDestroy {
  private readonly sidebarStorageKey = 'bo_sidebar_collapsed';
  private routeSub: Subscription | null = null;
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
  openGroup: GroupKey | null = 'inicio';
  navGroups: NavGroup[] = [];
  sidebarCollapsed = false;
  mobileOpen = false;

  private buildGroup(key: GroupKey, label: string, icon: string, items: NavItem[]): NavGroup {
    const visibleItems = items.filter(item => item.show);
    return { key, label, icon, show: visibleItems.length > 0, items: visibleItems };
  }

  private buildNavigation(): void {
    this.navGroups = [
      this.buildGroup('inicio', 'Inicio', '🏠', [
        { label: 'Panel gerencial', link: '/bo/dashboard-gerencial', icon: '📊', show: true, exact: true }
      ]),
      this.buildGroup('comercial', 'Comercial', '💼', [
        { label: 'Clientes', link: '/bo/clientes', icon: '👥', show: true },
        { label: 'Proveedores', link: '/bo/proveedores', icon: '🏢', show: true },
        { label: 'Pendientes transferencia', link: '/bo/pendientes-transferencia', icon: '🔁', show: true }
      ]),
      this.buildGroup('inventario', 'Inventario', '📦', [
        { label: 'Panel de stock', link: '/bo/stock', icon: '📦', show: true },
        { label: 'Productos', link: '/bo/productos', icon: '🏷️', show: this.canSeeProducts },
        { label: 'Importación unificada', link: '/bo/importaciones/unificada', icon: '📥', show: this.canSeeDataFlows },
        { label: 'Ajuste masivo', link: '/bo/importaciones/ajuste-masivo-stock', icon: '🧮', show: this.canSeeDataFlows },
        { label: 'Stock inicial', link: '/bo/importaciones/stock-inicial', icon: '🧱', show: this.canSeeDataFlows },
        { label: 'Compras manuales', link: '/bo/compras/manuales', icon: '🛒', show: this.modules.comprasSugeridas },
        { label: 'Compras sugeridas', link: '/bo/compras/sugeridas', icon: '💡', show: this.modules.comprasSugeridas }
      ]),
      this.buildGroup('operacion', 'Operación', '🧭', [
        { label: 'Checklist', link: '/bo/operacion/checklist', icon: '✅', show: this.canSeeOpsFlows },
        { label: 'Puesta en marcha', link: '/bo/operacion/puesta-en-marcha', icon: '🚀', show: this.canSeeOpsFlows },
        { label: 'Descargas', link: '/bo/operacion/descargas', icon: '📁', show: this.canSeeOpsFlows },
        { label: 'Onboarding', link: '/bo/onboarding', icon: '🧩', show: this.canSeeOpsFlows },
        { label: 'Kanban', link: '/bo/kanban', icon: '🗂️', show: true },
        { label: 'RRHH', link: '/bo/rrhh', icon: '🧑‍💼', show: true }
      ]),
      this.buildGroup('administracion', 'Administración', '⚙️', [
        { label: 'Empresa', link: '/bo/admin/empresa', icon: '🏛️', show: this.canSeeAdminFlows },
        { label: 'Locales', link: '/bo/admin/locales', icon: '🏬', show: this.canSeeAdminFlows },
        { label: 'Branding', link: '/bo/admin/branding', icon: '🎨', show: this.canSeeAdminFlows },
        { label: 'Demo', link: '/bo/admin/demo', icon: '🧪', show: this.canSeeAdminFlows }
      ]),
      this.buildGroup('analitica', 'Analítica', '📈', [
        { label: 'Exportaciones', link: '/bo/exportaciones', icon: '📤', show: this.modules.reportes },
        { label: 'Mapa módulos', link: '/bo/modulos', icon: '🗺️', show: true },
        { label: 'Capacitación', link: '/bo/capacitacion', icon: '🎓', show: true }
      ])
    ];
  }

  private groupForUrl(url: string): GroupKey {
    const path = `${url ?? ''}`.toLowerCase();
    if (path.includes('/bo/clientes') || path.includes('/bo/proveedores') || path.includes('/bo/pendientes-transferencia')) return 'comercial';
    if (
      path.includes('/bo/stock') ||
      path.includes('/bo/productos') ||
      path.includes('/bo/importaciones') ||
      path.includes('/bo/compras')
    ) return 'inventario';
    if (
      path.includes('/bo/operacion') ||
      path.includes('/bo/onboarding') ||
      path.includes('/bo/kanban') ||
      path.includes('/bo/rrhh')
    ) return 'operacion';
    if (path.includes('/bo/admin/')) return 'administracion';
    if (path.includes('/bo/exportaciones') || path.includes('/bo/modulos') || path.includes('/bo/capacitacion')) return 'analitica';
    return 'inicio';
  }

  private syncOpenGroupWithRoute(): void {
    this.openGroup = this.groupForUrl(this.router.url);
  }

  toggleGroup(group: GroupKey): void {
    if (this.sidebarCollapsed) {
      this.sidebarCollapsed = false;
      this.persistSidebarState();
      this.applyBodyClasses();
    }
    this.openGroup = this.openGroup === group ? null : group;
  }

  isOpen(group: GroupKey): boolean {
    return this.openGroup === group;
  }

  toggleSidebar(): void {
    if (window.innerWidth <= 1024) {
      this.mobileOpen = !this.mobileOpen;
      this.applyBodyClasses();
      return;
    }
    this.sidebarCollapsed = !this.sidebarCollapsed;
    this.persistSidebarState();
    this.applyBodyClasses();
  }

  onNavigate(): void {
    this.syncOpenGroupWithRoute();
    if (window.innerWidth <= 1024) {
      this.mobileOpen = false;
      this.applyBodyClasses();
    }
  }

  closeMobileMenu(): void {
    this.mobileOpen = false;
    this.applyBodyClasses();
  }

  private restoreSidebarState(): void {
    const raw = localStorage.getItem(this.sidebarStorageKey);
    this.sidebarCollapsed = raw === '1';
  }

  private persistSidebarState(): void {
    localStorage.setItem(this.sidebarStorageKey, this.sidebarCollapsed ? '1' : '0');
  }

  private applyBodyClasses(): void {
    document.body.classList.add('bo-sidebar-enabled');
    document.body.classList.toggle('bo-sidebar-expanded', !this.sidebarCollapsed);
    document.body.classList.toggle('bo-sidebar-collapsed', this.sidebarCollapsed);
    document.body.classList.toggle('bo-sidebar-mobile-open', this.mobileOpen);
  }

  private clearBodyClasses(): void {
    document.body.classList.remove(
      'bo-sidebar-enabled',
      'bo-sidebar-expanded',
      'bo-sidebar-collapsed',
      'bo-sidebar-mobile-open'
    );
  }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    this.closeMobileMenu();
  }

  @HostListener('window:resize')
  onResize(): void {
    if (window.innerWidth > 1024 && this.mobileOpen) {
      this.mobileOpen = false;
      this.applyBodyClasses();
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
    private readonly router: Router
  ) {
    this.modules = this.operatingMode.getConfig().modules;
    this.canSeeProducts = this.authJwt.hasAnyRole(['Admin', 'Supervisor']);
    this.canSeeDataFlows = this.authJwt.hasAnyRole(['Admin', 'Supervisor', 'Manager']);
    this.canSeeOpsFlows = this.authJwt.hasAnyRole(['Admin', 'Supervisor', 'Manager']);
    this.canSeeAdminFlows = this.authJwt.hasAnyRole(['Admin', 'Supervisor']);
    this.restoreSidebarState();
    this.buildNavigation();
    this.syncOpenGroupWithRoute();
    this.routeSub = this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe(() => this.syncOpenGroupWithRoute());
    this.applyBodyClasses();
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
    this.clearBodyClasses();
  }
}
