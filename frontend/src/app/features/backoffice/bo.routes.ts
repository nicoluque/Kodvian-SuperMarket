import { Routes } from '@angular/router';
import { backofficeGuard } from '../../core/guards/backoffice.guard';
import { moduleAccessGuard } from '../../core/guards/module-access.guard';
import { BoImportacionesComponent } from '../../pages/bo-importaciones.component';
import { BoImportacionesStockInicialComponent } from '../../pages/bo-importaciones-stock-inicial.component';
import { BoDashboardGerencialComponent } from '../../pages/bo-dashboard-gerencial.component';
import { BoAdminEmpresaComponent } from '../../pages/bo-admin-empresa.component';
import { BoAdminLocalesComponent } from '../../pages/bo-admin-locales.component';
import { BoAdminLocalConfigComponent } from '../../pages/bo-admin-local-config.component';
import { BoAdminLocalUsuariosComponent } from '../../pages/bo-admin-local-usuarios.component';
import { BoOperacionChecklistComponent } from '../../pages/bo-operacion-checklist.component';
import { BoOperacionDescargasComponent } from '../../pages/bo-operacion-descargas.component';
import { BoOperacionPuestaEnMarchaComponent } from '../../pages/bo-operacion-puesta-en-marcha.component';
import { BoOnboardingComponent } from '../../pages/bo-onboarding.component';
import { BoAdminBrandingComponent } from '../../pages/bo-admin-branding.component';
import { BoAdminDemoComponent } from '../../pages/bo-admin-demo.component';
import { BoExportacionesComponent } from '../../pages/bo-exportaciones.component';
import { BoCapacitacionComponent } from '../../pages/bo-capacitacion.component';
import { BoComprasSugeridasComponent } from '../../pages/bo-compras-sugeridas.component';
import { BoComprasManualesComponent } from '../../pages/bo-compras-manuales.component';
import { BoProductosComponent } from '../../pages/bo-productos.component';
import { BoTotemTransitionsComponent } from '../../pages/bo-totem-transitions.component';
import { BoModulosComponent } from '../../pages/bo-modulos.component';
import { BoImportacionUnificadaComponent } from '../../pages/bo-importacion-unificada.component';
import { BoAjusteMasivoStockComponent } from '../../pages/bo-ajuste-masivo-stock.component';
import { PlaceholderPageComponent } from '../../shared/components/placeholder-page.component';

export const BO_ROUTES: Routes = [
  { path: 'login', pathMatch: 'full', redirectTo: '/inicio/login' },
  {
    path: '',
    canActivate: [backofficeGuard],
    data: { roles: ['Supervisor', 'Admin', 'Manager'] },
    children: [
      { path: 'dashboard', component: BoDashboardGerencialComponent },
      { path: 'dashboard-gerencial', component: BoDashboardGerencialComponent },
      { path: 'admin/empresa', component: BoAdminEmpresaComponent },
      { path: 'admin/locales', component: BoAdminLocalesComponent },
      { path: 'admin/locales/:id/configuracion', component: BoAdminLocalConfigComponent },
      { path: 'admin/locales/:id/usuarios', component: BoAdminLocalUsuariosComponent },
      { path: 'admin/branding', component: BoAdminBrandingComponent },
      { path: 'admin/demo', component: BoAdminDemoComponent },
      { path: 'productos', component: BoProductosComponent, canActivate: [backofficeGuard], data: { roles: ['Admin', 'Supervisor'] } },
      { path: 'compras', pathMatch: 'full', redirectTo: 'compras/manuales' },
      { path: 'compras/manuales', component: BoComprasManualesComponent, canActivate: [moduleAccessGuard], data: { module: 'comprasSugeridas' } },
      { path: 'compras/sugeridas', component: BoComprasSugeridasComponent, canActivate: [moduleAccessGuard], data: { module: 'comprasSugeridas' } },
      { path: 'stock', component: PlaceholderPageComponent, data: { title: 'Backoffice - Stock' } },
      { path: 'totem/transiciones', component: BoTotemTransitionsComponent, canActivate: [backofficeGuard], data: { roles: ['Admin', 'Supervisor'] } },
      { path: 'claims', component: PlaceholderPageComponent, data: { title: 'Backoffice - Reclamos' } },
      { path: 'clientes', component: PlaceholderPageComponent, data: { title: 'Backoffice - Clientes' } },
      { path: 'reportes', pathMatch: 'full', redirectTo: 'exportaciones' },
      { path: 'importaciones', component: BoImportacionesComponent },
      { path: 'importaciones/stock-inicial', component: BoImportacionesStockInicialComponent },
      { path: 'importaciones/unificada', component: BoImportacionUnificadaComponent },
      { path: 'importaciones/ajuste-masivo-stock', component: BoAjusteMasivoStockComponent },
      { path: 'operacion/descargas', component: BoOperacionDescargasComponent },
      { path: 'operacion/puesta-en-marcha', component: BoOperacionPuestaEnMarchaComponent },
      { path: 'operacion/checklist', component: BoOperacionChecklistComponent },
      { path: 'onboarding', component: BoOnboardingComponent },
      { path: 'exportaciones', component: BoExportacionesComponent, canActivate: [moduleAccessGuard], data: { module: 'reportes' } },
      { path: 'capacitacion', component: BoCapacitacionComponent },
      { path: 'modulos', component: BoModulosComponent },
      { path: 'rrhh', component: PlaceholderPageComponent, data: { title: 'Backoffice - RRHH' } },
      { path: 'kanban', component: PlaceholderPageComponent, data: { title: 'Backoffice - Kanban' } },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' }
    ]
  }
];
