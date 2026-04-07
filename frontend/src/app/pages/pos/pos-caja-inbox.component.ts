import { CommonModule } from '@angular/common';
import { Component, ElementRef, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ActivityService } from '../../core/services/activity.service';
import { DialogService } from '../../core/services/dialog.service';
import { HealthService } from '../../core/services/health.service';
import { NotificationsService } from '../../core/services/notifications.service';
import { OfflineQueueService } from '../../core/services/offline-queue.service';
import { OperatorSessionService } from '../../core/services/operator-session.service';
import { CartInboxItem, CartItem, CashSessionCloseResponse, CashSessionHandoverResponse, CashSessionResponse, CashSessionSaleSummary, CigaretteStockBalance, CustomerRef, PendingTransferSale, PosCajaService, ProductLookupResponse } from '../../core/services/pos-caja.service';
import { PosModuleNavComponent } from '../../shared/components/pos-module-nav.component';

interface PaymentDraft {
  paymentMethod: string;
  amount: number;
  reference?: string;
  isPending: boolean;
}

interface CigaretteCountDraftRow {
  productId: number;
  productName: string;
  systemQty: number;
  countedQty: number;
}

@Component({
  standalone: true,
  selector: 'app-pos-caja-inbox',
  imports: [CommonModule, FormsModule, RouterLink, PosModuleNavComponent],
  template: `
    <main class="inbox-container">
      <div class="hero-bg">
        <div class="hero-shape shape-1"></div>
        <div class="hero-shape shape-2"></div>
      </div>

      <app-pos-module-nav />

      <header class="hero">
        <div class="hero-content">
          <h1>Bandeja de caja</h1>
          <p class="hero-subtitle">Selecciona un carrito para continuar con venta o cobro</p>
        </div>
        <div class="hero-actions">
          <button class="btn-primary" [disabled]="isBusy" (click)="createCartAndOpen()">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="12" y1="5" x2="12" y2="19"></line>
              <line x1="5" y1="12" x2="19" y2="12"></line>
            </svg>
            Nuevo carrito
          </button>
          <button class="btn-secondary" [disabled]="isBusy" (click)="refreshAll()">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="23 4 23 10 17 10"></polyline>
              <polyline points="1 20 1 14 7 14"></polyline>
              <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
            </svg>
            Actualizar
          </button>
          <a class="btn-ghost" routerLink="/pos/caja/cierre">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
              <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
            </svg>
            Ir a cierre
          </a>
          <a class="btn-ghost" routerLink="/inicio">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M3 11l9-8 9 8"></path>
              <path d="M5 10v10h14V10"></path>
            </svg>
            Ir a inicio
          </a>
        </div>
      </header>

      <section class="summary-section">
        <div class="summary-grid">
          <article class="summary-card">
            <div class="summary-icon pending">
              <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="9" cy="21" r="1"></circle>
                <circle cx="20" cy="21" r="1"></circle>
                <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path>
              </svg>
            </div>
            <div class="summary-content">
              <span class="summary-label">Carritos pendientes</span>
              <span class="summary-value">{{ inbox.length }}</span>
            </div>
          </article>

          <article class="summary-card">
            <div class="summary-icon transfer">
              <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="17 1 21 5 17 9"></polyline>
                <path d="M3 11V9a4 4 0 0 1 4-4h14"></path>
                <polyline points="7 23 3 19 7 15"></polyline>
                <path d="M21 13v2a4 4 0 0 1-4 4H3"></path>
              </svg>
            </div>
            <div class="summary-content">
              <span class="summary-label">Transferencias del turno</span>
              <span class="summary-value">{{ pendingTransfers.length }}</span>
              <small class="summary-sub" *ngIf="pendingTransfersInherited.length > 0">Heredadas: {{ pendingTransfersInherited.length }}</small>
            </div>
          </article>

          <article class="summary-card">
            <div class="summary-icon session">
              <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="2" y="5" width="20" height="14" rx="2"></rect>
                <line x1="2" y1="10" x2="22" y2="10"></line>
              </svg>
            </div>
            <div class="summary-content">
              <span class="summary-label">Sesion activa</span>
              <span class="summary-value">{{ cashSession ? ('#' + cashSession.id + ' - ' + shiftLabel(cashSession.shift)) : 'Sin apertura' }}</span>
            </div>
          </article>
        </div>
      </section>

      <section class="content-section">
        <div class="alert error" *ngIf="errorMessage">
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="15" y1="9" x2="9" y2="15"></line>
            <line x1="9" y1="9" x2="15" y2="15"></line>
          </svg>
          {{ errorMessage }}
        </div>

        <div class="alert warning" *ngIf="cashQueueNotice">
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="12" y1="8" x2="12" y2="12"></line>
            <line x1="12" y1="16" x2="12.01" y2="16"></line>
          </svg>
          {{ cashQueueNotice }}
        </div>

        <div class="alert warning" *ngIf="pendingTransfersInherited.length > 0">
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="12" y1="8" x2="12" y2="12"></line>
            <line x1="12" y1="16" x2="12.01" y2="16"></line>
          </svg>
          Hay {{ pendingTransfersInherited.length }} transferencia{{ pendingTransfersInherited.length === 1 ? '' : 's' }} heredada{{ pendingTransfersInherited.length === 1 ? '' : 's' }} de turnos anteriores.
          <a routerLink="/pos/caja/pendientes" class="alert-link">Ver pendientes</a>
        </div>

        <div class="card">
          <div class="card-header">
            <h2>
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="9" cy="21" r="1"></circle>
                <circle cx="20" cy="21" r="1"></circle>
                <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path>
              </svg>
              Carritos en bandeja
            </h2>
          </div>
          
          <div class="list-container" *ngIf="inbox.length > 0">
            <div class="list-header">
              <span>Carrito</span>
              <span>Dispositivo</span>
              <span>Total</span>
              <span>Acciones</span>
            </div>
            <div *ngFor="let c of inbox" class="list-row">
              <div class="cart-id">
                <strong>#{{ c.id }}</strong>
              </div>
              <div class="cart-device">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect>
                  <line x1="8" y1="21" x2="16" y2="21"></line>
                  <line x1="12" y1="17" x2="12" y2="21"></line>
                </svg>
                {{ c.deviceName }}
              </div>
              <div class="cart-total">
                <strong>{{ c.total | number:'1.2-2' }}</strong>
              </div>
              <div class="row-actions">
                <a class="btn-action sale" [routerLink]="['/pos/caja/venta', c.id]">
                  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="12" y1="1" x2="12" y2="23"></line>
                    <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"></path>
                  </svg>
                  Venta
                </a>
                <a class="btn-action" [routerLink]="['/pos/caja/cobro', c.id]">
                  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="1" y="4" width="22" height="16" rx="2" ry="2"></rect>
                    <line x1="1" y1="10" x2="23" y2="10"></line>
                  </svg>
                  Cobro
                </a>
        </div>
      </div>
    </div>

      <div class="card shift-sales-card" *ngIf="cashSession">
        <div class="card-header shift-sales-head">
          <div class="shift-sales-title">
            <h2>
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M3 3h18v18H3z"></path>
                <path d="M8 12h8"></path>
                <path d="M8 8h8"></path>
                <path d="M8 16h5"></path>
              </svg>
              Ventas del turno
            </h2>
            <p class="shift-sales-subtitle">Movimientos registrados en la sesion actual.</p>
          </div>
          <span class="shift-sales-meta" *ngIf="shiftSalesTotalCount > 0">
            {{ shiftSalesTotalCount }} venta{{ shiftSalesTotalCount === 1 ? '' : 's' }} · {{ shiftSalesTotalAmount | number:'1.2-2' }}
          </span>
        </div>

        <div class="shift-sales-filters" *ngIf="shiftSalesTotalCount > 0">
          <label class="shift-filter">
            <span>Estado</span>
            <select class="shift-filter-select" [(ngModel)]="shiftSalesStatusFilter" (ngModelChange)="onShiftSalesFiltersChange()">
              <option value="all">Todos los estados</option>
              <option value="Paid">Pagada</option>
              <option value="Pending">Pendiente</option>
              <option value="PartiallyPaid">Pago parcial</option>
              <option value="Cancelled">Cancelada</option>
            </select>
          </label>
          <label class="shift-filter">
            <span>Medio</span>
            <select class="shift-filter-select" [(ngModel)]="shiftSalesMethodFilter" (ngModelChange)="onShiftSalesFiltersChange()">
              <option value="all">Todos los medios</option>
              <option value="Efectivo">Efectivo</option>
              <option value="Tarjeta">Tarjeta</option>
              <option value="Transferencia">Transferencia</option>
              <option value="Cuenta corriente">Cuenta corriente</option>
              <option value="QR">QR</option>
            </select>
          </label>
        </div>

        <div class="list-container shift-sales-list" *ngIf="filteredShiftSales.length > 0">
          <div class="list-header shift-sales-header">
            <button type="button" class="shift-sort" (click)="setShiftSalesSort('createdAt')">
              Venta
              <span class="shift-sort-indicator" *ngIf="shiftSalesSortBy === 'createdAt'">{{ shiftSalesSortDirection === 'asc' ? '↑' : '↓' }}</span>
            </button>
            <button type="button" class="shift-sort" (click)="setShiftSalesSort('customerName')">
              Cliente
              <span class="shift-sort-indicator" *ngIf="shiftSalesSortBy === 'customerName'">{{ shiftSalesSortDirection === 'asc' ? '↑' : '↓' }}</span>
            </button>
            <button type="button" class="shift-sort" (click)="setShiftSalesSort('paymentMethodsLabel')">
              Medio
              <span class="shift-sort-indicator" *ngIf="shiftSalesSortBy === 'paymentMethodsLabel'">{{ shiftSalesSortDirection === 'asc' ? '↑' : '↓' }}</span>
            </button>
            <button type="button" class="shift-sort total" (click)="setShiftSalesSort('total')">
              Total
              <span class="shift-sort-indicator" *ngIf="shiftSalesSortBy === 'total'">{{ shiftSalesSortDirection === 'asc' ? '↑' : '↓' }}</span>
            </button>
            <span>Acciones</span>
          </div>
          <div *ngFor="let sale of sortedFilteredShiftSales" class="list-row shift-sales-row" [class.shift-sort-anim]="shiftSalesSortAnimating">
            <div class="shift-sales-id">
              <span class="shift-mobile-label">Venta</span>
              <strong>#{{ sale.saleId }}</strong>
              <small>{{ sale.createdAt | date:'shortTime' }}</small>
              <span class="status-badge" [ngClass]="'status-' + saleStatusTone(sale.status)">
                <svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="status-icon">
                  <path *ngIf="saleStatusTone(sale.status) === 'paid'" d="M20 6 9 17l-5-5"></path>
                  <path *ngIf="saleStatusTone(sale.status) === 'pending'" d="M12 6v6l4 2"></path>
                  <path *ngIf="saleStatusTone(sale.status) === 'partial'" d="M12 20V4"></path>
                  <path *ngIf="saleStatusTone(sale.status) === 'cancelled'" d="m18 6-12 12"></path>
                  <path *ngIf="saleStatusTone(sale.status) === 'cancelled'" d="m6 6 12 12"></path>
                  <circle *ngIf="saleStatusTone(sale.status) === 'neutral'" cx="12" cy="12" r="2"></circle>
                </svg>
                {{ saleStatusLabel(sale.status) }}
              </span>
            </div>
            <div class="shift-sales-customer">
              <span class="shift-mobile-label">Cliente</span>
              <span>{{ sale.customerName || 'Consumidor final' }}</span>
            </div>
            <div class="shift-sales-method">
              <span class="shift-mobile-label">Medio</span>
              <span class="method-chip" [ngClass]="'method-' + paymentMethodTone(sale.paymentMethodsLabel)">
                <svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="method-icon">
                  <path *ngIf="paymentMethodTone(sale.paymentMethodsLabel) === 'cash'" d="M12 1v22"></path>
                  <path *ngIf="paymentMethodTone(sale.paymentMethodsLabel) === 'cash'" d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"></path>
                  <rect *ngIf="paymentMethodTone(sale.paymentMethodsLabel) === 'card'" x="2" y="5" width="20" height="14" rx="2"></rect>
                  <path *ngIf="paymentMethodTone(sale.paymentMethodsLabel) === 'card'" d="M2 10h20"></path>
                  <path *ngIf="paymentMethodTone(sale.paymentMethodsLabel) === 'transfer'" d="M17 1l4 4-4 4"></path>
                  <path *ngIf="paymentMethodTone(sale.paymentMethodsLabel) === 'transfer'" d="M3 11V9a4 4 0 0 1 4-4h14"></path>
                  <path *ngIf="paymentMethodTone(sale.paymentMethodsLabel) === 'credit'" d="M12 2v20"></path>
                  <path *ngIf="paymentMethodTone(sale.paymentMethodsLabel) === 'credit'" d="M7 7h9"></path>
                  <path *ngIf="paymentMethodTone(sale.paymentMethodsLabel) === 'qr'" d="M4 4h6v6H4z"></path>
                  <path *ngIf="paymentMethodTone(sale.paymentMethodsLabel) === 'qr'" d="M14 4h6v6h-6z"></path>
                  <circle *ngIf="paymentMethodTone(sale.paymentMethodsLabel) === 'mixed'" cx="12" cy="12" r="2"></circle>
                </svg>
                {{ paymentMethodsLabel(sale.paymentMethodsLabel) || '-' }}
              </span>
            </div>
            <div class="shift-sales-total">
              <span class="shift-mobile-label">Total</span>
              <strong>{{ sale.total | number:'1.2-2' }}</strong>
            </div>
            <div class="row-actions shift-sales-actions">
              <a [href]="'/print/sale/' + sale.saleId" target="_blank" class="btn-action ticket">Ticket</a>
              <a [href]="'/print/sale/' + sale.saleId + '?autoprint=1&reprint=1'" target="_blank" class="btn-action reprint">Reimprimir</a>
            </div>
          </div>
        </div>

      <div class="shift-sales-footer" *ngIf="shiftSalesTotalCount > 0">
        <span class="shift-sales-progress">Mostrando {{ shiftSales.length }} de {{ shiftSalesTotalCount }}<span *ngIf="isShiftSalesFilterActive"> · filtradas: {{ filteredShiftSales.length }}</span></span>
        <button class="btn-load-more" *ngIf="shiftSalesHasMore" [disabled]="isBusy || shiftSalesLoading" (click)="loadMoreShiftSales()">
          {{ shiftSalesLoading ? 'Cargando...' : 'Cargar mas' }}
        </button>
      </div>

      <p class="shift-sales-error" *ngIf="shiftSalesError">{{ shiftSalesError }}</p>

      <div class="empty-state small" *ngIf="!isBusy && shiftSalesTotalCount === 0">
        <svg xmlns="http://www.w3.org/2000/svg" width="42" height="42" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
          <path d="M3 3h18v18H3z"></path>
          <path d="M8 12h8"></path>
          <path d="M8 8h8"></path>
          <path d="M8 16h5"></path>
        </svg>
        <p>No hay ventas registradas en este turno.</p>
      </div>

      <div class="empty-state small" *ngIf="!isBusy && shiftSalesTotalCount > 0 && filteredShiftSales.length === 0">
        <svg xmlns="http://www.w3.org/2000/svg" width="42" height="42" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
          <circle cx="11" cy="11" r="7"></circle>
          <path d="m20 20-3.5-3.5"></path>
        </svg>
        <p>No hay ventas que coincidan con los filtros.</p>
      </div>
      </div>

    <div class="modal-overlay" *ngIf="confirmDialog">
      <div class="modal confirm-modal">
        <div class="modal-icon warning">
          <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="12" y1="8" x2="12" y2="12"></line>
            <line x1="12" y1="16" x2="12.01" y2="16"></line>
          </svg>
        </div>
        <h3>Confirmacion</h3>
        <p>{{ confirmDialog.message }}</p>
        <div class="modal-actions">
          <button class="btn-secondary" (click)="confirmDialog = null">Cancelar</button>
          <button class="btn-primary" (click)="runConfirmAction()">Confirmar</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .inbox-container {
      min-height: 100vh;
      padding: 0 0 2rem 0;
      position: relative;
      overflow: hidden;
    }

    .hero-bg {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      height: 280px;
      background: linear-gradient(135deg, #1B4D3E 0%, #234F45 100%);
      overflow: hidden;
      z-index: 0;
    }

    .hero-shape {
      position: absolute;
      border-radius: 50%;
      filter: blur(80px);
      opacity: 0.4;
    }

    .shape-1 {
      width: 350px;
      height: 350px;
      background: #BFEBF1;
      top: -120px;
      right: -80px;
      opacity: 0.2;
    }

    .shape-2 {
      width: 250px;
      height: 250px;
      background: #a8d8e0;
      bottom: -80px;
      left: -60px;
      opacity: 0.25;
    }

    .hero {
      position: relative;
      z-index: 1;
      padding: 2rem 1.5rem 1.5rem;
      max-width: 1000px;
      margin: 0 auto;
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 1rem;
      flex-wrap: wrap;
    }

    .hero-content {
      animation: fadeInUp 0.5s ease-out;
    }

    @keyframes fadeInUp {
      from { opacity: 0; transform: translateY(20px); }
      to { opacity: 1; transform: translateY(0); }
    }

    .hero h1 {
      font-size: 2rem;
      font-weight: 700;
      color: #FFFFFF;
      margin: 0 0 0.4rem 0;
    }

    .hero-subtitle {
      font-size: 1rem;
      color: rgba(255, 255, 255, 0.7);
      margin: 0;
    }

    .hero-actions {
      display: flex;
      gap: 0.75rem;
      flex-wrap: wrap;
      animation: fadeInUp 0.5s ease-out 0.1s backwards;
    }

    .btn-primary {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.7rem 1rem;
      background: #BFEBF1;
      color: #1B4D3E;
      border: none;
      border-radius: 10px;
      font-size: 0.9rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .btn-primary:hover:not(:disabled) {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(191, 235, 241, 0.3);
    }

    .btn-primary:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .btn-secondary {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.7rem 1rem;
      background: rgba(255, 255, 255, 0.15);
      color: #FFFFFF;
      border: 1px solid rgba(255, 255, 255, 0.3);
      border-radius: 10px;
      font-size: 0.9rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .btn-secondary:hover:not(:disabled) {
      background: rgba(255, 255, 255, 0.25);
    }

    .btn-ghost {
      display: flex;
      align-items: center;
      gap: 0.4rem;
      padding: 0.7rem 1rem;
      background: transparent;
      color: #FFFFFF;
      border: 1px dashed rgba(255, 255, 255, 0.4);
      border-radius: 10px;
      font-size: 0.9rem;
      font-weight: 500;
      text-decoration: none;
      transition: all 0.2s ease;
    }

    .btn-ghost:hover {
      background: rgba(255, 255, 255, 0.1);
      border-style: solid;
    }

    .summary-section {
      position: relative;
      z-index: 1;
      max-width: 1000px;
      margin: 0 auto;
      padding: 0 1.5rem;
      animation: fadeInUp 0.5s ease-out 0.2s backwards;
    }

    .summary-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 1rem;
    }

    .summary-card {
      background: #FFFFFF;
      border-radius: 14px;
      padding: 1.25rem;
      display: flex;
      align-items: center;
      gap: 1rem;
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
      transition: transform 0.2s ease;
    }

    .summary-card:hover {
      transform: translateY(-2px);
    }

    .summary-icon {
      width: 52px;
      height: 52px;
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .summary-icon.pending {
      background: linear-gradient(135deg, #fff3cd 0%, #ffe69c 100%);
      color: #856404;
    }

    .summary-icon.transfer {
      background: linear-gradient(135deg, #cce5ff 0%, #b8daff 100%);
      color: #004085;
    }

    .summary-icon.session {
      background: linear-gradient(135deg, #d4edda 0%, #c3e6cb 100%);
      color: #155724;
    }

    .summary-content {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .summary-label {
      font-size: 0.8rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: #6c757d;
      font-weight: 500;
    }

    .summary-value {
      font-size: 1rem;
      font-weight: 600;
      color: #1B4D3E;
    }

    .summary-sub {
      color: #7b8b85;
      font-size: 0.76rem;
      font-weight: 600;
    }

    .content-section {
      position: relative;
      z-index: 1;
      max-width: 1000px;
      margin: 1.5rem auto 0;
      padding: 0 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 1rem;
      animation: fadeInUp 0.5s ease-out 0.3s backwards;
    }

    .alert {
      display: flex;
      align-items: center;
      gap: 0.6rem;
      padding: 0.75rem 1rem;
      border-radius: 10px;
      font-size: 0.9rem;
      font-weight: 500;
    }

    .alert.error {
      background: #f8d7da;
      color: #721c24;
    }

    .alert.warning {
      background: #fff3cd;
      color: #856404;
    }

    .alert-link {
      margin-left: auto;
      color: #6b4a00;
      font-weight: 700;
      text-decoration: underline;
    }

    .card {
      background: #FFFFFF;
      border-radius: 16px;
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
      overflow: hidden;
    }

    .card-header {
      padding: 1.25rem 1.5rem;
      border-bottom: 1px solid #e9ecef;
    }

    .card-header h2 {
      display: flex;
      align-items: center;
      gap: 0.6rem;
      font-size: 1.1rem;
      font-weight: 600;
      color: #1B4D3E;
      margin: 0;
    }

    .card-header h2 svg {
      color: #BFEBF1;
      background: #1B4D3E;
      padding: 4px;
      border-radius: 6px;
    }

    .list-container {
      padding: 0.5rem 0;
    }

    .list-header {
      display: grid;
      grid-template-columns: 100px 1fr 140px 220px;
      gap: 1rem;
      padding: 0.75rem 1.5rem;
      font-weight: 600;
      font-size: 0.8rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: #6c757d;
      border-bottom: 1px solid #e9ecef;
    }

    .list-row {
      display: grid;
      grid-template-columns: 100px 1fr 140px 220px;
      gap: 1rem;
      padding: 1rem 1.5rem;
      align-items: center;
      border-bottom: 1px solid #f1f3f5;
      transition: background 0.2s ease;
    }

    .list-row:hover {
      background: #f8fafc;
    }

    .cart-id strong {
      font-size: 1.1rem;
      color: #1B4D3E;
    }

    .cart-device {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      color: #495057;
    }

    .cart-total strong {
      font-size: 1.1rem;
      color: #28a745;
    }

    .row-actions {
      display: flex;
      gap: 0.5rem;
    }

    .btn-action {
      display: flex;
      align-items: center;
      gap: 0.35rem;
      padding: 0.5rem 0.85rem;
      border-radius: 8px;
      font-size: 0.85rem;
      font-weight: 500;
      text-decoration: none;
      transition: all 0.2s ease;
    }

    .btn-action.sale {
      background: #1B4D3E;
      color: #FFFFFF;
    }

    .btn-action.sale:hover {
      background: #234F45;
      transform: translateY(-1px);
    }

    .btn-action {
      background: #e9ecef;
      color: #495057;
    }

    .btn-action:hover {
      background: #dee2e6;
    }

    .shift-sales-card {
      border: 1px solid #e4ecea;
    }

    .shift-sales-head {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
    }

    .shift-sales-subtitle {
      margin: 0;
      color: #6c757d;
    }

    .shift-sales-meta {
      font-weight: 700;
      color: #1B4D3E;
      background: #e9f7f4;
      border: 1px solid #d4ebe6;
      border-radius: 999px;
      padding: 0.3rem 0.7rem;
      white-space: nowrap;
      font-variant-numeric: tabular-nums;
    }

    .shift-sales-filters {
      display: flex;
      flex-wrap: wrap;
      gap: 0.7rem;
      padding: 0.85rem 1.5rem 1rem;
      border-bottom: 1px solid #edf2f1;
    }

    .shift-filter {
      display: flex;
      flex-direction: column;
      gap: 0.2rem;
      color: #66757b;
      font-size: 0.78rem;
      font-weight: 700;
      text-transform: uppercase;
    }

    .shift-filter-select {
      min-width: 200px;
      border: 1px solid #cddfda;
      border-radius: 8px;
      background: #FFFFFF;
      color: #1B4D3E;
      padding: 0.45rem 0.6rem;
    }

    .shift-filter-select:focus {
      outline: none;
      border-color: #8fc8ba;
      box-shadow: 0 0 0 3px rgba(191, 235, 241, 0.35);
    }

    .shift-sales-list {
      max-height: 460px;
      overflow: auto;
    }

    .shift-sales-header,
    .shift-sales-row {
      grid-template-columns: 170px 1.15fr 1fr 120px 170px;
    }

    .shift-sales-list .shift-sales-header {
      position: sticky;
      top: 0;
      z-index: 2;
      background: #FFFFFF;
    }

    .shift-sales-row.shift-sort-anim {
      animation: shiftSortFade 0.2s ease-out;
    }

    @keyframes shiftSortFade {
      from { opacity: 0.72; transform: translateY(2px); }
      to { opacity: 1; transform: translateY(0); }
    }

    .shift-sort {
      border: none;
      background: transparent;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      font: inherit;
      font-size: 0.8rem;
      font-weight: 600;
      color: #6c757d;
      text-align: left;
      padding: 0;
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
      cursor: pointer;
    }

    .shift-sort.total {
      justify-self: end;
    }

    .shift-sort-indicator {
      color: #1B4D3E;
      font-weight: 700;
    }

    .shift-mobile-label {
      display: none;
      color: #73848a;
      font-size: 0.72rem;
      text-transform: uppercase;
      font-weight: 600;
    }

    .shift-sales-id {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .shift-sales-id strong {
      color: #1B4D3E;
      font-size: 1.05rem;
    }

    .shift-sales-id small {
      color: #6c757d;
      font-size: 0.78rem;
      font-variant-numeric: tabular-nums;
    }

    .status-badge {
      width: fit-content;
      border-radius: 999px;
      padding: 0.1rem 0.45rem;
      font-size: 0.73rem;
      font-weight: 600;
      display: inline-flex;
      align-items: center;
      gap: 0.2rem;
    }

    .status-icon,
    .method-icon {
      flex-shrink: 0;
    }

    .status-paid { background: #e8f7ee; color: #1f7a45; }
    .status-pending { background: #fff7e4; color: #8a5a00; }
    .status-partial { background: #e8f2ff; color: #23508f; }
    .status-cancelled { background: #ffeceb; color: #8f3b2f; }
    .status-neutral { background: #f1f3f5; color: #5b6770; }

    .shift-sales-customer,
    .shift-sales-method {
      color: #34424b;
      font-size: 0.92rem;
      display: flex;
      align-items: center;
    }

    .method-chip {
      border-radius: 999px;
      background: #f7fafb;
      color: #42515a;
      padding: 0.18rem 0.52rem;
      font-size: 0.79rem;
      font-weight: 600;
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
    }

    .method-cash { background: #ecf8ee; color: #1f7a45; }
    .method-card { background: #edf3ff; color: #275296; }
    .method-transfer { background: #eef7ff; color: #1f5d8a; }
    .method-credit { background: #f6efff; color: #62408f; }
    .method-qr { background: #f0fbf8; color: #1f7c70; }
    .method-mixed { background: #f4f6f8; color: #4f5f68; }

    .shift-sales-total {
      text-align: right;
    }

    .shift-sales-total strong {
      color: #1f8f58;
      font-size: 1.1rem;
      font-variant-numeric: tabular-nums;
    }

    .shift-sales-actions {
      justify-content: flex-end;
      gap: 0.4rem;
    }

    .shift-sales-actions .btn-action {
      padding: 0.4rem 0.68rem;
      font-size: 0.82rem;
    }

    .shift-sales-actions .btn-action.ticket {
      background: #e8f2ef;
      color: #1B4D3E;
    }

    .shift-sales-actions .btn-action.reprint {
      background: #f3f5f7;
      color: #4f5f68;
    }

    .shift-sales-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 1rem;
      padding: 0.85rem 1.5rem 1.2rem;
      border-top: 1px solid #edf2f1;
    }

    .shift-sales-progress {
      color: #64757d;
    }

    .btn-load-more {
      padding: 0.5rem 0.82rem;
      border: 1px solid #d1e6e0;
      border-radius: 8px;
      background: #ecf8f5;
      color: #1B4D3E;
      font-size: 0.83rem;
      font-weight: 700;
      cursor: pointer;
    }

    .btn-load-more:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .shift-sales-error {
      margin: 0 1.5rem 1rem;
      color: #8f3b2f;
      background: #fff1ee;
      border: 1px solid #f5c3b8;
      border-radius: 8px;
      padding: 0.65rem 0.8rem;
      font-size: 0.84rem;
    }

    .empty-state {
      padding: 3rem 1.5rem;
      text-align: center;
      color: #9EABB1;
    }

    .empty-state svg {
      margin-bottom: 1rem;
      opacity: 0.5;
    }

    .empty-state.small {
      padding: 2rem 1.5rem;
    }

    .empty-state.small svg {
      margin-bottom: 0.5rem;
    }

    .empty-state p {
      margin: 0;
      font-size: 0.95rem;
    }

    .transfer-list {
      padding: 0.5rem 0;
    }

    .transfer-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1rem 1.5rem;
      border-bottom: 1px solid #f1f3f5;
      gap: 1rem;
      flex-wrap: wrap;
    }

    .transfer-info {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .transfer-info strong {
      color: #1B4D3E;
      font-size: 1rem;
    }

    .transfer-total {
      color: #28a745;
      font-weight: 600;
    }

    .btn-confirm {
      display: flex;
      align-items: center;
      gap: 0.35rem;
      padding: 0.5rem 0.85rem;
      background: #28a745;
      color: #FFFFFF;
      border: none;
      border-radius: 8px;
      font-size: 0.85rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .btn-confirm:hover:not(:disabled) {
      background: #218838;
    }

    .btn-confirm:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .btn-cancel {
      display: flex;
      align-items: center;
      gap: 0.35rem;
      padding: 0.5rem 0.85rem;
      background: #f8d7da;
      color: #721c24;
      border: none;
      border-radius: 8px;
      font-size: 0.85rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .btn-cancel:hover:not(:disabled) {
      background: #f5c6cb;
    }

    .btn-cancel:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .modal-overlay {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 100;
      padding: 1rem;
    }

    .modal {
      background: #FFFFFF;
      border-radius: 16px;
      padding: 1.5rem;
      width: 100%;
      max-width: 420px;
      box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
    }

    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.5rem;
    }

    .modal-header h3 {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 1.1rem;
      font-weight: 600;
      color: #1B4D3E;
      margin: 0;
    }

    .modal-header h3 svg {
      color: #BFEBF1;
      background: #1B4D3E;
      padding: 4px;
      border-radius: 6px;
    }

    .modal-close {
      background: none;
      border: none;
      color: #6c757d;
      cursor: pointer;
      padding: 0.25rem;
      display: flex;
      transition: color 0.2s ease;
    }

    .modal-close:hover {
      color: #1B4D3E;
    }

    .modal-subtitle {
      color: #6c757d;
      margin: 0 0 1.25rem 0;
      font-size: 0.95rem;
    }

    .form-group {
      margin-bottom: 1rem;
    }

    .form-group label {
      display: block;
      font-size: 0.85rem;
      font-weight: 500;
      color: #495057;
      margin-bottom: 0.4rem;
    }

    .form-group input {
      width: 100%;
      padding: 0.75rem 1rem;
      font-size: 1rem;
      border: 2px solid #e9ecef;
      border-radius: 10px;
      background: #f8fafc;
      color: #1B4D3E;
      transition: all 0.2s ease;
    }

    .form-group input:focus {
      outline: none;
      border-color: #BFEBF1;
      background: #FFFFFF;
      box-shadow: 0 0 0 4px rgba(191, 235, 241, 0.2);
    }

    .modal-actions {
      display: flex;
      gap: 0.75rem;
      margin-top: 1.5rem;
    }

    .modal-actions .btn-secondary {
      flex: 1;
      color: #495057;
      background: #e9ecef;
      border: none;
      justify-content: center;
    }

    .modal-actions .btn-primary {
      flex: 1;
      justify-content: center;
    }

    @media (max-width: 900px) {
      .hero {
        flex-direction: column;
        align-items: stretch;
      }

      .hero-actions {
        justify-content: flex-start;
      }

      .list-header {
        display: none;
      }

      .list-row {
        grid-template-columns: 1fr;
        gap: 0.75rem;
      }

      .row-actions {
        justify-content: flex-start;
      }

      .shift-sales-header {
        display: none;
      }

      .shift-sales-head {
        align-items: flex-start;
        flex-direction: column;
      }

      .shift-sales-meta {
        white-space: normal;
      }

      .shift-sales-filters {
        flex-direction: column;
      }

      .shift-sales-list {
        max-height: none;
      }

      .shift-sales-row {
        grid-template-columns: 1fr;
        gap: 0.55rem;
      }

      .shift-mobile-label {
        display: inline-block;
      }

      .shift-sales-actions {
        justify-content: flex-start;
      }

      .shift-sales-total {
        text-align: left;
      }

      .shift-filter-select {
        min-width: 0;
      }

      .shift-sales-footer {
        flex-direction: column;
        align-items: stretch;
        padding-top: 0.4rem;
      }

      .btn-load-more {
        width: 100%;
      }
    }

    @media (max-width: 640px) {
      .hero {
        padding: 1.5rem 1rem 1rem;
      }

      .hero h1 {
        font-size: 1.5rem;
      }

      .summary-section,
      .content-section {
        padding: 0 1rem;
      }

      .summary-grid {
        grid-template-columns: 1fr;
      }

      .hero-actions {
        flex-direction: column;
      }

      .btn-primary,
      .btn-secondary,
      .btn-ghost {
        justify-content: center;
      }

      .shift-sales-title h2 {
        font-size: 1rem;
      }
    }

    .confirm-modal {
      text-align: center;
    }

    .confirm-modal .modal-icon {
      margin-bottom: 1rem;
    }

    .confirm-modal .modal-icon.warning {
      color: #F59E0B;
    }

    .confirm-modal h3 {
      font-size: 1.25rem;
      color: #1B4D3E;
      margin: 0 0 0.5rem 0;
    }

    .confirm-modal p {
      color: #495057;
      margin: 0;
    }
  `]
})
export class PosCajaInboxComponent {
  private static readonly SHIFT_SALES_PREFS_KEY = 'pos-caja-inbox.shift-sales-prefs';
  inbox: CartInboxItem[] = [];
  cashSession: CashSessionResponse | null = null;
  pendingTransfers: PendingTransferSale[] = [];
  pendingTransfersInherited: PendingTransferSale[] = [];
  shiftSales: CashSessionSaleSummary[] = [];
  shiftSalesTotalCount = 0;
  shiftSalesTotalAmount = 0;
  readonly shiftSalesLimit = 20;
  shiftSalesHasMore = false;
  shiftSalesLoading = false;
  shiftSalesError = '';
  shiftSalesSortBy: 'createdAt' | 'customerName' | 'paymentMethodsLabel' | 'total' = 'createdAt';
  shiftSalesSortDirection: 'asc' | 'desc' = 'desc';
  shiftSalesStatusFilter: 'all' | 'Paid' | 'Pending' | 'PartiallyPaid' | 'Cancelled' = 'all';
  shiftSalesMethodFilter = 'all';
  shiftSalesSortAnimating = false;
  private shiftSortAnimTimer: any = null;
  pendingTransferModal: { saleId: number; paymentId: number } | null = null;
  modalReference = '';
  modalNotes = '';
  errorMessage = '';
  cashQueueNotice = '';
  confirmDialog: { message: string; onConfirm: () => Promise<void> } | null = null;
  private pendingRequests = 0;

  get isBusy(): boolean {
    return this.pendingRequests > 0;
  }

  get isShiftSalesFilterActive(): boolean {
    return this.shiftSalesStatusFilter !== 'all' || this.shiftSalesMethodFilter !== 'all';
  }

  get filteredShiftSales(): CashSessionSaleSummary[] {
    return this.shiftSales.filter((sale) => {
      const statusOk = this.shiftSalesStatusFilter === 'all'
        || this.saleStatusTone(sale.status) === this.saleStatusTone(this.shiftSalesStatusFilter);
      const methodOk = this.shiftSalesMethodFilter === 'all'
        || this.normalizeText(this.paymentMethodsLabel(sale.paymentMethodsLabel)).includes(this.normalizeText(this.shiftSalesMethodFilter));
      return statusOk && methodOk;
    });
  }

  get sortedFilteredShiftSales(): CashSessionSaleSummary[] {
    const rows = [...this.filteredShiftSales];
    const direction = this.shiftSalesSortDirection === 'asc' ? 1 : -1;
    rows.sort((a, b) => {
      if (this.shiftSalesSortBy === 'createdAt') {
        return (new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()) * direction;
      }
      if (this.shiftSalesSortBy === 'total') {
        return (a.total - b.total) * direction;
      }
      if (this.shiftSalesSortBy === 'customerName') {
        return this.normalizeText(a.customerName).localeCompare(this.normalizeText(b.customerName)) * direction;
      }
      return this.normalizeText(this.paymentMethodsLabel(a.paymentMethodsLabel))
        .localeCompare(this.normalizeText(this.paymentMethodsLabel(b.paymentMethodsLabel))) * direction;
    });
    return rows;
  }

  constructor(
    private readonly api: PosCajaService,
    private readonly router: Router,
    private readonly operatorSessionService: OperatorSessionService,
    private readonly activityService: ActivityService,
    private readonly dialog: DialogService,
    public readonly health: HealthService
  ) {
    this.loadShiftSalesPrefs();
    void this.refreshAll();
  }

  async refreshAll(): Promise<void> {
    try {
      this.errorMessage = '';
      this.cashQueueNotice = '';
      await this.withBusy(async () => {
        try {
          this.cashSession = await this.api.getCurrentCashSession();
        } catch (err: any) {
          const message = `${err?.error?.message ?? err?.message ?? ''}`.toLowerCase();
          if (message.includes('no open cash session') || message.includes('no hay una caja abierta')) {
            this.cashSession = null;
          } else {
            throw err;
          }
        }

        this.inbox = await this.api.getInbox();

        if (this.cashSession) {
          const [currentTransfers, deviceTransfers] = await Promise.all([
            this.api.getPendingTransfers(undefined, 'current-session'),
            this.api.getPendingTransfers(undefined, 'device')
          ]);
          this.pendingTransfers = currentTransfers;
          const currentIds = new Set(currentTransfers.map(t => t.saleId));
          this.pendingTransfersInherited = deviceTransfers.filter(t => !currentIds.has(t.saleId));
          await this.loadShiftSales(true);
        } else {
          this.pendingTransfers = [];
          this.pendingTransfersInherited = [];
          this.shiftSales = [];
          this.shiftSalesTotalCount = 0;
          this.shiftSalesTotalAmount = 0;
          this.shiftSalesHasMore = false;
          this.shiftSalesLoading = false;
          this.shiftSalesError = '';
          if (this.inbox.length > 0) {
            this.cashQueueNotice = `Caja cerrada: hay ${this.inbox.length} carrito${this.inbox.length === 1 ? '' : 's'} esperando cobro.`;
          }
        }
      });
    } catch (err: any) {
      this.errorMessage = err?.error?.message ?? 'No se pudo cargar la bandeja de caja';
    }
  }

  async createCartAndOpen(): Promise<void> {
    try {
      const cart = await this.withBusy(() => this.api.createCart());
      void this.router.navigateByUrl(`/pos/caja/venta/${cart.id}`);
    } catch (err: any) {
      this.errorMessage = err?.error?.message ?? 'No se pudo crear carrito';
    }
  }

  openPendingTransferModal(p: PendingTransferSale): void {
    const pending = p.payments.find((x) => x.paymentMethod === 'Transfer' && x.status === 'Pending') ?? p.payments[0];
    if (!pending) return;
    this.pendingTransferModal = { saleId: p.saleId, paymentId: pending.id };
    this.modalReference = '';
    this.modalNotes = '';
  }

  async confirmPendingTransfer(): Promise<void> {
    if (!this.pendingTransferModal) return;

    const hasIdentity = await this.ensureActiveIdentity();
    if (!hasIdentity) {
      this.confirmDialog = {
        message: 'No se pudo verificar la identidad. Desea intentar de nuevo?',
        onConfirm: async () => await this.confirmPendingTransfer()
      };
      return;
    }

    try {
      await this.withBusy(() =>
        this.api.confirmTransfer(
          this.pendingTransferModal!.saleId,
          this.pendingTransferModal!.paymentId,
          this.modalReference || undefined,
          this.modalNotes || undefined
        )
      );
      this.closeModal();
      await this.refreshAll();
    } catch (err: any) {
      this.errorMessage = err?.error?.message ?? 'No se pudo confirmar transferencia';
    }
  }

  async cancelPendingTransfer(p: PendingTransferSale): Promise<void> {
    const hasIdentity = await this.ensureActiveIdentity();
    if (!hasIdentity) {
      this.confirmDialog = {
        message: 'No se pudo verificar la identidad. Desea intentar de nuevo?',
        onConfirm: async () => await this.cancelPendingTransfer(p)
      };
      return;
    }

    const reason = await this.dialog.prompt({
      title: 'Cancelar transferencia',
      message: `Venta #${p.saleId}. Ingresa el motivo de cancelacion para dejar registro.`,
      inputLabel: 'Motivo',
      inputPlaceholder: 'Ej: comprobante invalido o error de transferencia',
      yesLabel: 'Cancelar transferencia',
      noLabel: 'Volver',
      inputRequired: true
    });
    if (!reason) return;

    try {
      await this.withBusy(() => this.api.cancelPendingTransfer(p.saleId, reason));
      await this.refreshAll();
    } catch (err: any) {
      const status = Number(err?.status ?? 0);
      const message = `${err?.error?.message ?? ''}`.toLowerCase();
      if (status === 403 || message.includes('autorizacion') || message.includes('supervisor')) {
        await this.cancelPendingTransferWithSupervisorAuth(p, reason);
        return;
      }
      this.errorMessage = err?.error?.message ?? 'No se pudo cancelar la transferencia pendiente';
    }
  }

  closeModal(): void {
    this.pendingTransferModal = null;
  }

  async loadMoreShiftSales(): Promise<void> {
    if (!this.cashSession || this.shiftSalesLoading || !this.shiftSalesHasMore) return;
    await this.withBusy(async () => {
      await this.loadShiftSales(false);
    });
  }

  shiftLabel(shift: string): string {
    if (shift === 'Morning') return 'Manana';
    if (shift === 'Afternoon') return 'Tarde';
    if (shift === 'Night') return 'Noche';
    return shift;
  }

  saleStatusLabel(status: string): string {
    if (status === 'Paid') return 'Pagada';
    if (status === 'Completed') return 'Pagada';
    if (status === 'Pending') return 'Pendiente';
    if (status === 'PendingTransfer') return 'Transferencia pendiente';
    if (status === 'Cancelled') return 'Cancelada';
    if (status === 'PartiallyPaid') return 'Pago parcial';
    return status;
  }

  saleStatusTone(status: string): 'paid' | 'pending' | 'cancelled' | 'partial' | 'neutral' {
    if (status === 'Paid' || status === 'Completed') return 'paid';
    if (status === 'Pending' || status === 'PendingTransfer') return 'pending';
    if (status === 'Cancelled') return 'cancelled';
    if (status === 'PartiallyPaid') return 'partial';
    return 'neutral';
  }

  setShiftSalesSort(field: 'createdAt' | 'customerName' | 'paymentMethodsLabel' | 'total'): void {
    if (this.shiftSalesSortBy === field) {
      this.shiftSalesSortDirection = this.shiftSalesSortDirection === 'asc' ? 'desc' : 'asc';
      this.persistShiftSalesPrefs();
      this.triggerShiftSortAnimation();
      return;
    }
    this.shiftSalesSortBy = field;
    this.shiftSalesSortDirection = field === 'createdAt' || field === 'total' ? 'desc' : 'asc';
    this.persistShiftSalesPrefs();
    this.triggerShiftSortAnimation();
  }

  onShiftSalesFiltersChange(): void {
    this.persistShiftSalesPrefs();
    this.triggerShiftSortAnimation();
  }

  paymentMethodTone(value: string): 'cash' | 'card' | 'transfer' | 'credit' | 'qr' | 'mixed' {
    const label = this.normalizeText(this.paymentMethodsLabel(value));
    if (label.includes('+')) return 'mixed';
    if (label.includes('efectivo')) return 'cash';
    if (label.includes('tarjeta')) return 'card';
    if (label.includes('transferencia')) return 'transfer';
    if (label.includes('cuenta corriente')) return 'credit';
    if (label.includes('qr')) return 'qr';
    return 'mixed';
  }

  paymentMethodsLabel(value: string): string {
    const normalized = value
      .split('+')
      .map((part) => part.trim())
      .filter(Boolean)
      .map((part) => {
        if (part === 'Cash') return 'Efectivo';
        if (part === 'Card') return 'Tarjeta';
        if (part === 'Transfer') return 'Transferencia';
        if (part === 'AccountCredit' || part === 'Credit') return 'Cuenta corriente';
        if (part === 'QR') return 'QR';
        return part;
      });
    return normalized.join(' + ');
  }

  private async loadShiftSales(reset: boolean): Promise<void> {
    const offset = reset ? 0 : this.shiftSales.length;
    this.shiftSalesLoading = true;
    this.shiftSalesError = '';
    try {
      const sales = await this.api.getCurrentCashSessionSales(this.shiftSalesLimit, offset);
      const items = sales.items ?? [];
      this.shiftSales = reset ? items : [...this.shiftSales, ...items];
      this.shiftSalesTotalCount = sales.totalCount ?? this.shiftSales.length;
      this.shiftSalesTotalAmount = Number(sales.totalAmount ?? 0);
      this.shiftSalesHasMore = this.shiftSales.length < this.shiftSalesTotalCount;
    } catch (err: any) {
      if (reset) {
        this.shiftSales = [];
        this.shiftSalesTotalCount = 0;
        this.shiftSalesTotalAmount = 0;
        this.shiftSalesHasMore = false;
      }
      this.shiftSalesError = err?.error?.message ?? 'No se pudo cargar ventas del turno';
    } finally {
      this.shiftSalesLoading = false;
    }
  }

  private normalizeText(value: string | null | undefined): string {
    return `${value ?? ''}`.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
  }

  private async cancelPendingTransferWithSupervisorAuth(p: PendingTransferSale, reason: string): Promise<void> {
    const approverUsername = await this.dialog.prompt({
      title: 'Autorizacion requerida',
      message: 'Ingresa usuario supervisor o administrador para autorizar la cancelacion.',
      inputLabel: 'Usuario autorizador',
      inputPlaceholder: 'usuario',
      yesLabel: 'Continuar',
      noLabel: 'Cancelar',
      inputRequired: true
    });
    if (!approverUsername) return;

    if (this.normalizeText(approverUsername) === this.normalizeText(this.operatorSessionService.getOperatorName())) {
      this.errorMessage = 'Para cancelar una transferencia se requiere autorizacion de un supervisor o administrador distinto al operador activo.';
      return;
    }

    const approverPassword = await this.dialog.prompt({
      title: 'Autorizacion requerida',
      message: 'Ingresa la contrasena del supervisor o administrador autorizador.',
      inputLabel: 'Contrasena',
      inputType: 'password',
      inputPlaceholder: 'contrasena',
      yesLabel: 'Continuar',
      noLabel: 'Cancelar',
      inputRequired: true
    });
    if (!approverPassword) return;

    const approverPin = await this.dialog.prompt({
      title: 'Autorizacion requerida',
      message: 'Ingresa el PIN del supervisor o administrador autorizador.',
      inputLabel: 'PIN autorizador',
      inputPlaceholder: 'PIN (4 a 6 digitos)',
      inputType: 'password',
      inputMode: 'numeric',
      inputMinLength: 4,
      inputMaxLength: 6,
      inputPattern: '^[0-9]{4,6}$',
      inputDigitsOnly: true,
      inputErrorMessage: 'El PIN debe tener entre 4 y 6 numeros.',
      yesLabel: 'Autorizar cancelacion',
      noLabel: 'Cancelar',
      inputRequired: true
    });
    if (!approverPin) return;

    try {
      await this.withBusy(() => this.api.cancelPendingTransferWithAuthorization(p.saleId, {
        reason,
        approverUsername,
        approverPassword,
        approverPin
      }));
      await this.refreshAll();
    } catch (err: any) {
      this.errorMessage = err?.error?.message ?? 'No se pudo validar la autorizacion para cancelar la transferencia';
    }
  }

  private triggerShiftSortAnimation(): void {
    this.shiftSalesSortAnimating = false;
    if (this.shiftSortAnimTimer) {
      clearTimeout(this.shiftSortAnimTimer);
    }
    this.shiftSortAnimTimer = setTimeout(() => {
      this.shiftSalesSortAnimating = true;
      this.shiftSortAnimTimer = setTimeout(() => {
        this.shiftSalesSortAnimating = false;
        this.shiftSortAnimTimer = null;
      }, 220);
    }, 0);
  }

  private persistShiftSalesPrefs(): void {
    try {
      const value = {
        sortBy: this.shiftSalesSortBy,
        sortDirection: this.shiftSalesSortDirection,
        statusFilter: this.shiftSalesStatusFilter,
        methodFilter: this.shiftSalesMethodFilter
      };
      localStorage.setItem(PosCajaInboxComponent.SHIFT_SALES_PREFS_KEY, JSON.stringify(value));
    } catch {
      // ignore storage errors
    }
  }

  private loadShiftSalesPrefs(): void {
    try {
      const raw = localStorage.getItem(PosCajaInboxComponent.SHIFT_SALES_PREFS_KEY);
      if (!raw) return;
      const parsed = JSON.parse(raw) as {
        sortBy?: 'createdAt' | 'customerName' | 'paymentMethodsLabel' | 'total';
        sortDirection?: 'asc' | 'desc';
        statusFilter?: 'all' | 'Paid' | 'Pending' | 'PartiallyPaid' | 'Cancelled';
        methodFilter?: string;
      };
      if (parsed.sortBy && ['createdAt', 'customerName', 'paymentMethodsLabel', 'total'].includes(parsed.sortBy)) {
        this.shiftSalesSortBy = parsed.sortBy;
      }
      if (parsed.sortDirection === 'asc' || parsed.sortDirection === 'desc') {
        this.shiftSalesSortDirection = parsed.sortDirection;
      }
      if (parsed.statusFilter && ['all', 'Paid', 'Pending', 'PartiallyPaid', 'Cancelled'].includes(parsed.statusFilter)) {
        this.shiftSalesStatusFilter = parsed.statusFilter;
      }
      if (typeof parsed.methodFilter === 'string' && parsed.methodFilter.trim()) {
        this.shiftSalesMethodFilter = parsed.methodFilter;
      }
    } catch {
      // ignore parse errors
    }
  }

  private async ensureActiveIdentity(): Promise<boolean> {
    const confirmed = await this.activityService.ensureRecentIdentity({
      idleSeconds: 60,
      confirmationMessage: 'Pasaron mas de 60s sin interaccion. Sos vos?',
      pinPrompt: () => this.operatorSessionService.requestPin()
    });

    if (!confirmed) {
      this.errorMessage = 'Accion cancelada por inactividad';
    }

    return confirmed;
  }

  async runConfirmAction(): Promise<void> {
    const action = this.confirmDialog?.onConfirm;
    this.confirmDialog = null;
    if (action) {
      await action();
    }
  }

  private async withBusy<T>(fn: () => Promise<T>): Promise<T> {
    this.pendingRequests += 1;
    try {
      return await fn();
    } finally {
      this.pendingRequests -= 1;
    }
  }
}

@Component({
  standalone: true,
  selector: 'app-pos-caja-venta',
  imports: [CommonModule, FormsModule, RouterLink, PosModuleNavComponent],
  template: `
    <main class="venta-container">
      <div class="hero-bg">
        <div class="hero-shape shape-1"></div>
        <div class="hero-shape shape-2"></div>
      </div>

      <app-pos-module-nav />

      <header class="hero">
        <div class="hero-content">
          <h1>Venta de caja</h1>
          <p class="hero-subtitle">Carrito #{{ cartId }} - Total {{ cartTotal | number:'1.2-2' }}</p>
        </div>
        <div class="hero-actions">
          <a class="btn-ghost" routerLink="/pos/caja/inbox">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="22 12 16 12 14 15 10 15 8 12 2 12"></polyline>
              <path d="M2.36 11.86A8 8 0 0 1 13.64 5.64L22 12"></path>
            </svg>
            Bandeja
          </a>
          <a class="btn-primary" [routerLink]="['/pos/caja/cobro', cartId]">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="1" y="4" width="22" height="16" rx="2" ry="2"></rect>
              <line x1="1" y1="10" x2="23" y2="10"></line>
            </svg>
            Ir a cobro
          </a>
        </div>
      </header>

      <section class="content-section">
        <div class="alert error" *ngIf="errorMessage">
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="15" y1="9" x2="9" y2="15"></line>
            <line x1="9" y1="9" x2="15" y2="15"></line>
          </svg>
          {{ errorMessage }}
        </div>

        <div class="card scan-card">
          <div class="scan-input-wrapper">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M3 7V5a2 2 0 0 1 2-2h2"></path>
              <path d="M17 3h2a2 2 0 0 1 2 2v2"></path>
              <path d="M21 17v2a2 2 0 0 1-2 2h-2"></path>
              <path d="M7 21H5a2 2 0 0 1-2-2v-2"></path>
              <line x1="7" y1="12" x2="17" y2="12"></line>
            </svg>
            <input #scanInput type="text" placeholder="Escanear o buscar producto" [(ngModel)]="scanCode" (input)="onScanInputChange()" (keydown.enter)="onScanEnter($event)" (keydown.arrowdown)="moveSuggestion(1, $event)" (keydown.arrowup)="moveSuggestion(-1, $event)" (keydown.escape)="dismissSuggestions()" [disabled]="isBusy" />
            <select class="scan-mode" [(ngModel)]="addMode" [disabled]="isBusy">
              <option value="quantity">Cantidad</option>
              <option value="weight">Peso (kg)</option>
            </select>
            <input class="scan-qty" type="number" [(ngModel)]="manualValue" [disabled]="isBusy" [step]="addMode === 'weight' ? 0.01 : 1" min="0.01" />
            <button class="btn-add" [disabled]="isBusy" (click)="quickAddFromScan()">
              <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="12" y1="5" x2="12" y2="19"></line>
                <line x1="5" y1="12" x2="19" y2="12"></line>
              </svg>
              Agregar
            </button>
            <button class="btn-reload" [disabled]="isBusy" (click)="reloadCart()">
              <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="23 4 23 10 17 10"></polyline>
                <path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"></path>
              </svg>
            </button>
          </div>
          <div class="scan-suggestions" *ngIf="showSuggestions && filteredProducts.length > 0">
            <button type="button" class="suggestion-item" *ngFor="let p of filteredProducts; let i = index" [class.active]="i === suggestionIndex" (click)="selectSuggestedProduct(p)" [disabled]="isBusy">
              <span>{{ p.name }}</span>
              <small>{{ productCodesLabel(p) || ('PID:' + p.id) }}</small>
            </button>
          </div>
        </div>

        <div class="card">
          <div class="card-header">
            <h2>
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
                <line x1="3" y1="6" x2="21" y2="6"></line>
                <path d="M16 10a4 4 0 0 1-8 0"></path>
              </svg>
              Items del carrito
            </h2>
          </div>
          
          <div class="items-list" *ngIf="cartItems.length > 0">
            <div class="list-header">
              <span>Producto</span>
              <span>Subtotal</span>
              <span></span>
            </div>
            <div *ngFor="let item of cartItems" class="list-row">
              <div class="item-info">
                <strong>{{ item.productCode }}</strong>
                <span>{{ item.productName }} x{{ item.quantity }}</span>
              </div>
              <div class="item-subtotal">
                <strong>{{ item.subtotal | number:'1.2-2' }}</strong>
              </div>
              <button class="btn-remove" [disabled]="isBusy" (click)="removeItem(item)">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polyline points="3 6 5 6 21 6"></polyline>
                  <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                </svg>
              </button>
            </div>
          </div>
          
          <div class="empty-state" *ngIf="!isBusy && cartItems.length === 0">
            <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
              <line x1="3" y1="6" x2="21" y2="6"></line>
              <path d="M16 10a4 4 0 0 1-8 0"></path>
            </svg>
            <p>No hay items en este carrito</p>
          </div>
        </div>
      </section>
    </main>

    <div class="modal-overlay" *ngIf="weightModal">
      <div class="modal">
        <div class="modal-header">
          <h3>
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
              <line x1="3" y1="6" x2="21" y2="6"></line>
              <path d="M16 10a4 4 0 0 1-8 0"></path>
            </svg>
            Producto pesable
          </h3>
        </div>
        <p class="modal-subtitle">{{ weightModal!.product.name }}</p>
        
        <div class="form-group">
          <label>Peso (kg)</label>
          <input type="number" [(ngModel)]="weightKg" [disabled]="isBusy" step="0.01" min="0" />
        </div>
        
        <div class="form-group" *ngIf="weightModal!.product.allowsManualPrice">
          <label>Precio por kg ($)</label>
          <input type="number" [(ngModel)]="weightPricePerKg" [disabled]="isBusy" step="0.01" min="0" />
        </div>
        
        <div class="modal-actions">
          <button class="btn-secondary" [disabled]="isBusy" (click)="closeWeightModal()">Cancelar</button>
          <button class="btn-primary" [disabled]="isBusy" (click)="confirmWeightItem()">Agregar</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .venta-container {
      min-height: 100vh;
      padding: 0 0 2rem 0;
      position: relative;
      overflow: hidden;
    }

    .hero-bg {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      height: 220px;
      background: linear-gradient(135deg, #1B4D3E 0%, #234F45 100%);
      overflow: hidden;
      z-index: 0;
    }

    .hero-shape {
      position: absolute;
      border-radius: 50%;
      filter: blur(80px);
      opacity: 0.4;
    }

    .shape-1 {
      width: 280px;
      height: 280px;
      background: #BFEBF1;
      top: -100px;
      right: -60px;
      opacity: 0.2;
    }

    .shape-2 {
      width: 200px;
      height: 200px;
      background: #a8d8e0;
      bottom: -60px;
      left: -40px;
      opacity: 0.25;
    }

    .hero {
      position: relative;
      z-index: 1;
      padding: 1.5rem;
      max-width: 800px;
      margin: 0 auto;
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 1rem;
      flex-wrap: wrap;
    }

    .hero-content { animation: fadeInUp 0.5s ease-out; }
    .hero-actions { animation: fadeInUp 0.5s ease-out 0.1s backwards; }

    @keyframes fadeInUp {
      from { opacity: 0; transform: translateY(20px); }
      to { opacity: 1; transform: translateY(0); }
    }

    .hero h1 {
      font-size: 1.75rem;
      font-weight: 700;
      color: #FFFFFF;
      margin: 0 0 0.3rem 0;
    }

    .hero-subtitle {
      font-size: 0.95rem;
      color: rgba(255, 255, 255, 0.7);
      margin: 0;
    }

    .hero-actions {
      display: flex;
      gap: 0.75rem;
    }

    .btn-primary {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.7rem 1rem;
      background: #BFEBF1;
      color: #1B4D3E;
      border: none;
      border-radius: 10px;
      font-size: 0.9rem;
      font-weight: 600;
      text-decoration: none;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .btn-primary:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(191, 235, 241, 0.3);
    }

    .btn-ghost {
      display: flex;
      align-items: center;
      gap: 0.4rem;
      padding: 0.7rem 1rem;
      background: transparent;
      color: #FFFFFF;
      border: 1px dashed rgba(255, 255, 255, 0.4);
      border-radius: 10px;
      font-size: 0.9rem;
      font-weight: 500;
      text-decoration: none;
      transition: all 0.2s ease;
    }

    .btn-ghost:hover {
      background: rgba(255, 255, 255, 0.1);
      border-style: solid;
    }

    .content-section {
      position: relative;
      z-index: 1;
      max-width: 800px;
      margin: 0 auto;
      padding: 0 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .alert {
      display: flex;
      align-items: center;
      gap: 0.6rem;
      padding: 0.75rem 1rem;
      border-radius: 10px;
      font-size: 0.9rem;
      font-weight: 500;
    }

    .alert.error {
      background: #f8d7da;
      color: #721c24;
    }

    .card {
      background: #FFFFFF;
      border-radius: 16px;
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
      overflow: hidden;
    }

    .scan-card {
      padding: 1rem;
    }

    .scan-input-wrapper {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      background: #f8fafc;
      border: 2px solid #e9ecef;
      border-radius: 12px;
      padding: 0.5rem 0.75rem;
      transition: all 0.2s ease;
    }

    .scan-input-wrapper:focus-within {
      border-color: #BFEBF1;
      box-shadow: 0 0 0 4px rgba(191, 235, 241, 0.2);
    }

    .scan-input-wrapper svg {
      color: #9EABB1;
      flex-shrink: 0;
    }

    .scan-input-wrapper input:not(.scan-qty) {
      flex: 1;
      border: none;
      background: transparent;
      font-size: 1rem;
      color: #1B4D3E;
      padding: 0.5rem 0;
      min-width: 0;
    }

    .scan-input-wrapper input:focus {
      outline: none;
    }

    .scan-input-wrapper input::placeholder {
      color: #9EABB1;
    }

    .scan-mode,
    .scan-qty {
      border: 1px solid #d7dfe4;
      border-radius: 8px;
      background: #fff;
      color: #1B4D3E;
      font-size: 0.9rem;
      padding: 0.45rem 0.55rem;
    }

    .scan-mode {
      min-width: 130px;
      flex: 0 0 130px;
    }

    .scan-qty {
      width: 78px;
      min-width: 78px;
      flex: 0 0 78px;
      text-align: center;
      font-weight: 600;
    }

    .scan-suggestions {
      margin-top: 0.6rem;
      border: 1px solid #dbe3e8;
      border-radius: 10px;
      background: #fff;
      max-height: 220px;
      overflow: auto;
    }

    .suggestion-item {
      width: 100%;
      display: flex;
      justify-content: space-between;
      gap: 0.8rem;
      border: 0;
      background: #fff;
      padding: 0.6rem 0.75rem;
      text-align: left;
      color: #1b4d3e;
      cursor: pointer;
      font: inherit;
    }

    .suggestion-item + .suggestion-item {
      border-top: 1px solid #edf2f5;
    }

    .suggestion-item small {
      color: #6c7f87;
      white-space: nowrap;
    }

    .suggestion-item:hover:not(:disabled) {
      background: #f3faf8;
    }

    .suggestion-item.active {
      background: #e8f7f2;
    }

    .btn-add {
      display: flex;
      align-items: center;
      gap: 0.4rem;
      padding: 0.6rem 1rem;
      background: #1B4D3E;
      color: #FFFFFF;
      border: none;
      border-radius: 8px;
      font-size: 0.9rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s ease;
      white-space: nowrap;
    }

    .btn-add:hover:not(:disabled) {
      background: #234F45;
    }

    .btn-add:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .btn-reload {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      background: #e9ecef;
      color: #495057;
      border: none;
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .btn-reload:hover:not(:disabled) {
      background: #dee2e6;
    }

    .btn-reload:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .card-header {
      padding: 1.25rem 1.5rem;
      border-bottom: 1px solid #e9ecef;
    }

    .card-header h2 {
      display: flex;
      align-items: center;
      gap: 0.6rem;
      font-size: 1.1rem;
      font-weight: 600;
      color: #1B4D3E;
      margin: 0;
    }

    .card-header h2 svg {
      color: #BFEBF1;
      background: #1B4D3E;
      padding: 4px;
      border-radius: 6px;
    }

    .items-list {
      padding: 0.5rem 0;
    }

    .list-header {
      display: grid;
      grid-template-columns: 1fr 140px 60px;
      gap: 1rem;
      padding: 0.75rem 1.5rem;
      font-weight: 600;
      font-size: 0.8rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: #6c757d;
      border-bottom: 1px solid #e9ecef;
    }

    .list-row {
      display: grid;
      grid-template-columns: 1fr 140px 60px;
      gap: 1rem;
      padding: 1rem 1.5rem;
      align-items: center;
      border-bottom: 1px solid #f1f3f5;
    }

    .list-row:hover {
      background: #f8fafc;
    }

    .item-info {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .item-info strong {
      color: #1B4D3E;
      font-size: 0.95rem;
    }

    .item-info span {
      color: #6c757d;
      font-size: 0.85rem;
    }

    .item-subtotal strong {
      font-size: 1rem;
      color: #28a745;
    }

    .btn-remove {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 36px;
      height: 36px;
      background: #f8d7da;
      color: #721c24;
      border: none;
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .btn-remove:hover:not(:disabled) {
      background: #f5c6cb;
    }

    .btn-remove:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .empty-state {
      padding: 3rem 1.5rem;
      text-align: center;
      color: #9EABB1;
    }

    .empty-state svg {
      margin-bottom: 1rem;
      opacity: 0.5;
    }

    .empty-state p {
      margin: 0;
      font-size: 0.95rem;
    }

    .modal-overlay {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 100;
      padding: 1rem;
    }

    .modal {
      background: #FFFFFF;
      border-radius: 16px;
      padding: 1.5rem;
      width: 100%;
      max-width: 400px;
      box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
    }

    .modal-header h3 {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 1.1rem;
      font-weight: 600;
      color: #1B4D3E;
      margin: 0 0 0.5rem 0;
    }

    .modal-header h3 svg {
      color: #BFEBF1;
      background: #1B4D3E;
      padding: 4px;
      border-radius: 6px;
    }

    .modal-subtitle {
      color: #495057;
      margin: 0 0 1.25rem 0;
      font-size: 0.95rem;
    }

    .form-group {
      margin-bottom: 1rem;
    }

    .form-group label {
      display: block;
      font-size: 0.85rem;
      font-weight: 500;
      color: #495057;
      margin-bottom: 0.4rem;
    }

    .form-group input {
      width: 100%;
      padding: 0.75rem 1rem;
      font-size: 1rem;
      border: 2px solid #e9ecef;
      border-radius: 10px;
      background: #f8fafc;
      color: #1B4D3E;
    }

    .form-group input:focus {
      outline: none;
      border-color: #BFEBF1;
      box-shadow: 0 0 0 4px rgba(191, 235, 241, 0.2);
    }

    .modal-actions {
      display: flex;
      gap: 0.75rem;
      margin-top: 1.5rem;
    }

    .btn-secondary {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding: 0.7rem 1rem;
      background: #e9ecef;
      color: #495057;
      border: none;
      border-radius: 10px;
      font-size: 0.9rem;
      font-weight: 500;
      cursor: pointer;
    }

    .btn-primary {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding: 0.7rem 1rem;
      background: #1B4D3E;
      color: #FFFFFF;
      border: none;
      border-radius: 10px;
      font-size: 0.9rem;
      font-weight: 600;
      cursor: pointer;
    }

    @media (max-width: 640px) {
      .hero {
        flex-direction: column;
        align-items: stretch;
      }

      .hero-actions {
        justify-content: stretch;
      }

      .hero-actions a {
        flex: 1;
        justify-content: center;
      }

      .scan-input-wrapper {
        flex-wrap: wrap;
      }

      .scan-input-wrapper input {
        order: 1;
        width: 100%;
        flex: none;
      }

      .scan-mode,
      .scan-qty,
      .btn-add,
      .btn-reload {
        width: 100%;
      }

      .list-header { display: none; }
      .list-row { grid-template-columns: 1fr; gap: 0.5rem; }
    }
  `]
})
export class PosCajaVentaComponent {
  @ViewChild('scanInput') scanInput?: ElementRef<HTMLInputElement>;

  cartId = 0;
  cartItems: CartItem[] = [];
  cartTotal = 0;
  scanCode = '';
  errorMessage = '';
  private pendingRequests = 0;

  weightModal: { product: ProductLookupResponse; scannedCode: string } | null = null;
  weightKg = 1;
  weightPricePerKg = 0;
  addMode: 'quantity' | 'weight' = 'quantity';
  manualValue = 1;
  activeProducts: ProductLookupResponse[] = [];
  filteredProducts: ProductLookupResponse[] = [];
  selectedProduct: ProductLookupResponse | null = null;
  showSuggestions = false;
  suggestionIndex = -1;
  private productsLoaded = false;

  get isBusy(): boolean { return this.pendingRequests > 0; }

  constructor(private readonly api: PosCajaService, private readonly route: ActivatedRoute, private readonly router: Router) {
    this.cartId = Number(this.route.snapshot.paramMap.get('cartId'));
    if (!this.cartId) {
      void this.router.navigateByUrl('/pos/caja/inbox');
      return;
    }
    void this.reloadCart();
  }

  async reloadCart(): Promise<void> {
    try {
      this.errorMessage = '';
      const cart = await this.withBusy(() => this.api.getCart(this.cartId));
      this.cartItems = cart.items;
      this.cartTotal = Number(cart.total || 0);
      await this.ensureProductCatalog();
      this.focusScan();
    } catch (err: any) {
      if (err?.status === 404) {
        this.errorMessage = 'Este carrito no está disponible para edición en esta caja. Ingresá por Cobro o actualizá la bandeja.';
        return;
      }
      this.errorMessage = err?.error?.message ?? 'No se pudo cargar carrito';
    }
  }

  async quickAddFromScan(): Promise<void> {
    if (!this.scanCode.trim()) return;
    try {
      this.errorMessage = '';
      const product = await this.resolveInputProduct();
      if (!product) {
        this.errorMessage = 'No se encontro el producto. Escanea codigo o buscá por nombre.';
        return;
      }

      const quantity = Number(this.manualValue);
      if (!Number.isFinite(quantity) || quantity <= 0) {
        this.errorMessage = 'Ingresa una cantidad o peso valido mayor a 0.';
        return;
      }

      const isWeight = this.isWeightProduct(product);
      if (this.addMode === 'weight' && !isWeight) {
        this.errorMessage = 'El producto seleccionado no se vende por peso. Cambia el modo a Cantidad.';
        return;
      }
      if (this.addMode === 'quantity' && isWeight) {
        this.errorMessage = 'El producto seleccionado es pesable. Cambia el modo a Peso (kg).';
        return;
      }

      const unitPrice = this.addMode === 'weight'
        ? Number(product.defaultPricePerKg ?? product.defaultPrice ?? 0)
        : Number(product.defaultPrice ?? product.defaultPricePerKg ?? 0);

      if (unitPrice <= 0) {
        this.errorMessage = 'El producto no tiene precio configurado.';
        return;
      }

      await this.withBusy(() => this.api.addCartItem(this.cartId, {
        productId: product.id,
        productCode: this.resolveProductCode(product),
        productName: product.name,
        unitPrice,
        quantity,
        unit: this.addMode === 'weight' ? 'Weight' : 'Unit',
        discount: 0
      }));

      this.scanCode = '';
      this.selectedProduct = null;
      this.filteredProducts = [];
      this.showSuggestions = false;
      this.suggestionIndex = -1;
      this.addMode = 'quantity';
      this.manualValue = 1;
      await this.reloadCart();
    } catch (err: any) {
      this.errorMessage = err?.error?.message ?? 'No se pudo agregar producto';
    }
  }

  onScanInputChange(): void {
    const term = this.scanCode.trim();
    this.selectedProduct = null;

    if (!term) {
      this.filteredProducts = [];
      this.showSuggestions = false;
      this.suggestionIndex = -1;
      return;
    }

    const normalized = term.toLowerCase();
    this.filteredProducts = this.activeProducts
      .filter(p => {
        const name = `${p.name ?? ''}`.toLowerCase();
        const barcode = `${p.barcode ?? ''}`.toLowerCase();
        const quickCode = `${p.quickCode ?? ''}`.toLowerCase();
        return name.includes(normalized) || barcode.includes(normalized) || quickCode.includes(normalized);
      })
      .slice(0, 8);

    this.showSuggestions = this.filteredProducts.length > 0;
    this.suggestionIndex = this.filteredProducts.length > 0 ? 0 : -1;
  }

  onScanEnter(event: Event): void {
    if (this.showSuggestions && this.filteredProducts.length > 0) {
      event.preventDefault();
      const idx = this.suggestionIndex >= 0 ? this.suggestionIndex : 0;
      this.selectSuggestedProduct(this.filteredProducts[idx]);
      return;
    }

    event.preventDefault();
    void this.quickAddFromScan();
  }

  moveSuggestion(step: number, event: Event): void {
    if (!this.showSuggestions || this.filteredProducts.length === 0) return;
    event.preventDefault();
    const total = this.filteredProducts.length;
    const next = this.suggestionIndex < 0 ? 0 : (this.suggestionIndex + step + total) % total;
    this.suggestionIndex = next;
  }

  dismissSuggestions(): void {
    this.showSuggestions = false;
    this.suggestionIndex = -1;
  }

  selectSuggestedProduct(product: ProductLookupResponse): void {
    this.selectedProduct = product;
    this.scanCode = product.name;
    this.showSuggestions = false;
    this.filteredProducts = [];
    this.suggestionIndex = -1;
    this.addMode = this.isWeightProduct(product) ? 'weight' : 'quantity';
    this.manualValue = 1;
  }

  productCodesLabel(product: ProductLookupResponse): string {
    const parts: string[] = [];
    if (product.quickCode) parts.push(`QC:${product.quickCode}`);
    if (product.barcode) parts.push(product.barcode);
    return parts.join(' · ');
  }

  async removeItem(item: CartItem): Promise<void> {
    try {
      await this.withBusy(() => this.api.removeCartItem(this.cartId, item.id));
      await this.reloadCart();
    } catch (err: any) {
      this.errorMessage = err?.error?.message ?? 'No se pudo quitar item';
    }
  }

  closeWeightModal(): void {
    this.weightModal = null;
    this.focusScan();
  }

  async confirmWeightItem(): Promise<void> {
    if (!this.weightModal || this.weightKg <= 0 || this.weightPricePerKg <= 0) return;
    try {
      const product = this.weightModal.product;
      const pricePerKg = product.allowsManualPrice
        ? Number(this.weightPricePerKg)
        : Number(product.defaultPricePerKg ?? product.defaultPrice ?? this.weightPricePerKg);

      await this.withBusy(() => this.api.addCartItem(this.cartId, {
        productId: product.id,
        productCode: this.weightModal!.scannedCode,
        productName: product.name,
        unitPrice: pricePerKg,
        quantity: Number(this.weightKg),
        unit: 'Weight',
        discount: 0
      }));

      this.scanCode = '';
      this.closeWeightModal();
      await this.reloadCart();
    } catch (err: any) {
      this.errorMessage = err?.error?.message ?? 'No se pudo agregar pesable';
    }
  }

  private async ensureProductCatalog(): Promise<void> {
    if (this.productsLoaded) return;
    try {
      this.activeProducts = await this.withBusy(() => this.api.getActiveProducts());
      this.productsLoaded = true;
    } catch {
      this.activeProducts = [];
      this.productsLoaded = false;
    }
  }

  private async resolveInputProduct(): Promise<ProductLookupResponse | null> {
    if (this.selectedProduct) return this.selectedProduct;

    const input = this.scanCode.trim();
    if (!input) return null;

    try {
      return await this.withBusy(() => this.api.getProductByScan(input));
    } catch {
      const normalized = input.toLowerCase();
      const fromCatalog = this.activeProducts.find(p => {
        const name = `${p.name ?? ''}`.toLowerCase();
        const barcode = `${p.barcode ?? ''}`.toLowerCase();
        const quickCode = `${p.quickCode ?? ''}`.toLowerCase();
        return name === normalized || barcode === normalized || quickCode === normalized;
      }) ?? null;
      return fromCatalog;
    }
  }

  private resolveProductCode(product: ProductLookupResponse): string {
    return product.quickCode ?? product.barcode ?? `PID:${product.id}`;
  }

  private isWeightProduct(product: ProductLookupResponse): boolean {
    const saleType = (product.saleType ?? '').toLowerCase();
    const unitName = (product.unitName ?? '').toLowerCase();
    return saleType === 'weight' || unitName.includes('kg');
  }

  private focusScan(): void {
    queueMicrotask(() => this.scanInput?.nativeElement.focus());
  }

  private async withBusy<T>(fn: () => Promise<T>): Promise<T> {
    this.pendingRequests += 1;
    try { return await fn(); } finally { this.pendingRequests -= 1; }
  }
}

@Component({
  standalone: true,
  selector: 'app-pos-caja-cobro',
  imports: [CommonModule, FormsModule, RouterLink, PosModuleNavComponent],
  template: `
    <main class="cobro-container">
      <div class="hero-bg">
        <div class="hero-shape shape-1"></div>
        <div class="hero-shape shape-2"></div>
      </div>

      <app-pos-module-nav />

      <header class="hero">
        <div class="hero-content">
          <h1>Cobro de caja</h1>
          <p class="hero-subtitle">Carrito #{{ cartId }} - Total {{ cartTotal | number:'1.2-2' }}</p>
        </div>
        <div class="hero-actions">
          <a class="btn-ghost" [routerLink]="['/pos/caja/venta', cartId]">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="19" y1="12" x2="5" y2="12"></line>
              <polyline points="12 19 5 12 12 5"></polyline>
            </svg>
            Volver a venta
          </a>
          <a class="btn-ghost" routerLink="/pos/caja/inbox">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="22 12 16 12 14 15 10 15 8 12 2 12"></polyline>
              <path d="M2.36 11.86A8 8 0 0 1 13.64 5.64L22 12"></path>
            </svg>
            Bandeja
          </a>
        </div>
      </header>

      <section class="content-section">
        <div class="alert error" *ngIf="errorMessage">
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="15" y1="9" x2="9" y2="15"></line>
            <line x1="9" y1="9" x2="15" y2="15"></line>
          </svg>
          {{ errorMessage }}
        </div>

        <div class="card">
          <div class="card-header">
            <h2>
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
                <line x1="3" y1="6" x2="21" y2="6"></line>
                <path d="M16 10a4 4 0 0 1-8 0"></path>
              </svg>
              Resumen de items
            </h2>
          </div>
          <div class="items-list">
            <div *ngFor="let item of cartItems" class="item-row">
              <span>{{ item.productName }} x{{ item.quantity }}</span>
              <strong>{{ item.subtotal | number:'1.2-2' }}</strong>
            </div>
            <div class="item-row surcharge" *ngIf="backendCigaretteSurcharge > 0">
              <span>Recargo cigarrillos</span>
              <strong>{{ backendCigaretteSurcharge | number:'1.2-2' }}</strong>
            </div>
          </div>
        </div>

        <div class="card">
          <div class="card-header">
            <h2>
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="1" y="4" width="22" height="16" rx="2" ry="2"></rect>
                <line x1="1" y1="10" x2="23" y2="10"></line>
              </svg>
              Pagos
            </h2>
          </div>
          
          <div class="payment-form">
            <div class="form-row">
              <div class="form-group">
                <label>Metodo de pago</label>
                <select [(ngModel)]="paymentMethod" [disabled]="isBusy">
                  <option value="Cash">Efectivo</option>
                  <option value="Card" [disabled]="!health.isOnline">Tarjeta</option>
                  <option value="Transfer" [disabled]="!health.isOnline">Transferencia</option>
                  <option value="QrMp" [disabled]="!health.isOnline">QR Mercado Pago</option>
                  <option value="Credit" [disabled]="!health.isOnline">Cuenta corriente</option>
                </select>
              </div>
              <div class="form-group">
                <label>Monto</label>
                <input type="number" [(ngModel)]="paymentAmount" [disabled]="isBusy" placeholder="0.00" />
              </div>
            </div>
            
            <div class="form-row">
              <div class="form-group">
                <label>Referencia</label>
                <input type="text" [(ngModel)]="paymentReference" [disabled]="isBusy" placeholder="Numero de operacion" />
              </div>
              <div class="form-group checkbox-group">
                <label class="checkbox-label">
                  <input type="checkbox" [(ngModel)]="paymentPending" [disabled]="paymentMethod !== 'Transfer' || isBusy || !health.isOnline" />
                  <span>Transferencia pendiente</span>
                </label>
              </div>
            </div>

            <div class="credit-customer-panel" *ngIf="paymentMethod === 'Credit' || hasCreditInDraft() || containerCustomerRequired">
              <div class="credit-customer-header">
                <strong>{{ containerCustomerRequired ? 'Cliente para registrar envase adeudado' : 'Cliente para cuenta corriente' }}</strong>
                <span class="credit-cap" *ngIf="!containerCustomerRequired">Tope ocasional por operación: {{ OCCASIONAL_CREDIT_MAX | number:'1.0-0' }}</span>
              </div>
              <p class="hint" *ngIf="containerCustomerRequired" style="margin-top:0">El cliente no presentó envase. Seleccioná cliente cuenta corriente o creá cliente ocasional para registrar deuda de envase.</p>
              <div class="form-row credit-customer-row">
                <div class="form-group">
                  <label>Buscar cliente registrado</label>
                  <input type="text" [(ngModel)]="customerQuery" [disabled]="isBusy" placeholder="Nombre, DNI o teléfono" />
                </div>
                <div class="form-group">
                  <label>Cliente seleccionado</label>
                  <select [(ngModel)]="selectedCreditCustomerId" [disabled]="isBusy">
                    <option [ngValue]="null">Sin cliente seleccionado</option>
                    <option *ngFor="let c of filteredCreditCustomers()" [ngValue]="c.id">{{ c.fullName }}</option>
                  </select>
                </div>
              </div>
              <p class="hint" *ngIf="selectedCreditCustomer?.isCritical" style="margin-top:0">Alerta: cliente cercano al límite de crédito.</p>
              <p class="hint credit-danger" *ngIf="selectedCreditCustomer?.isCreditBlocked">La cuenta corriente del cliente seleccionado está bloqueada.</p>

              <div class="occasional-box">
                <div class="occasional-head">
                  <span>{{ containerCustomerRequired ? 'Si no está registrado, creá cliente ocasional para registrar envase adeudado.' : 'Si no está registrado, crea cliente ocasional al confirmar cobro.' }}</span>
                  <button type="button" class="btn-inline" [disabled]="isBusy" (click)="toggleOccasionalForm()">
                    {{ occasionalFormOpen ? 'Ocultar' : 'Cliente ocasional' }}
                  </button>
                </div>
                <div class="form-row" *ngIf="occasionalFormOpen">
                  <div class="form-group">
                    <label>Nombre y apellido</label>
                    <input type="text" [(ngModel)]="occasionalName" [disabled]="isBusy" placeholder="Ej: Juan Perez" />
                  </div>
                  <div class="form-group">
                    <label>Telefono (opcional)</label>
                    <input type="text" [(ngModel)]="occasionalPhone" [disabled]="isBusy" placeholder="Ej: 11 5555 0000" />
                  </div>
                </div>
              </div>
            </div>

            <button class="btn-add-payment" [disabled]="isBusy" (click)="addPayment()">
              <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="12" y1="5" x2="12" y2="19"></line>
                <line x1="5" y1="12" x2="19" y2="12"></line>
              </svg>
              Agregar pago
            </button>

            <p class="hint" *ngIf="!health.isOnline">
              <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="10"></circle>
                <line x1="12" y1="8" x2="12" y2="12"></line>
                <line x1="12" y1="16" x2="12.01" y2="16"></line>
              </svg>
              Modo offline: solo efectivo habilitado.
            </p>
          </div>

          <div class="payments-list" *ngIf="payments.length > 0">
            <div *ngFor="let p of payments; let i = index" class="payment-item">
              <div class="payment-info">
                <span class="payment-method">{{ paymentMethodLabel(p.paymentMethod) }}</span>
                <span class="payment-amount">{{ p.amount | number:'1.2-2' }}</span>
                <span class="payment-pending" *ngIf="p.isPending">(Pendiente)</span>
              </div>
              <button class="btn-remove" [disabled]="isBusy" (click)="removePayment(i)">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <line x1="18" y1="6" x2="6" y2="18"></line>
                  <line x1="6" y1="6" x2="18" y2="18"></line>
                </svg>
              </button>
            </div>
          </div>

          <div class="actions-footer">
            <button class="btn-confirm" [disabled]="isBusy || payments.length === 0" (click)="convertToSale()">
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="20 6 9 17 4 12"></polyline>
              </svg>
              Confirmar cobro
            </button>
          </div>

          <div class="receipt-actions" *ngIf="lastSaleReceiptId">
            <a [href]="'/print/sale/' + lastSaleReceiptId" target="_blank" class="btn-receipt">
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
                <polyline points="14 2 14 8 20 8"></polyline>
                <line x1="16" y1="13" x2="8" y2="13"></line>
                <line x1="16" y1="17" x2="8" y2="17"></line>
              </svg>
              Ver comprobante
            </a>
            <a [href]="'/print/sale/' + lastSaleReceiptId + '?autoprint=1'" target="_blank" class="btn-receipt">
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="6 9 6 2 18 2 18 9"></polyline>
                <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path>
                <rect x="6" y="14" width="12" height="8"></rect>
              </svg>
              Imprimir
            </a>
            <a [href]="'/print/sale/' + lastSaleReceiptId + '?autoprint=1&reprint=1'" target="_blank" class="btn-receipt">
              Reimprimir
            </a>
          </div>
        </div>
      </section>

    </main>
  `,
  styles: [`
    .cobro-container {
      min-height: 100vh;
      padding: 0 0 2rem 0;
      position: relative;
      overflow: hidden;
    }

    .hero-bg {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      height: 220px;
      background: linear-gradient(135deg, #1B4D3E 0%, #234F45 100%);
      overflow: hidden;
      z-index: 0;
    }

    .hero-shape {
      position: absolute;
      border-radius: 50%;
      filter: blur(80px);
      opacity: 0.4;
    }

    .shape-1 {
      width: 280px;
      height: 280px;
      background: #BFEBF1;
      top: -100px;
      right: -60px;
      opacity: 0.2;
    }

    .shape-2 {
      width: 200px;
      height: 200px;
      background: #a8d8e0;
      bottom: -60px;
      left: -40px;
      opacity: 0.25;
    }

    .hero {
      position: relative;
      z-index: 1;
      padding: 1.5rem;
      max-width: 800px;
      margin: 0 auto;
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 1rem;
      flex-wrap: wrap;
    }

    .hero-content { animation: fadeInUp 0.5s ease-out; }
    .hero-actions { animation: fadeInUp 0.5s ease-out 0.1s backwards; }

    @keyframes fadeInUp {
      from { opacity: 0; transform: translateY(20px); }
      to { opacity: 1; transform: translateY(0); }
    }

    .hero h1 {
      font-size: 1.75rem;
      font-weight: 700;
      color: #FFFFFF;
      margin: 0 0 0.3rem 0;
    }

    .hero-subtitle {
      font-size: 0.95rem;
      color: rgba(255, 255, 255, 0.7);
      margin: 0;
    }

    .hero-actions {
      display: flex;
      gap: 0.75rem;
    }

    .btn-ghost {
      display: flex;
      align-items: center;
      gap: 0.4rem;
      padding: 0.7rem 1rem;
      background: transparent;
      color: #FFFFFF;
      border: 1px dashed rgba(255, 255, 255, 0.4);
      border-radius: 10px;
      font-size: 0.9rem;
      font-weight: 500;
      text-decoration: none;
      transition: all 0.2s ease;
    }

    .btn-ghost:hover {
      background: rgba(255, 255, 255, 0.1);
      border-style: solid;
    }

    .content-section {
      position: relative;
      z-index: 1;
      max-width: 800px;
      margin: 0 auto;
      padding: 0 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .alert {
      display: flex;
      align-items: center;
      gap: 0.6rem;
      padding: 0.75rem 1rem;
      border-radius: 10px;
      font-size: 0.9rem;
      font-weight: 500;
    }

    .alert.error {
      background: #f8d7da;
      color: #721c24;
    }

    .card {
      background: #FFFFFF;
      border-radius: 16px;
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
      overflow: hidden;
    }

    .card-header {
      padding: 1.25rem 1.5rem;
      border-bottom: 1px solid #e9ecef;
    }

    .card-header h2 {
      display: flex;
      align-items: center;
      gap: 0.6rem;
      font-size: 1.1rem;
      font-weight: 600;
      color: #1B4D3E;
      margin: 0;
    }

    .card-header h2 svg {
      color: #BFEBF1;
      background: #1B4D3E;
      padding: 4px;
      border-radius: 6px;
    }

    .items-list {
      padding: 0.5rem 0;
    }

    .item-row {
      display: flex;
      justify-content: space-between;
      padding: 0.75rem 1.5rem;
      border-bottom: 1px solid #f1f3f5;
    }

    .item-row.surcharge {
      background: #fff3cd;
    }

    .item-row span {
      color: #495057;
    }

    .item-row strong {
      color: #1B4D3E;
    }

    .payment-form {
      padding: 1.25rem 1.5rem;
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
      margin-bottom: 1rem;
    }

    .form-group label {
      display: block;
      font-size: 0.85rem;
      font-weight: 500;
      color: #495057;
      margin-bottom: 0.4rem;
    }

    .form-group input,
    .form-group select {
      width: 100%;
      padding: 0.7rem 1rem;
      font-size: 1rem;
      border: 2px solid #e9ecef;
      border-radius: 10px;
      background: #f8fafc;
      color: #1B4D3E;
      transition: all 0.2s ease;
    }

    .form-group input:focus,
    .form-group select:focus {
      outline: none;
      border-color: #BFEBF1;
      box-shadow: 0 0 0 4px rgba(191, 235, 241, 0.2);
    }

    .checkbox-group {
      display: flex;
      align-items: flex-end;
    }

    .checkbox-label {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      cursor: pointer;
      font-size: 0.9rem;
      color: #495057;
    }

    .checkbox-label input {
      width: 18px;
      height: 18px;
      accent-color: #1B4D3E;
    }

    .btn-add-payment {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      width: 100%;
      padding: 0.75rem;
      background: #e9ecef;
      color: #495057;
      border: none;
      border-radius: 10px;
      font-size: 0.95rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
      margin-top: 0.5rem;
    }

    .btn-add-payment:hover:not(:disabled) {
      background: #dee2e6;
    }

    .btn-add-payment:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .hint {
      display: flex;
      align-items: center;
      gap: 0.4rem;
      font-size: 0.8rem;
      color: #856404;
      margin-top: 0.75rem;
      padding: 0.5rem 0.75rem;
      background: #fff3cd;
      border-radius: 8px;
    }

    .credit-customer-panel {
      border: 1px solid #dcebe6;
      border-radius: 12px;
      background: #f7fbf9;
      padding: 0.85rem;
      margin-bottom: 0.8rem;
    }

    .credit-customer-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 0.6rem;
      margin-bottom: 0.6rem;
      color: #1b4d3e;
      flex-wrap: wrap;
    }

    .credit-cap {
      font-size: 0.8rem;
      font-weight: 600;
      color: #8a5a00;
      background: #fff6df;
      border-radius: 999px;
      padding: 0.2rem 0.6rem;
    }

    .credit-customer-row {
      margin-bottom: 0.6rem;
    }

    .occasional-box {
      border-top: 1px dashed #c9ddd5;
      padding-top: 0.6rem;
    }

    .occasional-head {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 0.7rem;
      color: #355b4f;
      font-size: 0.85rem;
      margin-bottom: 0.5rem;
      flex-wrap: wrap;
    }

    .btn-inline {
      border: 1px solid #c0ddd3;
      background: #ecf8f2;
      color: #1b4d3e;
      border-radius: 999px;
      padding: 0.35rem 0.7rem;
      font-size: 0.8rem;
      font-weight: 600;
      cursor: pointer;
    }

    .btn-inline:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .hint.credit-danger {
      background: #fde9ea;
      color: #9f1f1f;
    }

    .payments-list {
      padding: 0.5rem 1.5rem;
      background: #f8fafc;
    }

    .payment-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.75rem 0;
      border-bottom: 1px solid #e9ecef;
    }

    .payment-item:last-child {
      border-bottom: none;
    }

    .payment-info {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .payment-method {
      font-weight: 600;
      color: #1B4D3E;
    }

    .payment-amount {
      color: #28a745;
      font-weight: 600;
    }

    .payment-pending {
      color: #ffc107;
      font-size: 0.85rem;
    }

    .btn-remove {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      background: #f8d7da;
      color: #721c24;
      border: none;
      border-radius: 6px;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .btn-remove:hover:not(:disabled) {
      background: #f5c6cb;
    }

    .actions-footer {
      padding: 1.25rem 1.5rem;
      border-top: 1px solid #e9ecef;
    }

    .btn-confirm {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      width: 100%;
      padding: 1rem;
      background: linear-gradient(135deg, #28a745 0%, #20c997 100%);
      color: #FFFFFF;
      border: none;
      border-radius: 12px;
      font-size: 1.1rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s ease;
      box-shadow: 0 4px 12px rgba(40, 167, 69, 0.3);
    }

    .btn-confirm:hover:not(:disabled) {
      transform: translateY(-2px);
      box-shadow: 0 6px 20px rgba(40, 167, 69, 0.4);
    }

    .btn-confirm:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .receipt-actions {
      display: flex;
      gap: 0.75rem;
      padding: 1rem 1.5rem;
      border-top: 1px solid #e9ecef;
      background: #f8fafc;
      flex-wrap: wrap;
    }

    .btn-receipt {
      display: flex;
      align-items: center;
      gap: 0.4rem;
      padding: 0.5rem 0.85rem;
      background: #FFFFFF;
      color: #1B4D3E;
      border: 1px solid #e9ecef;
      border-radius: 8px;
      font-size: 0.85rem;
      font-weight: 500;
      text-decoration: none;
      transition: all 0.2s ease;
    }

    .btn-receipt:hover {
      background: #1B4D3E;
      color: #FFFFFF;
      border-color: #1B4D3E;
    }


    @media (max-width: 640px) {
      .hero {
        flex-direction: column;
        align-items: stretch;
      }

      .hero-actions {
        justify-content: stretch;
      }

      .hero-actions a {
        flex: 1;
        justify-content: center;
      }

      .form-row {
        grid-template-columns: 1fr;
      }

      .receipt-actions {
        flex-direction: column;
      }

      .btn-receipt {
        justify-content: center;
      }

    }
  `]
})
export class PosCajaCobroComponent {
  readonly OCCASIONAL_CREDIT_MAX = 25000;

  cartId = 0;
  cartItems: CartItem[] = [];
  cartTotal = 0;
  backendCigaretteSurcharge = 0;
  errorMessage = '';
  confirmDialog: { message: string; onConfirm: () => Promise<void> } | null = null;
  customers: CustomerRef[] = [];
  customerQuery = '';
  selectedCreditCustomerId: number | null = null;
  occasionalFormOpen = false;
  occasionalName = '';
  occasionalPhone = '';
  containerCustomerRequired = false;

  paymentMethod = 'Cash';
  paymentAmount = 0;
  paymentReference = '';
  paymentPending = false;
  payments: PaymentDraft[] = [];
  lastSaleReceiptId: number | null = null;
  private pendingRequests = 0;

  get isBusy(): boolean { return this.pendingRequests > 0; }

  get selectedCreditCustomer(): CustomerRef | null {
    if (!this.selectedCreditCustomerId) return null;
    return this.customers.find(c => c.id === this.selectedCreditCustomerId) ?? null;
  }

  constructor(
    private readonly api: PosCajaService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly operatorSessionService: OperatorSessionService,
    private readonly activityService: ActivityService,
    private readonly dialog: DialogService,
    private readonly offlineQueue: OfflineQueueService,
    public readonly health: HealthService
  ) {
    this.cartId = Number(this.route.snapshot.paramMap.get('cartId') ?? this.route.snapshot.paramMap.get('saleId'));
    if (!this.cartId) {
      void this.router.navigateByUrl('/pos/caja/inbox');
      return;
    }
    void this.loadCart();
  }

  async loadCart(): Promise<void> {
    try {
      this.errorMessage = '';
      const cart = await this.withBusy(() => this.api.getCart(this.cartId));
      this.cartItems = cart.items;
      this.cartTotal = Number(cart.total || 0);
      this.backendCigaretteSurcharge = Number(cart.cigaretteSurcharge ?? 0);
      this.paymentAmount = Number(this.cartTotal.toFixed(2));
      if (this.health.isOnline && this.customers.length === 0) {
        this.customers = await this.withBusy(() => this.api.getCustomers());
      }
    } catch (err: any) {
      if (err?.status === 404 && this.lastSaleReceiptId) {
        return;
      }
      this.errorMessage = err?.error?.message ?? 'No se pudo cargar carrito para cobro';
    }
  }

  addPayment(): void {
    if (this.paymentAmount <= 0) return;
    if (!this.health.isOnline && this.paymentMethod !== 'Cash') {
      this.errorMessage = 'Offline: solo efectivo habilitado';
      return;
    }

    if (this.paymentMethod === 'Credit' && this.paymentAmount > this.OCCASIONAL_CREDIT_MAX && !this.selectedCreditCustomerId) {
      this.errorMessage = `El monto máximo para cliente ocasional es ${this.OCCASIONAL_CREDIT_MAX}.`;
      return;
    }

    if (this.paymentMethod === 'Credit' && this.selectedCreditCustomer?.isCreditBlocked) {
      this.errorMessage = 'La cuenta corriente del cliente seleccionado está bloqueada.';
      return;
    }

    this.payments.push({
      paymentMethod: this.paymentMethod,
      amount: Number(this.paymentAmount),
      reference: this.paymentReference || undefined,
      isPending: this.paymentMethod === 'Transfer' ? this.paymentPending : this.paymentMethod === 'QrMp'
    });

    this.paymentAmount = 0;
    this.paymentReference = '';
    this.paymentPending = false;
  }

  removePayment(index: number): void {
    this.payments.splice(index, 1);
  }

  async convertToSale(): Promise<void> {
    if (this.payments.length === 0) return;
    if (!(await this.ensureActiveIdentity())) return;

    if (!this.health.isOnline) {
      const cashOnly = this.payments.every(p => p.paymentMethod === 'Cash' && !p.isPending);
      if (!cashOnly) {
        this.errorMessage = 'Offline: solo cobro en efectivo permitido';
        return;
      }

      const totalCash = this.payments.reduce((sum, p) => sum + p.amount, 0);
      await this.withBusy(() => this.offlineQueue.createTicket({
        operatorAlias: this.operatorSessionService.getOperatorName(),
        items: this.cartItems.map(i => ({
          code: i.productCode,
          name: i.productName,
          quantity: Number(i.quantity),
          unitPrice: Number(i.unitPrice)
        })),
        totalCash
      }));

      this.payments = [];
      this.errorMessage = '';
      alert('Ticket offline creado correctamente');
      return;
    }

    try {
      const hasCredit = this.hasCreditInDraft();
      const creditTotal = this.creditDraftTotal();
      let creditCustomerId: number | null = this.selectedCreditCustomerId;
      let saleCustomerId: number | undefined;

      if (hasCredit) {
        if (creditCustomerId && this.selectedCreditCustomer?.isCreditBlocked) {
          this.errorMessage = 'La cuenta corriente del cliente seleccionado está bloqueada.';
          return;
        }

        if (!creditCustomerId) {
          if (creditTotal > this.OCCASIONAL_CREDIT_MAX) {
            this.errorMessage = `El monto máximo para cliente ocasional es ${this.OCCASIONAL_CREDIT_MAX}.`;
            return;
          }

          if (!this.occasionalName.trim()) {
            this.occasionalFormOpen = true;
            this.errorMessage = 'Completá nombre para crear cliente ocasional o seleccioná uno existente.';
            return;
          }

          const created = await this.withBusy(() =>
            this.api.createOccasionalCreditCustomer({
              fullName: this.occasionalName.trim(),
              phone: this.occasionalPhone.trim() || undefined,
              creditLimit: Math.min(creditTotal, this.OCCASIONAL_CREDIT_MAX)
            })
          );
          this.customers = [created, ...this.customers.filter(c => c.id !== created.id)];
          creditCustomerId = created.id;
          this.selectedCreditCustomerId = created.id;
          this.occasionalFormOpen = false;
        }

        saleCustomerId = creditCustomerId ?? undefined;
      }

        const containerCheck = await this.withBusy(() => this.api.getCartContainerCheck(this.cartId));
        if (containerCheck.hasOwedContainers && !saleCustomerId) {
          if (!this.containerCustomerRequired) {
            const presented = await this.dialog.confirm({
              title: 'Envases retornables',
              message: 'Este carrito incluye envases retornables. ¿El cliente presentó envase ahora?',
              yesLabel: 'SI, PRESENTO ENVASE',
              noLabel: 'NO, NO PRESENTO'
            });
            this.containerCustomerRequired = !presented;
          }

        if (this.containerCustomerRequired) {
          if (!creditCustomerId) {
            if (!this.occasionalName.trim()) {
              this.occasionalFormOpen = true;
              this.errorMessage = 'Para registrar envase adeudado, seleccioná cliente o creá cliente ocasional.';
              return;
            }

            const createdContainerCustomer = await this.withBusy(() =>
              this.api.createOccasionalContainerCustomer({
                fullName: this.occasionalName.trim(),
                phone: this.occasionalPhone.trim() || undefined
              })
            );
            this.customers = [createdContainerCustomer, ...this.customers.filter(c => c.id !== createdContainerCustomer.id)];
            creditCustomerId = createdContainerCustomer.id;
            this.selectedCreditCustomerId = createdContainerCustomer.id;
            this.occasionalFormOpen = false;
          }

          saleCustomerId = creditCustomerId ?? undefined;
        }
      }

      const salePayments = this.payments.map(p => ({
        ...p,
        paymentMethod: p.paymentMethod === 'Credit' ? 'AccountCredit' : p.paymentMethod
      }));

      const sale = await this.withBusy(() => this.api.createSaleFromCart(this.cartId, {
        customerId: saleCustomerId,
        accountCreditCustomerId: hasCredit ? creditCustomerId ?? undefined : undefined,
        discount: 0,
        payments: salePayments
      }));

      this.lastSaleReceiptId = sale.id;
      this.payments = [];
      this.errorMessage = '';
      this.occasionalName = '';
      this.occasionalPhone = '';
      this.containerCustomerRequired = false;
      this.cartItems = [];
      this.cartTotal = 0;
      this.backendCigaretteSurcharge = 0;
      this.paymentAmount = 0;
    } catch (err: any) {
      this.errorMessage = err?.error?.message ?? 'No se pudo convertir a venta';
    }
  }

  paymentMethodLabel(method: string): string {
    if (method === 'Cash') return 'Efectivo';
    if (method === 'Card') return 'Tarjeta';
    if (method === 'Transfer') return 'Transferencia';
    if (method === 'QrMp') return 'QR Mercado Pago';
    if (method === 'Credit') return 'Cuenta corriente';
    return method;
  }

  filteredCreditCustomers(): CustomerRef[] {
    const q = this.customerQuery.trim().toLowerCase();
    let rows = this.customers.filter(c => !q || `${c.fullName ?? ''} ${c.dni ?? ''} ${c.phone ?? ''}`.toLowerCase().includes(q));
    if (this.selectedCreditCustomerId && !rows.some(c => c.id === this.selectedCreditCustomerId)) {
      const selected = this.customers.find(c => c.id === this.selectedCreditCustomerId);
      if (selected) rows = [selected, ...rows];
    }
    return rows.slice(0, 30);
  }

  hasCreditInDraft(): boolean {
    return this.payments.some(p => p.paymentMethod === 'Credit');
  }

  creditDraftTotal(): number {
    return this.payments
      .filter(p => p.paymentMethod === 'Credit')
      .reduce((sum, p) => sum + Number(p.amount || 0), 0);
  }

  toggleOccasionalForm(): void {
    this.occasionalFormOpen = !this.occasionalFormOpen;
  }

  private async ensureActiveIdentity(): Promise<boolean> {
    const confirmed = await this.activityService.ensureRecentIdentity({
      idleSeconds: 60,
      confirmationMessage: 'Pasaron mas de 60s sin interaccion. Sos vos?',
      pinPrompt: () => this.operatorSessionService.requestPin()
    });

    if (!confirmed) {
      this.errorMessage = 'Accion cancelada por inactividad';
    }

    return confirmed;
  }

  async runConfirmAction(): Promise<void> {
    const action = this.confirmDialog?.onConfirm;
    this.confirmDialog = null;
    if (action) {
      await action();
    }
  }

  private async withBusy<T>(fn: () => Promise<T>): Promise<T> {
    this.pendingRequests += 1;
    try { return await fn(); } finally { this.pendingRequests -= 1; }
  }
}

@Component({
  standalone: true,
  selector: 'app-pos-caja-cierre',
  imports: [CommonModule, FormsModule, RouterLink, PosModuleNavComponent],
  template: `
    <main class="cierre-container">
      <div class="hero-bg">
        <div class="hero-shape shape-1"></div>
        <div class="hero-shape shape-2"></div>
      </div>

      <app-pos-module-nav />

      <header class="hero">
        <div class="hero-content">
          <h1>Cierre de caja</h1>
          <p class="hero-subtitle" *ngIf="cashSession">Sesion #{{ cashSession.id }} ({{ shiftLabel(cashSession.shift) }})</p>
          <p class="hero-subtitle error" *ngIf="!cashSession">No hay sesion abierta para cerrar.</p>
        </div>
        <div class="hero-actions">
          <a class="btn-ghost" routerLink="/pos/caja/apertura">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
              <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
            </svg>
            Ir a apertura
          </a>
          <a class="btn-ghost" routerLink="/pos/caja/inbox">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="22 12 16 12 14 15 10 15 8 12 2 12"></polyline>
              <path d="M2.36 11.86A8 8 0 0 1 13.64 5.64L22 12"></path>
            </svg>
            Bandeja
          </a>
        </div>
      </header>

      <section class="content-section">
        <div class="alert error" *ngIf="errorMessage">
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="15" y1="9" x2="9" y2="15"></line>
            <line x1="9" y1="9" x2="15" y2="15"></line>
          </svg>
          {{ errorMessage }}
        </div>

        <div class="card">
          <div class="card-header">
            <h2>
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="12" y1="1" x2="12" y2="23"></line>
                <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"></path>
              </svg>
              Declarados
            </h2>
          </div>

          <div class="session-audit" *ngIf="cashSession">
            <span><b>Apertura:</b> {{ cashSession.openedByUsername || 'Sin dato' }}</span>
            <span><b>Al mando:</b> {{ cashSession.currentUsername || 'Sin dato' }}</span>
            <span><b>Cierre:</b> {{ cashSession.closedByUsername || 'Pendiente' }}</span>
          </div>

          <div class="form-grid">
            <div class="form-group">
              <label>Efectivo declarado ($)</label>
              <input type="number" [(ngModel)]="declaredCash" [disabled]="isBusy" placeholder="0.00" />
            </div>
            <div class="form-group">
              <label>Tarjeta declarada ($)</label>
              <input type="number" [(ngModel)]="declaredCard" [disabled]="isBusy" placeholder="0.00" />
            </div>
            <div class="form-group">
              <label>Transferencia declarada ($)</label>
              <input type="number" [(ngModel)]="declaredTransfer" [disabled]="isBusy" placeholder="0.00" />
            </div>
            <div class="form-group">
              <label>Cuenta corriente declarada ($)</label>
              <input type="number" [(ngModel)]="declaredCredit" [disabled]="isBusy" placeholder="0.00" />
            </div>
            <div class="form-group full-width">
              <label>Notas</label>
              <input type="text" [(ngModel)]="closeNotes" [disabled]="isBusy" placeholder="Notas adicionales del cierre" />
            </div>
          </div>

          <div class="actions-footer">
            <button class="btn-handover" [disabled]="isBusy || !cashSession" (click)="takeOverCashSession()">
              Tomar mando
            </button>
            <button class="btn-close" [disabled]="isBusy || !cashSession" (click)="closeSession()">
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
                <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
              </svg>
              Cerrar caja
            </button>
          </div>

          <div class="handover-history" *ngIf="handoverHistory.length > 0">
            <h3>Cambios de operador</h3>
            <ul>
              <li *ngFor="let h of handoverHistory">
                {{ h.createdAt | date:'dd/MM HH:mm' }} · {{ h.fromUsername || 'Sin dato' }} -> {{ h.toUsername }} · {{ h.reason }}
              </li>
            </ul>
          </div>

          <div class="print-actions" *ngIf="lastClosedSessionId">
            <a [href]="'/print/cash-close/' + lastClosedSessionId + '?autoprint=1'" target="_blank" class="btn-print">
              <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="6 9 6 2 18 2 18 9"></polyline>
                <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path>
                <rect x="6" y="14" width="12" height="8"></rect>
              </svg>
              Imprimir cierre
            </a>
          </div>
        </div>

        <div class="card block" *ngIf="closeBlock">
          <div class="card-header warning">
            <h2>
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path>
                <line x1="12" y1="9" x2="12" y2="13"></line>
                <line x1="12" y1="17" x2="12.01" y2="17"></line>
              </svg>
              Cierre bloqueado
            </h2>
          </div>
          <div class="block-content">
            <p *ngIf="closeBlock.blockedByCigarettesCount">Falta conteo de cigarrillos</p>
            <button class="btn-count-cigarettes" *ngIf="closeBlock.blockedByCigarettesCount && cashSession" [disabled]="isBusy || cigaretteCountBusy" (click)="openCigaretteCountModal()">
              {{ cigaretteCountBusy ? 'Cargando conteo...' : 'Realizar conteo de cigarrillos' }}
            </button>
            <ul>
              <li *ngFor="let t of closeBlock.missingRequiredTasks">{{ closeTaskLabel(t) }}</li>
            </ul>
          </div>
        </div>
      </section>
    </main>

    <div class="modal-overlay" *ngIf="showHandoverModal">
      <div class="modal handover-modal" role="dialog" aria-modal="true" (click)="$event.stopPropagation()">
        <h3>Cambio de operador</h3>
        <p class="modal-subtitle">Ingresá el operador entrante para transferir el mando de caja.</p>

        <p class="count-error" *ngIf="handoverError">{{ handoverError }}</p>

        <div class="handover-grid">
          <div class="form-group handover-field handover-full">
            <label>Motivo del cambio</label>
            <input type="text" [disabled]="isBusy" [(ngModel)]="handoverReason" placeholder="Ej: emergencia, relevo de turno" />
          </div>

          <div class="form-group handover-field">
            <label>Usuario nuevo operador</label>
            <input type="text" [disabled]="isBusy" [(ngModel)]="handoverUsername" placeholder="usuario" />
          </div>

          <div class="form-group handover-field">
            <label>Contraseña</label>
            <input type="password" [disabled]="isBusy" [(ngModel)]="handoverPassword" placeholder="contraseña" autocomplete="current-password" />
          </div>

          <div class="form-group handover-field handover-pin">
            <label>PIN del nuevo operador</label>
            <input type="password" inputmode="numeric" [disabled]="isBusy" [(ngModel)]="handoverPin" placeholder="PIN (4 a 6 digitos)" maxlength="6" autocomplete="current-password" />
          </div>
        </div>

        <div class="modal-actions">
          <button class="btn-secondary" [disabled]="isBusy" (click)="cancelHandoverModal()">Cancelar</button>
          <button class="btn-primary" [disabled]="isBusy" (click)="submitTakeOverCashSession()">
            {{ isBusy ? 'Procesando...' : 'Tomar mando' }}
          </button>
        </div>
      </div>
    </div>

    <div class="modal-overlay" *ngIf="showCigaretteCountModal">
      <div class="modal cigarette-count-modal" role="dialog" aria-modal="true" (click)="$event.stopPropagation()">
        <h3>Conteo de cigarrillos</h3>
        <p class="modal-subtitle">Registrá el conteo físico para habilitar el cierre de caja.</p>

        <p class="count-error" *ngIf="cigaretteCountError">{{ cigaretteCountError }}</p>

        <div class="count-list" *ngIf="cigaretteCountRows.length > 0">
          <div class="count-row count-header">
            <span>Producto</span>
            <span>Sistema</span>
            <span>Conteo</span>
          </div>
          <div class="count-row" *ngFor="let row of cigaretteCountRows">
            <span class="count-product">{{ row.productName }}</span>
            <span class="count-system">{{ row.systemQty | number:'1.2-2' }}</span>
            <input type="number" min="0" step="0.01" [disabled]="isBusy || cigaretteCountBusy" [(ngModel)]="row.countedQty" />
          </div>
        </div>

        <p class="count-empty" *ngIf="!cigaretteCountBusy && cigaretteCountRows.length === 0">No hay productos de cigarrillos para contar.</p>

        <div class="form-group count-notes">
          <label>Notas del conteo</label>
          <input type="text" [disabled]="isBusy || cigaretteCountBusy" [(ngModel)]="cigaretteCountNotes" placeholder="Observaciones (opcional)" />
        </div>

        <div class="modal-actions">
          <button class="btn-secondary" [disabled]="cigaretteCountBusy" (click)="cancelCigaretteCountModal()">Cancelar</button>
          <button class="btn-primary" [disabled]="isBusy || cigaretteCountBusy" (click)="submitCigaretteCount()">
            {{ cigaretteCountBusy ? 'Guardando...' : 'Guardar conteo y validar cierre' }}
          </button>
        </div>
      </div>
    </div>

    <div class="modal-overlay" *ngIf="confirmDialog">
      <div class="modal confirm-modal">
        <div class="modal-icon warning">
          <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="12" y1="8" x2="12" y2="12"></line>
            <line x1="12" y1="16" x2="12.01" y2="16"></line>
          </svg>
        </div>
        <h3>Confirmacion</h3>
        <p>{{ confirmDialog.message }}</p>
        <div class="modal-actions">
          <button class="btn-secondary" (click)="confirmDialog = null">Cancelar</button>
          <button class="btn-primary" (click)="runConfirmAction()">Confirmar</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .cierre-container {
      min-height: 100vh;
      padding: 0 0 2rem 0;
      position: relative;
      overflow: hidden;
    }

    .hero-bg {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      height: 220px;
      background: linear-gradient(135deg, #1B4D3E 0%, #234F45 100%);
      overflow: hidden;
      z-index: 0;
    }

    .hero-shape {
      position: absolute;
      border-radius: 50%;
      filter: blur(80px);
      opacity: 0.4;
    }

    .shape-1 {
      width: 280px;
      height: 280px;
      background: #BFEBF1;
      top: -100px;
      right: -60px;
      opacity: 0.2;
    }

    .shape-2 {
      width: 200px;
      height: 200px;
      background: #a8d8e0;
      bottom: -60px;
      left: -40px;
      opacity: 0.25;
    }

    .hero {
      position: relative;
      z-index: 1;
      padding: 1.5rem;
      max-width: 600px;
      margin: 0 auto;
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 1rem;
      flex-wrap: wrap;
    }

    .hero-content { animation: fadeInUp 0.5s ease-out; }
    .hero-actions { animation: fadeInUp 0.5s ease-out 0.1s backwards; }

    @keyframes fadeInUp {
      from { opacity: 0; transform: translateY(20px); }
      to { opacity: 1; transform: translateY(0); }
    }

    .hero h1 {
      font-size: 1.75rem;
      font-weight: 700;
      color: #FFFFFF;
      margin: 0 0 0.3rem 0;
    }

    .hero-subtitle {
      font-size: 0.95rem;
      color: rgba(255, 255, 255, 0.7);
      margin: 0;
    }

    .hero-subtitle.error {
      color: #ffc107;
    }

    .hero-actions {
      display: flex;
      gap: 0.75rem;
    }

    .btn-ghost {
      display: flex;
      align-items: center;
      gap: 0.4rem;
      padding: 0.7rem 1rem;
      background: transparent;
      color: #FFFFFF;
      border: 1px dashed rgba(255, 255, 255, 0.4);
      border-radius: 10px;
      font-size: 0.9rem;
      font-weight: 500;
      text-decoration: none;
      transition: all 0.2s ease;
    }

    .btn-ghost:hover {
      background: rgba(255, 255, 255, 0.1);
      border-style: solid;
    }

    .content-section {
      position: relative;
      z-index: 1;
      max-width: 600px;
      margin: 0 auto;
      padding: 0 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .alert {
      display: flex;
      align-items: center;
      gap: 0.6rem;
      padding: 0.75rem 1rem;
      border-radius: 10px;
      font-size: 0.9rem;
      font-weight: 500;
    }

    .alert.error {
      background: #f8d7da;
      color: #721c24;
    }

    .card {
      background: #FFFFFF;
      border-radius: 16px;
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
      overflow: hidden;
    }

    .card-header {
      padding: 1.25rem 1.5rem;
      border-bottom: 1px solid #e9ecef;
    }

    .card-header.warning {
      background: #fff3cd;
      border-bottom-color: #ffe69c;
    }

    .card-header h2 {
      display: flex;
      align-items: center;
      gap: 0.6rem;
      font-size: 1.1rem;
      font-weight: 600;
      color: #1B4D3E;
      margin: 0;
    }

    .card-header.warning h2 {
      color: #856404;
    }

    .card-header h2 svg {
      color: #BFEBF1;
      background: #1B4D3E;
      padding: 4px;
      border-radius: 6px;
    }

    .card-header.warning h2 svg {
      background: #856404;
      color: #fff3cd;
    }

    .session-audit {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 0.6rem;
      padding: 0.85rem 1.5rem;
      border-top: 1px solid #e9ecef;
      border-bottom: 1px solid #e9ecef;
      background: #f7faf9;
      font-size: 0.9rem;
      color: #2f4f46;
    }

    .form-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
      padding: 1.25rem 1.5rem;
    }

    .form-group.full-width {
      grid-column: 1 / -1;
    }

    .form-group label {
      display: block;
      font-size: 0.85rem;
      font-weight: 500;
      color: #495057;
      margin-bottom: 0.4rem;
    }

    .form-group input {
      width: 100%;
      padding: 0.75rem 1rem;
      font-size: 1rem;
      border: 2px solid #e9ecef;
      border-radius: 10px;
      background: #f8fafc;
      color: #1B4D3E;
      transition: all 0.2s ease;
    }

    .form-group input:focus {
      outline: none;
      border-color: #BFEBF1;
      box-shadow: 0 0 0 4px rgba(191, 235, 241, 0.2);
    }

    .actions-footer {
      padding: 1rem 1.5rem;
      border-top: 1px solid #e9ecef;
      display: flex;
      gap: 0.75rem;
    }

    .btn-handover {
      flex: 0 0 170px;
      padding: 1rem;
      border-radius: 12px;
      border: 1px solid #9fd8ca;
      background: #e6f6f1;
      color: #1b4d3e;
      font-size: 0.98rem;
      font-weight: 600;
      cursor: pointer;
    }

    .btn-handover:hover:not(:disabled) {
      background: #d6f0e8;
    }

    .btn-handover:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .btn-close {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      flex: 1;
      padding: 1rem;
      background: linear-gradient(135deg, #dc3545 0%, #c82333 100%);
      color: #FFFFFF;
      border: none;
      border-radius: 12px;
      font-size: 1.1rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s ease;
      box-shadow: 0 4px 12px rgba(220, 53, 69, 0.3);
    }

    .btn-close:hover:not(:disabled) {
      transform: translateY(-2px);
      box-shadow: 0 6px 20px rgba(220, 53, 69, 0.4);
    }

    .btn-close:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .print-actions {
      padding: 1rem 1.5rem;
      border-top: 1px solid #e9ecef;
      background: #f8fafc;
    }

    .btn-print {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      width: 100%;
      padding: 0.75rem;
      background: #1B4D3E;
      color: #FFFFFF;
      border: none;
      border-radius: 10px;
      font-size: 0.95rem;
      font-weight: 600;
      text-decoration: none;
      transition: all 0.2s ease;
    }

    .btn-print:hover {
      background: #234F45;
    }

    .handover-history {
      padding: 0.85rem 1.5rem 1.1rem;
      border-top: 1px solid #e9ecef;
      background: #fcfdfd;
    }

    .handover-history h3 {
      margin: 0 0 0.5rem;
      font-size: 0.95rem;
      color: #234f45;
    }

    .handover-history ul {
      margin: 0;
      padding-left: 1.1rem;
      color: #5f6f68;
      font-size: 0.86rem;
    }

    .block {
      background: #fff3cd;
      border: 1px solid #ffe69c;
    }

    .block-content {
      padding: 1rem 1.5rem;
    }

    .block-content p {
      color: #856404;
      font-weight: 500;
      margin: 0 0 0.5rem 0;
    }

    .block-content ul {
      margin: 0;
      padding-left: 1.25rem;
      color: #856404;
    }

    .block-content li {
      margin-bottom: 0.25rem;
    }

    .btn-count-cigarettes {
      border: 1px solid #d8b75f;
      background: #fff9e8;
      color: #7f5600;
      border-radius: 10px;
      padding: 0.65rem 0.85rem;
      font-size: 0.88rem;
      font-weight: 700;
      cursor: pointer;
      margin-bottom: 0.75rem;
    }

    .btn-count-cigarettes:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .modal-overlay {
      position: fixed;
      inset: 0;
      background: rgba(15, 23, 42, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 1rem;
      z-index: 220;
    }

    .modal {
      width: min(620px, 100%);
      background: #FFFFFF;
      border-radius: 14px;
      padding: 1rem;
      border: 1px solid #d8e9e4;
      box-shadow: 0 18px 36px rgba(15, 23, 42, 0.2);
    }

    .modal-subtitle {
      margin: 0.2rem 0 0.8rem;
      color: #5a6b64;
      font-size: 0.9rem;
    }

    .handover-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 0.75rem;
      margin-bottom: 0.75rem;
    }

    .handover-field {
      margin: 0;
      padding: 0;
    }

    .handover-full {
      grid-column: 1 / -1;
    }

    .handover-pin {
      grid-column: 1 / -1;
      max-width: 220px;
    }

    .count-error {
      margin: 0 0 0.6rem;
      color: #8f3b2f;
      background: #fff1ee;
      border: 1px solid #f5c3b8;
      border-radius: 8px;
      padding: 0.55rem 0.7rem;
      font-size: 0.85rem;
    }

    .count-list {
      border: 1px solid #e5ecea;
      border-radius: 10px;
      overflow: hidden;
      margin-bottom: 0.8rem;
    }

    .count-row {
      display: grid;
      grid-template-columns: 1fr 110px 120px;
      gap: 0.6rem;
      align-items: center;
      padding: 0.55rem 0.65rem;
      border-bottom: 1px solid #eef2f1;
    }

    .count-row:last-child {
      border-bottom: none;
    }

    .count-header {
      background: #f7faf9;
      color: #56706a;
      font-size: 0.77rem;
      font-weight: 700;
      text-transform: uppercase;
    }

    .count-product {
      color: #1f3f35;
      font-size: 0.9rem;
      font-weight: 600;
    }

    .count-system {
      color: #5d6d67;
      font-variant-numeric: tabular-nums;
      font-size: 0.86rem;
    }

    .count-row input {
      width: 100%;
      border: 1px solid #d7e1de;
      border-radius: 8px;
      padding: 0.45rem 0.5rem;
      color: #1B4D3E;
      font-size: 0.88rem;
      font-variant-numeric: tabular-nums;
    }

    .count-empty {
      margin: 0 0 0.8rem;
      color: #60716a;
      font-size: 0.88rem;
      background: #f8fbfa;
      border: 1px dashed #dce7e3;
      border-radius: 8px;
      padding: 0.6rem;
      text-align: center;
    }

    .count-notes {
      margin-bottom: 0.75rem;
      padding: 0;
    }

    .modal-actions {
      margin-top: 0.2rem;
      display: flex;
      gap: 0.7rem;
    }

    .modal-actions .btn-secondary,
    .modal-actions .btn-primary {
      flex: 1;
      min-height: 42px;
      border-radius: 10px;
      font-size: 0.9rem;
      font-weight: 700;
      cursor: pointer;
    }

    .modal-actions .btn-secondary {
      border: 1px solid #cfdbd7;
      background: #FFFFFF;
      color: #3e5a51;
    }

    .modal-actions .btn-primary {
      border: 1px solid #1b8f5e;
      background: linear-gradient(135deg, #20a36b 0%, #1b8f5e 100%);
      color: #FFFFFF;
    }

    .modal-actions .btn-primary:disabled,
    .modal-actions .btn-secondary:disabled {
      opacity: 0.55;
      cursor: not-allowed;
    }

    @media (max-width: 640px) {
      .hero {
        flex-direction: column;
        align-items: stretch;
      }

      .hero-actions {
        justify-content: stretch;
      }

      .hero-actions a {
        flex: 1;
        justify-content: center;
      }

      .form-grid {
        grid-template-columns: 1fr;
      }

      .session-audit {
        grid-template-columns: 1fr;
      }

      .actions-footer {
        flex-direction: column;
      }

      .btn-handover,
      .btn-close {
        width: 100%;
        flex: 1 1 auto;
      }

      .count-row {
        grid-template-columns: 1fr;
      }

      .count-header {
        display: none;
      }

      .modal-actions {
        flex-direction: column;
      }

      .handover-grid {
        grid-template-columns: 1fr;
      }

      .handover-pin {
        max-width: none;
      }
    }

    .confirm-modal {
      text-align: center;
    }

    .confirm-modal .modal-icon {
      margin-bottom: 1rem;
    }

    .confirm-modal .modal-icon.warning {
      color: #F59E0B;
    }

    .confirm-modal h3 {
      font-size: 1.25rem;
      color: #1B4D3E;
      margin: 0 0 0.5rem 0;
    }

    .confirm-modal p {
      color: #495057;
      margin: 0;
    }
  `]
})
export class PosCajaCierreComponent {
  cashSession: CashSessionResponse | null = null;
  handoverHistory: CashSessionHandoverResponse[] = [];
  declaredCash = 0;
  declaredCard = 0;
  declaredTransfer = 0;
  declaredCredit = 0;
  closeNotes = '';
  closeBlock: any = null;
  errorMessage = '';
  lastClosedSessionId: number | null = null;
  showHandoverModal = false;
  handoverReason = '';
  handoverUsername = '';
  handoverPassword = '';
  handoverPin = '';
  handoverError = '';
  showCigaretteCountModal = false;
  cigaretteCountRows: CigaretteCountDraftRow[] = [];
  cigaretteCountNotes = '';
  cigaretteCountError = '';
  cigaretteCountBusy = false;
  confirmDialog: { message: string; onConfirm: () => Promise<void> } | null = null;
  private pendingRequests = 0;

  get isBusy(): boolean { return this.pendingRequests > 0; }

  constructor(
    private readonly api: PosCajaService,
    private readonly operatorSessionService: OperatorSessionService,
    private readonly activityService: ActivityService,
    private readonly notifications: NotificationsService
  ) {
    void this.loadSession();
  }

  async loadSession(): Promise<void> {
    try {
      this.errorMessage = '';
      this.cashSession = await this.withBusy(() => this.api.getCurrentCashSession());
      this.handoverHistory = this.cashSession
        ? await this.withBusy(() => this.api.getCashSessionHandoverHistory(this.cashSession!.id))
        : [];
    } catch (err: any) {
      this.errorMessage = err?.error?.message ?? 'No se pudo obtener sesion de caja';
      this.cashSession = null;
      this.handoverHistory = [];
    }
  }

  async takeOverCashSession(): Promise<void> {
    if (!this.cashSession) return;

    this.resetHandoverForm(true);
    this.showHandoverModal = true;
  }

  cancelHandoverModal(): void {
    this.showHandoverModal = false;
    this.resetHandoverForm(true);
  }

  async submitTakeOverCashSession(): Promise<void> {
    if (!this.cashSession) return;

    const reason = this.handoverReason.trim();
    const username = this.handoverUsername.trim();
    const password = this.handoverPassword;
    const pin = this.handoverPin.trim();

    if (!reason) {
      this.handoverError = 'Ingresa el motivo del cambio de operador.';
      return;
    }

    if (!username || !password || !pin) {
      this.handoverError = 'Completa usuario, contrasena y PIN del nuevo operador.';
      return;
    }

    if (!/^\d{4,6}$/.test(pin)) {
      this.handoverError = 'El PIN del nuevo operador debe tener entre 4 y 6 numeros.';
      return;
    }

    const hasIdentity = await this.ensureActiveIdentity();
    if (!hasIdentity) return;

    try {
      this.errorMessage = '';
      this.handoverError = '';
      const response = await this.withBusy(() => this.api.handoverCashSessionWithAuth(this.cashSession!.id, {
        reason,
        newOperatorUsername: username,
        newOperatorPassword: password,
        newOperatorPin: pin
      }));

      if (this.cashSession) {
        this.cashSession.currentUsername = response.operatorSession.username;
        this.cashSession.currentUsuarioId = response.operatorSession.usuarioId;
      }

      this.operatorSessionService.setActiveSession(response.operatorSession);
      this.notifications.push('success', `Mando transferido a ${response.operatorSession.username}.`);
      this.showHandoverModal = false;
      this.resetHandoverForm(true);
      await this.loadSession();
    } catch (err: any) {
      const message = err?.error?.message ?? 'No se pudo transferir el mando de caja';
      this.handoverError = message;
      this.errorMessage = message;
    }
  }

  async closeSession(skipIdentityCheck = false): Promise<void> {
    if (!this.cashSession) return;

    if (!skipIdentityCheck) {
      const hasIdentity = await this.ensureActiveIdentity();
      if (!hasIdentity) {
        this.confirmDialog = {
          message: 'No se pudo verificar la identidad. Desea intentar de nuevo?',
          onConfirm: async () => await this.closeSession()
        };
        return;
      }
    }

    this.closeBlock = null;

    try {
      const closeResult = await this.withBusy(() => this.api.closeCashSession(this.cashSession!.id, {
        declaredCash: Number(this.declaredCash || 0),
        declaredCard: Number(this.declaredCard || 0),
        declaredTransfer: Number(this.declaredTransfer || 0),
        declaredCredit: Number(this.declaredCredit || 0),
        notes: this.closeNotes || null
      }));

      this.notifyNonBlockingCloseWarnings(closeResult);

      this.lastClosedSessionId = this.cashSession.id;
      this.cashSession = null;
      this.handoverHistory = [];
      this.errorMessage = '';
    } catch (err: any) {
      const payload = err?.error;
      if (payload?.blockedByCigarettesCount) {
        this.closeBlock = {
          ...payload,
          missingRequiredTasks: this.normalizeMissingTasks(payload?.missingRequiredTasks)
        };
        if (payload?.blockedByCigarettesCount) {
          await this.openCigaretteCountModal();
        }
      } else {
        this.errorMessage = payload?.message ?? 'No se pudo cerrar caja';
      }
    }
  }

  async openCigaretteCountModal(): Promise<void> {
    if (!this.cashSession) return;

    this.showCigaretteCountModal = true;
    this.cigaretteCountError = '';
    this.cigaretteCountBusy = true;
    try {
      const stock = await this.api.getStockCigarettes();
      this.cigaretteCountRows = stock
        .map((row: CigaretteStockBalance) => {
          const systemQty = Number(row.vendible ?? 0);
          const code = row.productCode ? ` (${row.productCode})` : '';
          return {
            productId: row.productId,
            productName: `${row.productName ?? `Producto #${row.productId}`}${code}`,
            systemQty,
            countedQty: systemQty
          };
        })
        .sort((a, b) => a.productName.localeCompare(b.productName));
    } catch (err: any) {
      this.cigaretteCountRows = [];
      this.cigaretteCountError = err?.error?.message ?? 'No se pudo cargar productos para conteo de cigarrillos';
    } finally {
      this.cigaretteCountBusy = false;
    }
  }

  cancelCigaretteCountModal(): void {
    this.showCigaretteCountModal = false;
    this.cigaretteCountError = '';
  }

  async submitCigaretteCount(): Promise<void> {
    if (!this.cashSession || this.cigaretteCountBusy) return;

    const invalid = this.cigaretteCountRows.find(row => !Number.isFinite(Number(row.countedQty)) || Number(row.countedQty) < 0);
    if (invalid) {
      this.cigaretteCountError = `Cantidad invalida para ${invalid.productName}.`;
      return;
    }

    this.cigaretteCountError = '';
    this.cigaretteCountBusy = true;
    try {
      await this.api.createCigaretteCount(this.cashSession.id, {
        notes: this.cigaretteCountNotes.trim() || undefined,
        lines: this.cigaretteCountRows.map(row => ({
          productId: row.productId,
          countedQty: Number(row.countedQty)
        }))
      });

      this.notifications.push('success', 'Conteo de cigarrillos guardado. Validando cierre de caja...');
      this.showCigaretteCountModal = false;
      this.closeBlock = null;
      await this.closeSession(true);
    } catch (err: any) {
      this.cigaretteCountError = err?.error?.message ?? 'No se pudo registrar el conteo de cigarrillos';
    } finally {
      this.cigaretteCountBusy = false;
    }
  }

  shiftLabel(shift: string): string {
    if (shift === 'Morning') return 'Manana';
    if (shift === 'Afternoon') return 'Tarde';
    if (shift === 'Night') return 'Noche';
    return shift;
  }

  closeTaskLabel(task: string): string {
    if (task === 'CigaretteCount') return 'Conteo de cigarrillos';
    if (task === 'PendingTransfers') return 'Transferencias pendientes';
    return task;
  }

  private async ensureActiveIdentity(): Promise<boolean> {
    const confirmed = await this.activityService.ensureRecentIdentity({
      idleSeconds: 60,
      confirmationMessage: 'Pasaron mas de 60s sin interaccion. Sos vos?',
      pinPrompt: () => this.operatorSessionService.requestPin()
    });

    if (!confirmed) {
      this.errorMessage = 'Accion cancelada por inactividad';
    }

    return confirmed;
  }

  async runConfirmAction(): Promise<void> {
    const action = this.confirmDialog?.onConfirm;
    this.confirmDialog = null;
    if (action) {
      await action();
    }
  }

  private async withBusy<T>(fn: () => Promise<T>): Promise<T> {
    this.pendingRequests += 1;
    try { return await fn(); } finally { this.pendingRequests -= 1; }
  }

  private notifyNonBlockingCloseWarnings(result: CashSessionCloseResponse): void {
    const tasks = this.normalizeMissingTasks(result?.pendingNonBlockingTasks).map(t => this.closeTaskLabel(t));
    if (tasks.length === 0) return;

    const preview = tasks.slice(0, 2).join(', ');
    const suffix = tasks.length > 2 ? ` y ${tasks.length - 2} mas` : '';
    this.notifications.push('warning', `Caja cerrada. Quedaron controles pendientes: ${preview}${suffix}.`);
  }

  private normalizeMissingTasks(tasks: unknown): string[] {
    const values = Array.isArray(tasks) ? tasks.filter((t): t is string => typeof t === 'string' && t.trim().length > 0) : [];
    return [...new Set(values)];
  }

  private resetHandoverForm(resetReason: boolean): void {
    if (resetReason) this.handoverReason = '';
    this.handoverUsername = '';
    this.handoverPassword = '';
    this.handoverPin = '';
    this.handoverError = '';
  }
}
