import { Routes } from '@angular/router';
import { deviceTypeChildGuard, deviceTypeGuard } from '../../core/guards/device-type.guard';
import { moduleAccessGuard } from '../../core/guards/module-access.guard';
import { posGuard } from '../../core/guards/pos.guard';
import { PosSetupComponent } from '../../pages/pos-setup.component';
import { PosCajaAperturaComponent } from '../../pages/pos/pos-caja-apertura.component';
import { PosCajaDevolucionesComponent } from '../../pages/pos/pos-caja-devoluciones.component';
import { PosCajaEnvasesComponent } from '../../pages/pos/pos-caja-envases.component';
import { PosCajaCierreComponent, PosCajaCobroComponent, PosCajaInboxComponent, PosCajaVentaComponent } from '../../pages/pos/pos-caja-inbox.component';
import { PosCajaMovimientosComponent } from '../../pages/pos/pos-caja-movimientos.component';
import { PosCajaOfflineQueueComponent } from '../../pages/pos/pos-caja-offline-queue.component';
import { PosCajaPagoCuentaComponent } from '../../pages/pos/pos-caja-pago-cuenta.component';
import { PosCajaPendientesComponent } from '../../pages/pos/pos-caja-pendientes.component';
import { PosTabletCarritoComponent } from '../../pages/pos/pos-tablet-carrito.component';
import { PosTabletNuevaComponent } from '../../pages/pos/pos-tablet-nueva.component';
import { PosEntryRedirectComponent } from '../../pages/pos-entry-redirect.component';
import { PlaceholderPageComponent } from '../../shared/components/placeholder-page.component';

export const POS_ROUTES: Routes = [
  { path: 'setup', component: PosSetupComponent },
  {
    path: 'caja',
    canActivate: [posGuard, deviceTypeGuard],
    canActivateChild: [deviceTypeChildGuard],
    data: { requiredDeviceType: 'CashRegister' },
    children: [
      { path: 'apertura', component: PosCajaAperturaComponent },
      { path: 'inbox', component: PosCajaInboxComponent },
      { path: 'venta/:cartId', component: PosCajaVentaComponent },
      { path: 'cobro/:cartId', component: PosCajaCobroComponent },
      { path: 'pendientes-transfer', component: PosCajaPendientesComponent },
      { path: 'pendientes', component: PosCajaPendientesComponent },
      { path: 'pago-cuenta', component: PosCajaPagoCuentaComponent, canActivate: [moduleAccessGuard], data: { module: 'cuentaCorriente' } },
      { path: 'devoluciones', component: PosCajaDevolucionesComponent },
      { path: 'envases', component: PosCajaEnvasesComponent, canActivate: [moduleAccessGuard], data: { module: 'envases' } },
      { path: 'movimientos', component: PosCajaMovimientosComponent },
      { path: 'offline-queue', component: PosCajaOfflineQueueComponent },
      { path: 'cierre', component: PosCajaCierreComponent },
      { path: '', pathMatch: 'full', redirectTo: 'apertura' }
    ]
  },
  {
    path: 'tablet',
    canActivate: [posGuard, moduleAccessGuard, deviceTypeGuard],
    canActivateChild: [deviceTypeChildGuard],
    data: { module: 'tablet', requiredDeviceType: 'Tablet' },
    children: [
      { path: 'nueva', component: PosTabletNuevaComponent },
      { path: 'carrito/:cartId', component: PosTabletCarritoComponent },
      { path: 'qr', component: PlaceholderPageComponent, data: { title: 'Tablet - Cobro QR' } },
      { path: '', pathMatch: 'full', redirectTo: 'nueva' }
    ]
  },
  { path: '', canActivate: [posGuard], component: PosEntryRedirectComponent }
];
