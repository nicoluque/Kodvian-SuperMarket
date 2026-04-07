import { Routes } from '@angular/router';
import { inicioSessionGuard } from './core/guards/inicio-session.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'inicio/login' },
  {
    path: 'inicio/login',
    loadComponent: () => import('./pages/inicio-login.component').then(m => m.InicioLoginComponent)
  },
  {
    path: 'inicio',
    canActivate: [inicioSessionGuard],
    loadComponent: () => import('./pages/inicio-operativo.component').then(m => m.InicioOperativoComponent)
  },

  {
    path: 'pos',
    loadChildren: () => import('./features/pos/pos.routes').then(m => m.POS_ROUTES)
  },
  {
    path: 'bo',
    loadChildren: () => import('./features/backoffice/bo.routes').then(m => m.BO_ROUTES)
  },
  {
    path: 'print/sale/:id',
    loadComponent: () => import('./pages/print-sale.component').then(m => m.PrintSaleComponent)
  },
  {
    path: 'print/customer-payment/:id',
    loadComponent: () => import('./pages/print-customer-payment.component').then(m => m.PrintCustomerPaymentComponent)
  },
  {
    path: 'print/return/:id',
    loadComponent: () => import('./pages/print-return.component').then(m => m.PrintReturnComponent)
  },
  {
    path: 'print/cash-movement/:id',
    loadComponent: () => import('./pages/print-cash-movement.component').then(m => m.PrintCashMovementComponent)
  },
  {
    path: 'print/cash-close/:id',
    loadComponent: () => import('./pages/print-cash-close.component').then(m => m.PrintCashCloseComponent)
  },

  { path: '**', redirectTo: 'inicio/login' }
];
