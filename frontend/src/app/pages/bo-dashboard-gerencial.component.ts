import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BoDashboardService } from '../core/services/bo-dashboard.service';
import { BoExportsService } from '../core/services/bo-exports.service';
import { OperatingModeService } from '../core/services/operating-mode.service';
import { BoModuleNavComponent } from '../shared/components/bo-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-bo-dashboard-gerencial',
  imports: [CommonModule, RouterLink, BoModuleNavComponent],
  template: `
    <main class="dashboard-container">
      <div class="hero-bg">
        <div class="hero-shape shape-1"></div>
        <div class="hero-shape shape-2"></div>
      </div>

      <app-bo-module-nav />

      <header class="hero">
        <div class="hero-content">
          <h1>Dashboard gerencial</h1>
          <p class="hero-subtitle">Resumen de operaciones y métricas clave</p>
        </div>
        <div class="hero-actions">
          <button class="btn-primary" (click)="exportSalesRange()" [disabled]="isBusy">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
              <polyline points="7 10 12 15 17 10"></polyline>
              <line x1="12" y1="15" x2="12" y2="3"></line>
            </svg>
            Exportar ventas
          </button>
          <a class="btn-secondary" routerLink="/bo/exportaciones">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
              <polyline points="14 2 14 8 20 8"></polyline>
              <line x1="16" y1="13" x2="8" y2="13"></line>
              <line x1="16" y1="17" x2="8" y2="17"></line>
              <polyline points="10 9 9 9 8 9"></polyline>
            </svg>
            Centro exportaciones
          </a>
          <button class="btn-ghost" (click)="exportPendingTransfers()" [disabled]="isBusy">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="17 1 21 5 17 9"></polyline>
              <path d="M3 11V9a4 4 0 0 1 4-4h14"></path>
              <polyline points="7 23 3 19 7 15"></polyline>
              <path d="M21 13v2a4 4 0 0 1-4 4H3"></path>
            </svg>
            Transferencias pendientes
          </button>
        </div>
      </header>

      <section class="quick-access">
        <a class="quick-pill" routerLink="/bo/importaciones/unificada">Importacion unificada</a>
        <a class="quick-pill" routerLink="/bo/importaciones/ajuste-masivo-stock">Ajuste masivo stock</a>
        <a class="quick-pill" routerLink="/bo/importaciones">Importar catalogo (solo datos)</a>
        <a class="quick-pill" routerLink="/bo/importaciones/stock-inicial">Stock inicial (B22)</a>
        <a class="quick-pill" routerLink="/bo/compras/manuales">Compras manuales</a>
        <a class="quick-pill" routerLink="/bo/compras/sugeridas">Compras sugeridas</a>
        <a class="quick-pill" routerLink="/bo/operacion/checklist">Checklist operativo</a>
        <a class="quick-pill" routerLink="/bo/modulos">Ver mapa de módulos</a>
      </section>

      <div class="alert warning" *ngIf="pendingYieldRecalibrations > 0">
        <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path>
          <line x1="12" y1="9" x2="12" y2="13"></line>
          <line x1="12" y1="17" x2="12.01" y2="17"></line>
        </svg>
        Hay {{ pendingYieldRecalibrations }} recalibración{{ pendingYieldRecalibrations === 1 ? '' : 'es' }} de rendimiento pendiente{{ pendingYieldRecalibrations === 1 ? '' : 's' }} en Transformaciones.
        <a routerLink="/bo/stock">Revisar ahora</a>
      </div>

      <section class="range-filters">
        <button 
          class="filter-chip" 
          [class.active]="activeRange === 'today'"
          (click)="setRange('today')">
          Hoy
        </button>
        <button 
          class="filter-chip" 
          [class.active]="activeRange === '7d'"
          (click)="setRange('7d')">
          Últimos 7 días
        </button>
        <button 
          class="filter-chip" 
          [class.active]="activeRange === 'month'"
          (click)="setRange('month')">
          Mes actual
        </button>
      </section>

      <section class="kpis" *ngIf="summary">
        <article class="kpi-card">
          <div class="kpi-icon sales">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="12" y1="1" x2="12" y2="23"></line>
              <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"></path>
            </svg>
          </div>
          <div class="kpi-content">
            <span class="kpi-label">Ventas</span>
            <span class="kpi-value">{{ summary.sales | number:'1.2-2' }}</span>
          </div>
        </article>

        <article class="kpi-card">
          <div class="kpi-icon tickets">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M2 9a3 3 0 0 1 3-3h14a3 3 0 0 1 3 3v0a3 3 0 0 1-3 3H5a3 3 0 0 1-3-3v0z"></path>
              <path d="M2 15a3 3 0 0 1 3-3h14a3 3 0 0 1 3 3v0a3 3 0 0 1-3 3H5a3 3 0 0 1-3-3v0z"></path>
            </svg>
          </div>
          <div class="kpi-content">
            <span class="kpi-label">Tickets</span>
            <span class="kpi-value">{{ summary.tickets }}</span>
          </div>
        </article>

        <article class="kpi-card">
          <div class="kpi-icon avg">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"></circle>
              <polyline points="12 6 12 12 16 14"></polyline>
            </svg>
          </div>
          <div class="kpi-content">
            <span class="kpi-label">Ticket promedio</span>
            <span class="kpi-value">{{ summary.avgTicket | number:'1.2-2' }}</span>
          </div>
        </article>

        <article class="kpi-card">
          <div class="kpi-icon transfer">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="17 1 21 5 17 9"></polyline>
              <path d="M3 11V9a4 4 0 0 1 4-4h14"></path>
              <polyline points="7 23 3 19 7 15"></polyline>
              <path d="M21 13v2a4 4 0 0 1-4 4H3"></path>
            </svg>
          </div>
          <div class="kpi-content">
            <span class="kpi-label">Pend. transferencia</span>
            <span class="kpi-value">{{ summary.pendingTransfers }}</span>
          </div>
        </article>

        <article class="kpi-card">
          <div class="kpi-icon credit">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="1" y="4" width="22" height="16" rx="2" ry="2"></rect>
              <line x1="1" y1="10" x2="23" y2="10"></line>
            </svg>
          </div>
          <div class="kpi-content">
            <span class="kpi-label">Cuenta corriente</span>
            <span class="kpi-value">{{ summary.accountBalance | number:'1.2-2' }}</span>
          </div>
        </article>

        <article class="kpi-card">
          <div class="kpi-icon waste">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="3 6 5 6 21 6"></polyline>
              <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
            </svg>
          </div>
          <div class="kpi-content">
            <span class="kpi-label">Merma</span>
            <span class="kpi-value">{{ summary.waste | number:'1.2-2' }}</span>
          </div>
        </article>

        <article class="kpi-card">
          <div class="kpi-icon supplier">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"></path>
              <rect x="8" y="2" width="8" height="4" rx="1" ry="1"></rect>
            </svg>
          </div>
          <div class="kpi-content">
            <span class="kpi-label">Créditos proveedor</span>
            <span class="kpi-value">{{ summary.supplierCredits | number:'1.2-2' }}</span>
          </div>
        </article>
      </section>

      <section class="loading-state" *ngIf="isBusy">
        <div class="spinner"></div>
        <p>Cargando datos...</p>
      </section>

      <div class="charts-grid" *ngIf="!isBusy">
        <article class="chart-card">
          <div class="card-header">
            <h3>Ventas últimos 7 días</h3>
          </div>
          <div class="card-body">
            <div class="bar-chart">
              <div class="bar-item" *ngFor="let s of salesSeries">
                <div class="bar-track">
                  <div class="bar-fill sales-fill" [style.height.%]="barWidth(s.total, maxSales)"></div>
                </div>
                <span class="bar-label">{{ s.date }}</span>
                <span class="bar-value">{{ s.total | number:'1.0-0' }}</span>
              </div>
            </div>
          </div>
        </article>

        <article class="chart-card">
          <div class="card-header">
            <h3>Medios de pago</h3>
          </div>
          <div class="card-body">
            <div class="bar-chart horizontal">
              <div class="bar-item" *ngFor="let p of paymentItems">
                <span class="bar-label">{{ p.method }}</span>
                <div class="bar-track">
                  <div class="bar-fill payment-fill" [style.width.%]="barWidth(p.amount, maxPayments)"></div>
                </div>
                <span class="bar-value">{{ p.amount | number:'1.0-0' }}</span>
              </div>
            </div>
          </div>
        </article>

        <article class="chart-card">
          <div class="card-header">
            <h3>Ventas por turno</h3>
          </div>
          <div class="card-body">
            <div class="bar-chart horizontal">
              <div class="bar-item" *ngFor="let s of salesByShift">
                <span class="bar-label">{{ s.shift }}</span>
                <div class="bar-track">
                  <div class="bar-fill shift-fill" [style.width.%]="barWidth(s.totalSales, maxShiftSales)"></div>
                </div>
                <span class="bar-value">{{ s.totalSales | number:'1.0-0' }}</span>
              </div>
            </div>
          </div>
        </article>
      </div>

      <section class="tables-grid" *ngIf="!isBusy">
        <article class="table-card">
          <div class="card-header">
            <h3>Top deudores fiado</h3>
          </div>
          <div class="card-body">
            <div class="table-wrapper">
              <table>
                <thead>
                  <tr>
                    <th>Cliente</th>
                    <th>Deuda</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let x of topCredit">
                    <td>{{ x.customerName }}</td>
                    <td class="amount">{{ x.debt | number:'1.2-2' }}</td>
                  </tr>
                  <tr *ngIf="topCredit.length === 0">
                    <td colspan="2" class="empty">No hay deudores.</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </article>

        <article class="table-card">
          <div class="card-header">
            <h3>Top deudores envases</h3>
          </div>
          <div class="card-body">
            <div class="table-wrapper">
              <table>
                <thead>
                  <tr>
                    <th>Cliente</th>
                    <th>Cant.</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let x of topContainers">
                    <td>{{ x.customerName }}</td>
                    <td class="amount">{{ x.containerDebtQty | number:'1.2-2' }}</td>
                  </tr>
                  <tr *ngIf="topContainers.length === 0">
                    <td colspan="2" class="empty">No hay deudores.</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </article>

        <article class="table-card">
          <div class="card-header">
            <h3>Stock crítico</h3>
          </div>
          <div class="card-body">
            <div class="table-wrapper">
              <table>
                <thead>
                  <tr>
                    <th>Producto</th>
                    <th>Vendible</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let x of criticalStock">
                    <td>{{ x.productName }}</td>
                    <td class="amount warning">{{ x.vendibleQty }}</td>
                  </tr>
                  <tr *ngIf="criticalStock.length === 0">
                    <td colspan="2" class="empty">No hay stock crítico.</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </article>

        <article class="table-card">
          <div class="card-header">
            <h3>Pendientes transferencia</h3>
          </div>
          <div class="card-body">
            <div class="table-wrapper">
              <table>
                <thead>
                  <tr>
                    <th>Venta</th>
                    <th>Total</th>
                    <th>Comprobante</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let x of pendingTransfers">
                    <td>#{{ x.id }}</td>
                    <td class="amount">{{ x.total | number:'1.2-2' }}</td>
                    <td>
                      <a [href]="'/print/sale/' + x.id" target="_blank" class="action-link">Ver</a>
                      <span class="sep">·</span>
                      <a [href]="'/print/sale/' + x.id + '?autoprint=1'" target="_blank" class="action-link">Imprimir</a>
                      <span class="sep">·</span>
                      <a [href]="'/print/sale/' + x.id + '?autoprint=1&reprint=1'" target="_blank" class="action-link">Reimprimir</a>
                    </td>
                  </tr>
                  <tr *ngIf="pendingTransfers.length === 0">
                    <td colspan="3" class="empty">No hay pendientes de transferencia.</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </article>

        <article class="table-card">
          <div class="card-header">
            <h3>Reclamos pendientes</h3>
          </div>
          <div class="card-body">
            <div class="table-wrapper">
              <table>
                <thead>
                  <tr>
                    <th>Claim</th>
                    <th>Estado</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let x of pendingClaims">
                    <td>#{{ x.id }}</td>
                    <td><span class="status-badge">{{ x.status }}</span></td>
                  </tr>
                  <tr *ngIf="pendingClaims.length === 0">
                    <td colspan="2" class="empty">No hay reclamos pendientes.</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </article>
      </section>

      <div class="alert error" *ngIf="error">
        <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <circle cx="12" cy="12" r="10"></circle>
          <line x1="15" y1="9" x2="9" y2="15"></line>
          <line x1="9" y1="9" x2="15" y2="15"></line>
        </svg>
        {{ error }}
      </div>
    </main>
  `,
  styles: [`
    .dashboard-container {
      min-height: 100vh;
      padding: 0 0 2rem 0;
      position: relative;
      overflow: hidden;
    }

    .dashboard-container > app-bo-module-nav {
      position: relative;
      z-index: 2;
      display: block;
      padding: 0.75rem 2rem 0;
    }

    .hero-bg {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      height: 320px;
      background: linear-gradient(135deg, #1B4D3E 0%, #234F45 100%);
      overflow: hidden;
      z-index: 0;
      pointer-events: none;
    }

    .hero-shape {
      position: absolute;
      border-radius: 50%;
      filter: blur(80px);
      opacity: 0.4;
    }

    .shape-1 {
      width: 400px;
      height: 400px;
      background: #BFEBF1;
      top: -150px;
      right: -100px;
    }

    .shape-2 {
      width: 300px;
      height: 300px;
      background: #1B4D3E;
      bottom: -100px;
      left: -50px;
    }

    .hero {
      position: relative;
      z-index: 1;
      padding: 1.5rem 2rem 0;
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      flex-wrap: wrap;
      gap: 1rem;
    }

    .hero-content h1 {
      margin: 0;
      font-size: 1.75rem;
      font-weight: 700;
      color: #FFFFFF;
    }

    .hero-subtitle {
      margin: 0.25rem 0 0;
      color: #BFEBF1;
      font-size: 0.9375rem;
    }

    .hero-actions {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
    }

    .quick-access {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
      margin-top: 0.75rem;
    }

    .quick-pill {
      text-decoration: none;
      color: #1f7f57;
      background: #e7f4ee;
      border: 1px solid #c6e5d6;
      border-radius: 999px;
      padding: 7px 11px;
      font-size: 13px;
      font-weight: 600;
    }

    .btn-primary {
      padding: 0.625rem 1rem;
      background: #FFFFFF;
      color: #1B4D3E;
      border: none;
      border-radius: 10px;
      font-size: 0.875rem;
      font-weight: 600;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 0.5rem;
      transition: background 0.2s, transform 0.1s;
    }

    .btn-primary:hover:not(:disabled) {
      background: #E8F5F4;
    }

    .btn-primary:active:not(:disabled) {
      transform: scale(0.98);
    }

    .btn-primary:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .btn-secondary {
      padding: 0.625rem 1rem;
      background: rgba(255, 255, 255, 0.15);
      color: #FFFFFF;
      border: 1px solid rgba(255, 255, 255, 0.3);
      border-radius: 10px;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      text-decoration: none;
      display: flex;
      align-items: center;
      gap: 0.5rem;
      transition: background 0.2s;
    }

    .btn-secondary:hover {
      background: rgba(255, 255, 255, 0.25);
    }

    .btn-ghost {
      padding: 0.625rem 1rem;
      background: transparent;
      color: #BFEBF1;
      border: none;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 0.5rem;
      transition: color 0.2s;
    }

    .btn-ghost:hover:not(:disabled) {
      color: #FFFFFF;
    }

    .btn-ghost:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .range-filters {
      position: relative;
      z-index: 1;
      padding: 1rem 2rem;
      display: flex;
      gap: 0.5rem;
    }

    .filter-chip {
      padding: 0.5rem 1rem;
      background: rgba(255, 255, 255, 0.1);
      color: #BFEBF1;
      border: 1px solid rgba(255, 255, 255, 0.2);
      border-radius: 999px;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s;
    }

    .filter-chip:hover {
      background: rgba(255, 255, 255, 0.2);
    }

    .filter-chip.active {
      background: #FFFFFF;
      color: #1B4D3E;
      border-color: #FFFFFF;
    }

    .kpis {
      position: relative;
      z-index: 1;
      padding: 0 2rem;
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
      gap: 1rem;
    }

    .kpi-card {
      background: #FFFFFF;
      border-radius: 16px;
      padding: 1.25rem;
      display: flex;
      align-items: center;
      gap: 1rem;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
      transition: transform 0.2s, box-shadow 0.2s;
    }

    .kpi-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 8px 30px rgba(0, 0, 0, 0.12);
    }

    .kpi-icon {
      width: 48px;
      height: 48px;
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .kpi-icon.sales { background: #E8F5F4; color: #1B4D3E; }
    .kpi-icon.tickets { background: #F0FDF4; color: #16A34A; }
    .kpi-icon.avg { background: #FEF3C7; color: #D97706; }
    .kpi-icon.transfer { background: #EFF6FF; color: #2563EB; }
    .kpi-icon.credit { background: #F5F3FF; color: #7C3AED; }
    .kpi-icon.waste { background: #FEF2F2; color: #DC2626; }
    .kpi-icon.supplier { background: #FFF7ED; color: #EA580C; }

    .kpi-content {
      display: flex;
      flex-direction: column;
      min-width: 0;
    }

    .kpi-label {
      font-size: 0.75rem;
      color: #6B7280;
      text-transform: uppercase;
      letter-spacing: 0.03em;
    }

    .kpi-value {
      font-size: 1.375rem;
      font-weight: 700;
      color: #1B4D3E;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .loading-state {
      position: relative;
      z-index: 1;
      padding: 3rem;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
      color: #6B7280;
    }

    .spinner {
      width: 40px;
      height: 40px;
      border: 3px solid #E5E7EB;
      border-top-color: #1B4D3E;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .charts-grid {
      position: relative;
      z-index: 1;
      padding: 1.5rem 2rem;
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(340px, 1fr));
      gap: 1.5rem;
    }

    .chart-card, .table-card {
      background: #FFFFFF;
      border-radius: 16px;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.06);
      overflow: hidden;
    }

    .card-header {
      padding: 1rem 1.25rem;
      border-bottom: 1px solid #F3F4F6;
    }

    .card-header h3 {
      margin: 0;
      font-size: 0.9375rem;
      font-weight: 600;
      color: #1B4D3E;
    }

    .card-body {
      padding: 1rem 1.25rem;
    }

    .bar-chart {
      display: flex;
      align-items: flex-end;
      gap: 0.5rem;
      height: 160px;
    }

    .bar-chart.horizontal {
      flex-direction: column;
      height: auto;
      align-items: stretch;
    }

    .bar-item {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.25rem;
      min-width: 0;
    }

    .bar-chart.horizontal .bar-item {
      flex-direction: row;
      align-items: center;
      gap: 0.5rem;
    }

    .bar-track {
      width: 100%;
      height: 100px;
      background: #F3F4F6;
      border-radius: 6px;
      overflow: hidden;
      display: flex;
      align-items: flex-end;
    }

    .bar-chart.horizontal .bar-track {
      width: 120px;
      height: 24px;
      flex-shrink: 0;
    }

    .bar-fill {
      width: 100%;
      border-radius: 6px;
      transition: height 0.3s ease;
    }

    .bar-chart.horizontal .bar-fill {
      height: 100%;
      width: auto;
    }

    .sales-fill { background: linear-gradient(180deg, #1B4D3E 0%, #2D6A5A 100%); }
    .payment-fill { background: linear-gradient(90deg, #3B82F6 0%, #60A5FA 100%); }
    .shift-fill { background: linear-gradient(90deg, #F59E0B 0%, #FBBF24 100%); }

    .bar-label {
      font-size: 0.6875rem;
      color: #6B7280;
      text-align: center;
    }

    .bar-chart.horizontal .bar-label {
      width: 50px;
      text-align: left;
      flex-shrink: 0;
    }

    .bar-value {
      font-size: 0.75rem;
      font-weight: 600;
      color: #1B4D3E;
    }

    .tables-grid {
      position: relative;
      z-index: 1;
      padding: 0 2rem;
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
      gap: 1.5rem;
    }

    .table-wrapper {
      overflow-x: auto;
    }

    table {
      width: 100%;
      border-collapse: collapse;
    }

    th {
      text-align: left;
      font-size: 0.6875rem;
      font-weight: 600;
      color: #6B7280;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      padding: 0.5rem 0;
      border-bottom: 1px solid #E5E7EB;
    }

    td {
      padding: 0.75rem 0;
      font-size: 0.875rem;
      color: #374151;
      border-bottom: 1px solid #F3F4F6;
    }

    tr:last-child td {
      border-bottom: none;
    }

    .amount {
      text-align: right;
      font-weight: 600;
      font-variant-numeric: tabular-nums;
    }

    .amount.warning {
      color: #DC2626;
    }

    .empty {
      text-align: center;
      color: #9CA3AF;
      font-style: italic;
      padding: 1.5rem 0 !important;
    }

    .action-link {
      color: #1B4D3E;
      text-decoration: none;
      font-weight: 500;
      font-size: 0.8125rem;
    }

    .action-link:hover {
      text-decoration: underline;
    }

    .sep {
      color: #D1D5DB;
      margin: 0 0.25rem;
    }

    .status-badge {
      display: inline-block;
      padding: 0.25rem 0.5rem;
      background: #FEF3C7;
      color: #D97706;
      border-radius: 999px;
      font-size: 0.6875rem;
      font-weight: 600;
      text-transform: uppercase;
    }

    .alert {
      position: relative;
      z-index: 1;
      margin: 1rem 2rem;
      padding: 1rem;
      border-radius: 12px;
      display: flex;
      align-items: center;
      gap: 0.75rem;
      font-size: 0.9375rem;
    }

    .alert.error {
      background: #FEF2F2;
      border: 1px solid #FECACA;
      color: #DC2626;
    }

    .alert.warning {
      background: #FFF7ED;
      border: 1px solid #FED7AA;
      color: #C2410C;
    }

    .alert.warning a {
      margin-left: auto;
      color: #9A3412;
      border: 1px solid #FDBA74;
      background: #FFEDD5;
      border-radius: 999px;
      font-weight: 700;
      text-decoration: none;
      padding: 0.25rem 0.625rem;
      white-space: nowrap;
    }

    .alert.warning a:hover {
      background: #FED7AA;
    }

    @media (max-width: 768px) {
      .hero {
        padding: 1rem;
        flex-direction: column;
      }

      .hero-actions {
        width: 100%;
      }

      .btn-primary, .btn-secondary, .btn-ghost {
        flex: 1;
        justify-content: center;
      }

      .range-filters {
        padding: 1rem;
        overflow-x: auto;
      }

      .kpis {
        padding: 1rem;
        grid-template-columns: repeat(2, 1fr);
      }

      .kpi-card {
        padding: 1rem;
      }

      .kpi-icon {
        width: 40px;
        height: 40px;
      }

      .kpi-value {
        font-size: 1.125rem;
      }

      .charts-grid, .tables-grid {
        padding: 1rem;
      }

      .alert {
        margin: 1rem;
      }
    }

    @media (max-width: 480px) {
      .kpis {
        grid-template-columns: 1fr;
      }

      .charts-grid, .tables-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class BoDashboardGerencialComponent {
  summary: any = null;
  salesSeries: any[] = [];
  paymentItems: any[] = [];
  salesByShift: any[] = [];
  topCredit: any[] = [];
  topContainers: any[] = [];
  criticalStock: any[] = [];
  pendingTransfers: any[] = [];
  pendingClaims: any[] = [];
  pendingYieldRecalibrations = 0;
  error = '';
  isBusy = false;
  activeRange: 'today' | '7d' | 'month' = '7d';

  from = '';
  to = '';

  constructor(
    private readonly api: BoDashboardService,
    private readonly exportsApi: BoExportsService,
    private readonly operatingMode: OperatingModeService
  ) {
    this.setRange('7d');
  }

  async exportSalesRange(): Promise<void> {
    try {
      await this.exportsApi.download('/sales/range', { from: this.from, to: this.to, format: 'xlsx' });
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo exportar ventas';
    }
  }

  async exportPendingTransfers(): Promise<void> {
    try {
      await this.exportsApi.download('/transfers/pending', { format: 'xlsx' });
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo exportar transferencias';
    }
  }

  get maxSales(): number {
    return Math.max(1, ...this.salesSeries.map(x => Number(x.total || 0)));
  }

  get maxPayments(): number {
    return Math.max(1, ...this.paymentItems.map(x => Number(x.amount || 0)));
  }

  get maxShiftSales(): number {
    return Math.max(1, ...this.salesByShift.map(x => Number(x.totalSales || 0)));
  }

  barWidth(v: number, max: number): number {
    return Math.round((Number(v || 0) / Math.max(1, max)) * 100);
  }

  setRange(kind: 'today' | '7d' | 'month'): void {
    this.activeRange = kind;
    const now = new Date();
    const yyyy = now.getFullYear();
    const mm = String(now.getMonth() + 1).padStart(2, '0');
    const dd = String(now.getDate()).padStart(2, '0');

    if (kind === 'today') {
      this.from = `${yyyy}-${mm}-${dd}`;
      this.to = `${yyyy}-${mm}-${dd}`;
    } else if (kind === '7d') {
      const d = new Date(now);
      d.setDate(d.getDate() - 6);
      this.from = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
      this.to = `${yyyy}-${mm}-${dd}`;
    } else {
      this.from = `${yyyy}-${mm}-01`;
      this.to = `${yyyy}-${mm}-${dd}`;
    }

    void this.load();
  }

  async load(): Promise<void> {
    this.error = '';
    this.isBusy = true;
    try {
      const [summary, salesSeries, paymentMethods, topCredit, topContainers, criticalStock, operations, pendingRecalibrations] = await Promise.all([
        this.api.summary(this.from, this.to),
        this.api.salesSeries(7),
        this.api.paymentMethods(this.from, this.to),
        this.api.topCreditDebtors(10),
        this.api.topContainerDebtors(10),
        this.api.criticalStock(),
        this.api.operationsSummary(),
        this.api.transformationPendingRecalibrations()
      ]);

      this.summary = summary;
      this.salesSeries = salesSeries;
      this.paymentItems = paymentMethods.items ?? [];
      this.topCredit = topCredit;
      this.topContainers = topContainers;
      this.criticalStock = criticalStock;
      this.pendingTransfers = operations.pendingTransfers ?? [];
      this.pendingClaims = operations.pendingClaims ?? [];
      this.salesByShift = operations.salesByShift ?? [];
      this.pendingYieldRecalibrations = Array.isArray(pendingRecalibrations) ? pendingRecalibrations.length : 0;
    } catch (err: any) {
      this.pendingYieldRecalibrations = 0;
      const status = err?.status;
      if (status === 401 || status === 403) {
        this.error = 'Tu usuario no tiene acceso al dashboard o la sesión expiró. Ingresá con un usuario Admin o Supervisor.';
      } else {
        this.error = err?.error?.message ?? 'No se pudo cargar dashboard gerencial';
      }
    } finally {
      this.isBusy = false;
    }
  }
}
