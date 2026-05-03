import { CommonModule } from '@angular/common';
import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ActivityService } from '../../core/services/activity.service';
import { OperatorSessionService } from '../../core/services/operator-session.service';
import { CartItem, CustomerRef, PosCajaService, ProductLookupResponse } from '../../core/services/pos-caja.service';
import { OperatingModeService } from '../../core/services/operating-mode.service';
import { PosModuleNavComponent } from '../../shared/components/pos-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-pos-tablet-carrito',
  imports: [CommonModule, FormsModule, PosModuleNavComponent],
  template: `
    <main class="carrito-container">
      <div class="hero-bg">
        <div class="hero-shape shape-1"></div>
        <div class="hero-shape shape-2"></div>
      </div>

      <app-pos-module-nav />

      <header class="hero">
        <div class="hero-content">
          <div class="hero-badge">
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="4" y="2" width="16" height="20" rx="2" ry="2"></rect>
              <line x1="12" y1="18" x2="12.01" y2="18"></line>
            </svg>
            <span>{{ headerLabel }}</span>
          </div>
          <h1>Carrito #{{ cartId }}</h1>
          <p class="hero-subtitle">{{ headerSubtitle }}</p>
        </div>
      </header>

      <section class="content-section">
        <div class="actions-bar">
          <button class="btn-icon" [disabled]="isBusy" (click)="reloadCart()" title="Recargar">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="23 4 23 10 17 10"></polyline>
              <path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"></path>
            </svg>
          </button>
          <button class="btn-primary" [disabled]="isBusy" (click)="sendToCashier()">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="22" y1="2" x2="11" y2="13"></line>
              <polygon points="22 2 15 22 11 13 2 9 22 2"></polygon>
            </svg>
            {{ isTotemMode ? 'Derivar a caja' : 'Enviar a caja' }}
          </button>
          <button class="btn-ghost" [disabled]="isBusy" (click)="goNueva()">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="18" y1="6" x2="6" y2="18"></line>
              <line x1="6" y1="6" x2="18" y2="18"></line>
            </svg>
            Cancelar
          </button>
        </div>

        <div class="alert warning">
          <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="12" y1="8" x2="12" y2="12"></line>
            <line x1="12" y1="16" x2="12.01" y2="16"></line>
          </svg>
          {{ isTotemMode ? 'Modo Totem: cobrá por QR, cuenta corriente o derivá el carrito a caja.' : 'Transferencias en tablet deshabilitadas. Podés cobrar QR, cuenta corriente o derivar a caja.' }}
        </div>

        <div class="alert success" *ngIf="message">
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
            <polyline points="22 4 12 14.01 9 11.01"></polyline>
          </svg>
          {{ message }}
        </div>

        <div class="alert error" *ngIf="error">
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="15" y1="9" x2="9" y2="15"></line>
            <line x1="9" y1="9" x2="15" y2="15"></line>
          </svg>
          {{ error }}
        </div>
        <button class="btn-secondary error-action" *ngIf="showOpenCashAction" (click)="goCashOpen()">
          Ir a apertura de caja
        </button>

        <div class="cards-row">
          <div class="card">
            <div class="card-header">
              <h2>
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M3 7V5a2 2 0 0 1 2-2h2"></path>
                  <path d="M17 3h2a2 2 0 0 1 2 2v2"></path>
                  <path d="M21 17v2a2 2 0 0 1-2 2h-2"></path>
                  <path d="M7 21H5a2 2 0 0 1-2-2v-2"></path>
                  <line x1="7" y1="12" x2="17" y2="12"></line>
                </svg>
                Scanner
              </h2>
            </div>
            <div class="card-body">
              <div class="scan-input-wrapper">
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M3 7V5a2 2 0 0 1 2-2h2"></path>
                  <path d="M17 3h2a2 2 0 0 1 2 2v2"></path>
                  <path d="M21 17v2a2 2 0 0 1-2 2h-2"></path>
                  <path d="M7 21H5a2 2 0 0 1-2-2v-2"></path>
                </svg>
                <input #scanInput type="text" placeholder="Escanear o buscar producto" [(ngModel)]="scanCode" (input)="onScanInputChange()" (keydown.enter)="onScanEnter($event)" (keydown.arrowdown)="moveSuggestion(1, $event)" (keydown.arrowup)="moveSuggestion(-1, $event)" (keydown.escape)="dismissSuggestions()" [disabled]="isBusy" />
                <select class="scan-mode" [(ngModel)]="addMode" [disabled]="isBusy">
                  <option value="quantity">Cantidad</option>
                  <option value="weight">Peso (kg)</option>
                </select>
                <input class="scan-qty" type="number" [(ngModel)]="manualValue" [disabled]="isBusy" [step]="addMode === 'weight' ? 0.01 : 1" min="0.01" />
                <button class="btn-add" [disabled]="isBusy" (click)="addFromScan()">
                  <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="12" y1="5" x2="12" y2="19"></line>
                    <line x1="5" y1="12" x2="19" y2="12"></line>
                  </svg>
                  Agregar
                </button>
              </div>
              <div class="scan-suggestions" *ngIf="showSuggestions && filteredProducts.length > 0">
                <button type="button" class="suggestion-item" *ngFor="let p of filteredProducts; let i = index" [class.active]="i === suggestionIndex" (click)="selectSuggestedProduct(p)" [disabled]="isBusy">
                  <span>{{ p.name }}</span>
                  <small>{{ productCodesLabel(p) || ('PID:' + p.id) }}</small>
                </button>
              </div>

              <div class="items-list" *ngIf="items.length > 0">
                <div class="list-header">
                  <span>Producto</span>
                  <span>Subtotal</span>
                  <span></span>
                </div>
                <div *ngFor="let item of items" class="list-row">
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

              <div class="empty-state" *ngIf="items.length === 0 && !isBusy">
                <svg xmlns="http://www.w3.org/2000/svg" width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <circle cx="9" cy="21" r="1"></circle>
                  <circle cx="20" cy="21" r="1"></circle>
                  <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path>
                </svg>
                <p>No hay items en el carrito</p>
              </div>
            </div>
          </div>

          <div class="card">
            <div class="card-header">
              <h2>
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="3" y="3" width="18" height="18" rx="2"></rect>
                  <rect x="7" y="7" width="3" height="9"></rect>
                  <rect x="14" y="7" width="3" height="5"></rect>
                </svg>
                Cobro Tablet
              </h2>
            </div>
            <div class="card-body">
              <div class="payment-tabs">
                <button type="button" class="tab-btn" [class.active]="paymentMethod === 'Qr'" [disabled]="isBusy" (click)="setPaymentMethod('Qr')">QR</button>
                <button type="button" class="tab-btn" [class.active]="paymentMethod === 'AccountCredit'" [disabled]="isBusy || !canUseAccountCredit" (click)="setPaymentMethod('AccountCredit')">Cuenta corriente</button>
              </div>

              <div *ngIf="paymentMethod === 'Qr'">
                <div class="form-row">
                  <div class="form-group">
                    <label>Monto</label>
                    <input type="number" [(ngModel)]="qrAmount" [disabled]="isBusy" placeholder="0.00" />
                  </div>
                  <div class="form-group">
                    <label>Referencia QR</label>
                    <input type="text" [(ngModel)]="qrReference" [disabled]="isBusy" placeholder="Numero de operacion" />
                  </div>
                </div>

                <button class="btn-primary payment-cta qr-cta" [disabled]="!canChargeQr()" (click)="chargeQr()">
                  <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="3" y="3" width="18" height="18" rx="2"></rect>
                    <rect x="7" y="7" width="3" height="9"></rect>
                    <rect x="14" y="7" width="3" height="5"></rect>
                  </svg>
                  Cobrar QR
                </button>
              </div>

              <div *ngIf="paymentMethod === 'AccountCredit'">
                <div class="form-group">
                  <label>Cliente de cuenta corriente</label>
                  <select [(ngModel)]="selectedCreditCustomerId" [disabled]="isBusy">
                    <option [ngValue]="null">Selecciona o crea cliente ocasional</option>
                    <option *ngFor="let c of customers" [ngValue]="c.id">{{ c.fullName }}</option>
                  </select>
                  <p class="credit-help">Si no está registrado, podés crear cliente ocasional al momento de cobrar.</p>
                  <p class="credit-help warn">Tope ocasional por operación: {{ OCCASIONAL_CREDIT_MAX | number:'1.0-0' }}</p>
                  <div class="credit-status" *ngIf="selectedCreditCustomerId && selectedCreditCustomer">
                    <span class="status-chip" [class.critical]="selectedCreditCustomer.effectiveStatus === 'Critical'">{{ customerStatusLabel(selectedCreditCustomer) }}</span>
                    <span class="usage-chip" [class.blocked]="selectedCreditCustomer.isCreditBlocked">Uso {{ selectedCreditCustomer.creditUsedPct || 0 }}%</span>
                  </div>
                  <p class="credit-help warn" *ngIf="selectedCreditCustomer?.isCritical">Alerta: cliente cercano al límite de crédito.</p>
                  <p class="credit-help danger" *ngIf="selectedCreditCustomer?.isCreditBlocked">Cuenta corriente bloqueada: no se permiten nuevas ventas a crédito.</p>
                  <button class="btn-mini" type="button" [disabled]="isBusy" (click)="toggleOccasionalCreditForm()">{{ occasionalCreditFormOpen ? 'Ocultar cliente ocasional' : 'Crear cliente ocasional' }}</button>
                  <div class="form-row" *ngIf="occasionalCreditFormOpen">
                    <div class="form-group">
                      <label>Nombre y apellido</label>
                      <input type="text" [(ngModel)]="occasionalCreditName" [disabled]="isBusy" placeholder="Ej: Maria Gomez" />
                    </div>
                    <div class="form-group">
                      <label>Telefono (opcional)</label>
                      <input type="text" [(ngModel)]="occasionalCreditPhone" [disabled]="isBusy" placeholder="Ej: 11 5555 0000" />
                    </div>
                  </div>
                </div>

                <div class="form-row" *ngIf="canUseAccountCredit">
                  <div class="form-group">
                    <label>Monto cuenta corriente</label>
                    <input type="number" [ngModel]="creditChargeAmount" disabled />
                    <p class="credit-help" *ngIf="selectedCreditCustomerId">Deuda proyectada: {{ projectedDebt | number:'1.0-2' }} / Tope 110%: {{ creditToleranceLimit | number:'1.0-2' }}</p>
                    <p class="credit-help danger" *ngIf="selectedCreditCustomerId && isProjectedCreditBlocked()">No se puede cargar a cuenta corriente: supera el 110% del límite. Usá QR o derivá a caja.</p>
                  </div>
                </div>

                <button class="btn-secondary payment-cta credit-cta" *ngIf="canUseAccountCredit" [disabled]="!canChargeAccountCredit()" (click)="chargeAccountCredit()">
                  Cargar a cuenta corriente
                </button>
              </div>
              
              <button class="btn-secondary" [disabled]="isBusy" (click)="goNueva()">
                Nuevo carrito
              </button>

            <div class="qr-pending" *ngIf="qrPendingSaleId">
              <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="spin">
                <line x1="12" y1="2" x2="12" y2="6"></line>
                <line x1="12" y1="18" x2="12" y2="22"></line>
                <line x1="4.93" y1="4.93" x2="7.76" y2="7.76"></line>
                <line x1="16.24" y1="16.24" x2="19.07" y2="19.07"></line>
                <line x1="2" y1="12" x2="6" y2="12"></line>
                <line x1="18" y1="12" x2="22" y2="12"></line>
                <line x1="4.93" y1="19.07" x2="7.76" y2="16.24"></line>
                <line x1="16.24" y1="7.76" x2="19.07" y2="4.93"></line>
              </svg>
              Esperando confirmacion QR para venta #{{ qrPendingSaleId }}...
            </div>
          </div>
        </div>
      </div>
    </section>

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

    <div class="modal-overlay" *ngIf="quickCreateModal">
      <div class="modal">
        <div class="modal-header">
          <h3>Producto no registrado</h3>
        </div>
        <p class="modal-subtitle">Codigo: {{ quickCreateModal!.scannedCode }}</p>

        <div class="form-group">
          <label>Nombre</label>
          <input type="text" [(ngModel)]="quickCreateName" [disabled]="isBusy" placeholder="Nombre basico" />
        </div>

        <div class="form-group">
          <label>Precio</label>
          <input type="number" [(ngModel)]="quickCreatePrice" [disabled]="isBusy" placeholder="0.00" />
        </div>

        <p class="modal-subtitle">Se creará como pendiente para completar en Administración.</p>

        <div class="modal-actions">
          <button class="btn-secondary" [disabled]="isBusy" (click)="quickCreateModal = null">Cancelar</button>
          <button class="btn-primary" [disabled]="isBusy" (click)="confirmQuickCreateProduct()">Crear y agregar</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .carrito-container {
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
      max-width: 100%;
      margin: 0 auto;
    }

    .hero-content { animation: fadeInUp 0.5s ease-out; }

    @keyframes fadeInUp {
      from { opacity: 0; transform: translateY(20px); }
      to { opacity: 1; transform: translateY(0); }
    }

    .hero-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.4rem;
      background: rgba(191, 235, 241, 0.15);
      border: 1px solid rgba(191, 235, 241, 0.3);
      padding: 0.3rem 0.6rem;
      border-radius: 16px;
      color: #BFEBF1;
      font-size: 0.75rem;
      font-weight: 500;
      margin-bottom: 0.75rem;
    }

    .hero h1 {
      font-size: 1.5rem;
      font-weight: 700;
      color: #FFFFFF;
      margin: 0 0 0.3rem 0;
    }

    .hero-subtitle {
      font-size: 0.9rem;
      color: rgba(255, 255, 255, 0.7);
      margin: 0;
    }

    .content-section {
      position: relative;
      z-index: 1;
      max-width: 100%;
      margin: 0 auto;
      padding: 0 1rem;
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .actions-bar {
      display: flex;
      gap: 0.75rem;
      align-items: center;
      animation: fadeInUp 0.5s ease-out 0.1s backwards;
    }

    .btn-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 44px;
      height: 44px;
      background: #FFFFFF;
      color: #1B4D3E;
      border: 1px solid #e9ecef;
      border-radius: 10px;
      cursor: pointer;
    }

    .btn-icon:disabled {
      opacity: 0.5;
      cursor: not-allowed;
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
    }

    .btn-primary:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .btn-secondary {
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

    .payment-cta {
      border: 1px solid transparent;
    }

    .qr-cta {
      background: #bfebf1;
      color: #1b4d3e;
      border-color: #a8dbe3;
    }

    .credit-cta {
      background: #e3f4ec;
      color: #1b4d3e;
      border-color: #b8d9cb;
      font-weight: 600;
    }

    .credit-cta:hover:not(:disabled) {
      background: #d6ecdf;
    }

    .btn-ghost {
      display: flex;
      align-items: center;
      gap: 0.4rem;
      padding: 0.7rem 1rem;
      background: transparent;
      color: #6c757d;
      border: 1px dashed #e9ecef;
      border-radius: 10px;
      font-size: 0.9rem;
      font-weight: 500;
      cursor: pointer;
    }

    .alert {
      display: flex;
      align-items: center;
      gap: 0.6rem;
      padding: 0.75rem 1rem;
      border-radius: 10px;
      font-size: 0.85rem;
      font-weight: 500;
    }

    .alert.warning {
      background: #fff3cd;
      color: #856404;
    }

    .alert.success {
      background: #d4edda;
      color: #155724;
    }

    .alert.error {
      background: #f8d7da;
      color: #721c24;
    }

    .error-action {
      width: fit-content;
      margin-top: 0.4rem;
    }

    .cards-row {
      display: grid;
      grid-template-columns: 1.2fr 0.8fr;
      gap: 1.5rem;
      padding: 0 1rem;
    }

    .card {
      background: #FFFFFF;
      border-radius: 16px;
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
      overflow: hidden;
    }

    .card-header {
      padding: 1rem 1.25rem;
      border-bottom: 1px solid #e9ecef;
    }

    .card-header h2 {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 1rem;
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

    .card-body {
      padding: 1.25rem;
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

    .scan-input-wrapper input:focus { outline: none; }
    .scan-input-wrapper input::placeholder { color: #9EABB1; }

    .scan-mode,
    .scan-qty {
      border: 1px solid #d7dfe4;
      border-radius: 8px;
      background: #fff;
      color: #1B4D3E;
      font-size: 0.86rem;
      padding: 0.45rem 0.5rem;
    }

    .scan-mode {
      min-width: 126px;
      flex: 0 0 126px;
    }

    .scan-qty {
      width: 72px;
      min-width: 72px;
      flex: 0 0 72px;
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
      gap: 0.75rem;
      border: 0;
      background: #fff;
      padding: 0.6rem 0.7rem;
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
      padding: 0.6rem 0.85rem;
      background: #1B4D3E;
      color: #FFFFFF;
      border: none;
      border-radius: 8px;
      font-size: 0.85rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s ease;
      white-space: nowrap;
    }

    .btn-add:hover:not(:disabled) { background: #234F45; }
    .btn-add:disabled { opacity: 0.5; cursor: not-allowed; }

    .items-list {
      margin-top: 1rem;
    }

    .list-header {
      display: grid;
      grid-template-columns: 1fr 100px 50px;
      gap: 0.5rem;
      padding: 0.5rem 0;
      font-weight: 600;
      font-size: 0.75rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: #6c757d;
      border-bottom: 1px solid #e9ecef;
    }

    .list-row {
      display: grid;
      grid-template-columns: 1fr 100px 50px;
      gap: 0.5rem;
      padding: 0.75rem 0;
      align-items: center;
      border-bottom: 1px solid #f1f3f5;
    }

    .item-info {
      display: flex;
      flex-direction: column;
      gap: 0.15rem;
    }

    .item-info strong {
      color: #1B4D3E;
      font-size: 0.9rem;
    }

    .item-info span {
      color: #6c757d;
      font-size: 0.8rem;
    }

    .item-subtotal strong {
      font-size: 0.95rem;
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

    .btn-remove:hover:not(:disabled) { background: #f5c6cb; }
    .btn-remove:disabled { opacity: 0.5; cursor: not-allowed; }

    .empty-state {
      padding: 2rem;
      text-align: center;
      color: #9EABB1;
    }

    .empty-state svg { margin-bottom: 0.75rem; opacity: 0.5; }
    .empty-state p { margin: 0; font-size: 0.9rem; }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
      margin-bottom: 1rem;
    }

    .form-group label {
      display: block;
      font-size: 0.8rem;
      font-weight: 500;
      color: #495057;
      margin-bottom: 0.35rem;
    }

    .form-group input {
      width: 100%;
      padding: 0.65rem 0.85rem;
      font-size: 0.95rem;
      border: 2px solid #e9ecef;
      border-radius: 10px;
      background: #f8fafc;
      color: #1B4D3E;
      transition: all 0.2s ease;
    }

    .form-group select {
      width: 100%;
      padding: 0.65rem 0.85rem;
      font-size: 0.95rem;
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

    .form-group select:focus {
      outline: none;
      border-color: #BFEBF1;
      box-shadow: 0 0 0 4px rgba(191, 235, 241, 0.2);
    }

    .payment-tabs { display: grid; grid-template-columns: 1fr 1fr; gap: 0.5rem; margin-bottom: 0.7rem; }
    .tab-btn { border: 1px solid #d5e5df; border-radius: 10px; background: #f4f8f6; color: #355b4f; font-size: 0.84rem; font-weight: 700; padding: 0.5rem 0.65rem; cursor: pointer; }
    .tab-btn.active { border-color: #1b4d3e; background: #e2f4ec; color: #1b4d3e; }
    .tab-btn:disabled { opacity: 0.55; cursor: not-allowed; }

    .credit-status {
      display: flex;
      gap: 0.45rem;
      flex-wrap: wrap;
      margin-top: 0.5rem;
    }

    .status-chip,
    .usage-chip {
      display: inline-flex;
      padding: 0.2rem 0.5rem;
      border-radius: 999px;
      font-size: 0.74rem;
      font-weight: 700;
      background: #e8f1ed;
      color: #315f4f;
    }

    .status-chip.critical {
      background: #fff0dc;
      color: #8a5a00;
    }

    .usage-chip.blocked {
      background: #fde9ea;
      color: #9f1f1f;
    }

    .credit-help {
      margin: 0.35rem 0 0;
      font-size: 0.78rem;
    }

    .credit-help.warn {
      color: #8a5a00;
    }

    .credit-help.danger {
      color: #9f1f1f;
    }

    .btn-mini {
      margin-top: 0.5rem;
      border: 1px solid #c0ddd3;
      background: #ecf8f2;
      color: #1b4d3e;
      border-radius: 999px;
      padding: 0.35rem 0.7rem;
      font-size: 0.78rem;
      font-weight: 700;
      cursor: pointer;
    }

    .btn-mini:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .card-body .btn-primary,
    .card-body .btn-secondary {
      width: 100%;
      margin-bottom: 0.75rem;
    }

    .qr-pending {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding: 0.75rem;
      background: #e7f5ff;
      color: #0066cc;
      border-radius: 8px;
      font-size: 0.85rem;
      font-weight: 500;
      margin-top: 0.5rem;
    }

    .spin {
      animation: spin 1.5s linear infinite;
    }

    @keyframes spin {
      from { transform: rotate(0deg); }
      to { transform: rotate(360deg); }
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
      max-width: 380px;
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

    .modal-actions {
      display: flex;
      gap: 0.75rem;
      margin-top: 1.5rem;
    }

    .modal-actions .btn-secondary {
      flex: 1;
      justify-content: center;
    }

    .modal-actions .btn-primary {
      flex: 1;
      justify-content: center;
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

    @media (max-width: 1024px) {
      .cards-row {
        grid-template-columns: 1fr;
      }
    }

    @media (max-width: 640px) {
      .actions-bar {
        flex-wrap: wrap;
      }

      .scan-input-wrapper {
        flex-wrap: wrap;
      }

      .scan-input-wrapper input {
        width: 100%;
        flex: none;
      }

      .scan-mode,
      .scan-qty,
      .btn-add {
        width: 100%;
      }

      .actions-bar .btn-primary,
      .actions-bar .btn-ghost {
        flex: 1;
        justify-content: center;
      }

      .list-header { display: none; }
      .list-row { grid-template-columns: 1fr; gap: 0.5rem; }

      .form-row {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class PosTabletCarritoComponent implements AfterViewInit, OnDestroy {
  readonly OCCASIONAL_CREDIT_MAX = 25000;
  readonly CREDIT_TOLERANCE_FACTOR = 1.1;

  @ViewChild('scanInput') scanInput?: ElementRef<HTMLInputElement>;

  cartId = 0;
  items: CartItem[] = [];
  scanCode = '';
  qrAmount = 0;
  cartTotal = 0;
  qrReference = '';
  qrPendingSaleId: number | null = null;
  message = '';
  error = '';
  showOpenCashAction = false;
  queueWithoutCashSession = false;
  isTotemMode = false;
  canUseAccountCredit = false;
  headerLabel = 'Totem';
  headerSubtitle = 'Agrega productos y envia a caja';
  paymentMethod: 'Qr' | 'AccountCredit' = 'Qr';

  customers: CustomerRef[] = [];
  selectedCreditCustomerId: number | null = null;
  occasionalCreditFormOpen = false;
  occasionalCreditName = '';
  occasionalCreditPhone = '';

  confirmDialog: { message: string; onConfirm: () => Promise<void> } | null = null;

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
  quickCreateModal: { scannedCode: string } | null = null;
  quickCreateName = '';
  quickCreatePrice = 0;

  private pendingRequests = 0;
  private qrPollTimer?: ReturnType<typeof setInterval>;

  get isBusy(): boolean {
    return this.pendingRequests > 0;
  }

  get selectedCreditCustomer(): CustomerRef | null {
    if (!this.selectedCreditCustomerId) return null;
    return this.customers.find(c => c.id === this.selectedCreditCustomerId) ?? null;
  }

  canChargeAccountCredit(): boolean {
    if (!this.canUseAccountCredit) return false;
    const amount = this.creditChargeAmount;
    if (this.isBusy || amount <= 0 || amount > this.OCCASIONAL_CREDIT_MAX && !this.selectedCreditCustomerId) return false;
    if (this.selectedCreditCustomerId && this.isProjectedCreditBlocked()) return false;
    if (!this.selectedCreditCustomerId) return this.occasionalCreditName.trim().length >= 3;
    return !this.selectedCreditCustomer?.isCreditBlocked;
  }

  get creditChargeAmount(): number {
    return Math.max(0, Number(this.cartTotal || 0));
  }

  get projectedDebt(): number {
    const currentDebt = Number(this.selectedCreditCustomer?.currentDebt || 0);
    return currentDebt + this.creditChargeAmount;
  }

  get creditToleranceLimit(): number {
    const limit = Number(this.selectedCreditCustomer?.creditLimit || 0);
    return limit > 0 ? limit * this.CREDIT_TOLERANCE_FACTOR : 0;
  }

  isProjectedCreditBlocked(): boolean {
    if (!this.selectedCreditCustomerId) return false;
    if (this.selectedCreditCustomer?.isCreditBlocked) return true;
    const tolerance = this.creditToleranceLimit;
    if (tolerance <= 0) return true;
    return this.projectedDebt > tolerance;
  }

  canChargeQr(): boolean {
    return !this.isBusy && this.qrAmount > 0;
  }

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly api: PosCajaService,
    private readonly activityService: ActivityService,
    private readonly operatorSessionService: OperatorSessionService,
    operatingMode: OperatingModeService
  ) {
    const cfg = operatingMode.getConfig();
    this.isTotemMode = cfg.mode === 'TotemQrOnly';
    this.canUseAccountCredit = !!cfg.modules?.cuentaCorriente;
    if (this.isTotemMode) {
      this.headerLabel = 'Totem';
      this.headerSubtitle = 'Escanea articulos y cobra por QR o cuenta corriente';
    }
  }

  async ngAfterViewInit(): Promise<void> {
    const id = Number(this.route.snapshot.paramMap.get('cartId'));
    if (!id) {
      void this.router.navigateByUrl('/pos/tablet/nueva');
      return;
    }

    this.cartId = id;
    await this.reloadCart();
    this.focusScan();
  }

  ngOnDestroy(): void {
    if (this.qrPollTimer) clearInterval(this.qrPollTimer);
  }

  focusScan(): void {
    queueMicrotask(() => {
      if (!this.shouldAutoFocusScan()) return;
      this.scanInput?.nativeElement.focus();
    });
  }

  private shouldAutoFocusScan(): boolean {
    const active = typeof document !== 'undefined' ? document.activeElement as HTMLElement | null : null;
    const scan = this.scanInput?.nativeElement;
    if (!scan) return false;
    if (!active || active === document.body || active === scan) return true;

    const tag = (active.tagName || '').toLowerCase();
    const isEditable = active.isContentEditable || tag === 'input' || tag === 'select' || tag === 'textarea' || tag === 'button';
    return !isEditable;
  }

  async reloadCart(): Promise<void> {
    this.clearMessages();
    try {
      const cart = await this.withBusy(() => this.api.getCart(this.cartId));
      this.items = cart.items;
      this.cartTotal = this.items.reduce((sum, item) => sum + Number(item.subtotal || 0), 0);
      await this.ensureProductCatalog();
      if (this.qrAmount <= 0) {
        this.qrAmount = Number(this.cartTotal.toFixed(2));
      }

      if (this.canUseAccountCredit && this.customers.length === 0) {
        this.customers = await this.withBusy(() => this.api.getCustomers());
      }
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo cargar carrito';
    }
  }

  async addFromScan(): Promise<void> {
    if (!this.scanCode.trim()) return;
    this.clearMessages();

    try {
      const product = await this.resolveInputProduct();
      if (!product) {
        this.error = 'No se encontro el producto. Escanea codigo o buscá por nombre.';
        return;
      }

      const quantity = Number(this.manualValue);
      if (!Number.isFinite(quantity) || quantity <= 0) {
        this.error = 'Ingresa una cantidad o peso valido mayor a 0.';
        return;
      }

      const isWeight = this.isWeightProduct(product);
      if (this.addMode === 'weight' && !isWeight) {
        this.error = 'El producto seleccionado no se vende por peso. Cambia el modo a Cantidad.';
        return;
      }
      if (this.addMode === 'quantity' && isWeight) {
        this.error = 'El producto seleccionado es pesable. Cambia el modo a Peso (kg).';
        return;
      }

      const unitPrice = this.addMode === 'weight'
        ? Number(product.defaultPricePerKg ?? product.defaultPrice ?? 0)
        : Number(product.defaultPrice ?? product.defaultPricePerKg ?? 0);

      if (unitPrice <= 0) {
        this.error = 'El producto no tiene precio configurado.';
        return;
      }

      await this.withBusy(() =>
        this.api.addCartItem(this.cartId, {
          productId: product.id,
          productCode: this.resolveProductCode(product),
          productName: product.name,
          unitPrice,
          quantity,
          unit: this.addMode === 'weight' ? 'Weight' : 'Unit',
          discount: 0
        })
      );

      await this.reloadCart();
      this.scanCode = '';
      this.selectedProduct = null;
      this.filteredProducts = [];
      this.showSuggestions = false;
      this.suggestionIndex = -1;
      this.addMode = 'quantity';
      this.manualValue = 1;
      this.focusScan();
    } catch (err: any) {
      if (err?.status === 404) {
        this.quickCreateModal = { scannedCode: this.scanCode.trim() };
        this.quickCreateName = '';
        this.quickCreatePrice = 0;
        return;
      }
      this.error = err?.error?.message ?? 'No se pudo agregar item';
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
    void this.addFromScan();
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

  async confirmWeightItem(): Promise<void> {
    if (!this.weightModal || this.weightKg <= 0 || this.weightPricePerKg <= 0) return;
    this.clearMessages();

    try {
      const pricePerKg = this.weightModal.product.allowsManualPrice
        ? Number(this.weightPricePerKg)
        : Number(this.weightModal.product.defaultPricePerKg ?? this.weightModal.product.defaultPrice ?? this.weightPricePerKg);

      await this.withBusy(() =>
        this.api.addCartItem(this.cartId, {
          productId: this.weightModal!.product.id,
          productCode: this.weightModal!.scannedCode,
          productName: this.weightModal!.product.name,
          unitPrice: pricePerKg,
          quantity: Number(this.weightKg),
          unit: 'Weight',
          discount: 0
        })
      );

      await this.reloadCart();
      this.scanCode = '';
      this.closeWeightModal();
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo agregar pesable';
    }
  }

  closeWeightModal(): void {
    this.weightModal = null;
    this.focusScan();
  }

  async removeItem(item: CartItem): Promise<void> {
    this.clearMessages();
    try {
      await this.withBusy(() => this.api.removeCartItem(this.cartId, item.id));
      await this.reloadCart();
      this.focusScan();
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo quitar item';
    }
  }

  async runConfirmAction(): Promise<void> {
    const action = this.confirmDialog?.onConfirm;
    this.confirmDialog = null;
    if (action) {
      await action();
    }
  }

  async sendToCashier(): Promise<void> {
    const hasIdentity = await this.ensureActiveIdentity();
    if (!hasIdentity) {
      this.confirmDialog = {
        message: 'No se pudo verificar la identidad. Desea intentar de nuevo?',
        onConfirm: async () => await this.sendToCashier()
      };
      return;
    }
    this.clearMessages();

    try {
      await this.ensureOperatorSession();
      await this.withBusy(() => this.api.sendCartToCashier(this.cartId));
      this.message = this.isTotemMode
        ? `Carrito #${this.cartId} derivado a caja. Continuá con una nueva venta.`
        : `Carrito #${this.cartId} enviado a caja`;
      setTimeout(() => this.goNueva(), 450);
    } catch (err: any) {
      if (this.isSessionReauthRequired(err)) return;
      this.error = err?.error?.message ?? err?.message ?? 'No se pudo enviar a caja';
    }
  }

  async chargeQr(): Promise<void> {
    if (this.qrAmount <= 0) return;

    this.clearMessages();
    const cashReady = await this.ensureCashSessionReady();
    if (!cashReady) return;

    const hasIdentity = await this.ensureActiveIdentity();
    if (!hasIdentity) {
      this.confirmDialog = {
        message: 'No se pudo verificar la identidad. Desea intentar de nuevo?',
        onConfirm: async () => await this.chargeQr()
      };
      return;
    }

    try {
      await this.ensureOperatorSession();
      const sale = await this.withBusy(() =>
        this.api.createSaleFromCart(this.cartId, {
          customerId: undefined,
          discount: 0,
          payments: [
            {
              paymentMethod: 'QrMp',
              amount: Number(this.qrAmount),
              reference: this.qrReference || undefined,
              isPending: true
            }
          ]
        })
      );

      const qr = sale.payments?.find(p => p.paymentMethod === 'QrMp');
      if (qr?.status === 'Pending' || sale.status === 'Pending') {
        this.qrPendingSaleId = sale.id;
        this.message = `Esperando confirmacion QR para venta #${sale.id}`;
        this.startQrPolling();
      } else {
        this.message = 'Cobro QR confirmado';
      }
    } catch (err: any) {
      if (this.isSessionReauthRequired(err)) return;
      if (this.tryOfferDeriveToCashier(err)) return;
      this.error = err?.error?.message ?? 'No se pudo cobrar QR';
    }
  }

  async chargeAccountCredit(): Promise<void> {
    if (!this.canUseAccountCredit) {
      this.error = 'La cuenta corriente no está habilitada para este modo operativo.';
      return;
    }

    const creditAmount = this.creditChargeAmount;

    if (creditAmount <= 0) {
      this.error = 'Ingresa un monto valido para cuenta corriente';
      return;
    }

    if (!this.selectedCreditCustomerId && creditAmount > this.OCCASIONAL_CREDIT_MAX) {
      this.error = `El monto máximo para cliente ocasional es ${this.OCCASIONAL_CREDIT_MAX}.`;
      return;
    }

    if (this.selectedCreditCustomerId && this.isProjectedCreditBlocked()) {
      this.error = 'No se puede cargar a cuenta corriente: supera el 110% del límite. Usá QR o derivá a caja.';
      return;
    }

    if (this.selectedCreditCustomer?.isCreditBlocked) {
      this.error = 'La cuenta corriente de este cliente está bloqueada por límite o estado.';
      return;
    }

    this.clearMessages();
    const cashReady = await this.ensureCashSessionReady();
    if (!cashReady) return;

    const hasIdentity = await this.ensureActiveIdentity();
    if (!hasIdentity) {
      this.confirmDialog = {
        message: 'No se pudo verificar la identidad. Desea intentar de nuevo?',
        onConfirm: async () => await this.chargeAccountCredit()
      };
      return;
    }

    try {
      let creditCustomerId = this.selectedCreditCustomerId;
      if (!creditCustomerId) {
        if (!this.occasionalCreditName.trim()) {
          this.occasionalCreditFormOpen = true;
          this.error = 'Completá nombre para crear cliente ocasional o seleccioná uno existente.';
          return;
        }

        const created = await this.withBusy(() =>
          this.api.createOccasionalCreditCustomer({
            fullName: this.occasionalCreditName.trim(),
            phone: this.occasionalCreditPhone.trim() || undefined,
            creditLimit: Math.min(creditAmount, this.OCCASIONAL_CREDIT_MAX)
          })
        );
        this.customers = [created, ...this.customers.filter(c => c.id !== created.id)];
        this.selectedCreditCustomerId = created.id;
        creditCustomerId = created.id;
        this.occasionalCreditFormOpen = false;
      }

      await this.ensureOperatorSession();
      await this.withBusy(() =>
        this.api.createSaleFromCart(this.cartId, {
          customerId: creditCustomerId,
          accountCreditCustomerId: creditCustomerId,
          discount: 0,
          payments: [
            {
              paymentMethod: 'AccountCredit',
              amount: creditAmount,
              isPending: false
            }
          ]
        })
      );

      this.message = 'Venta cargada a cuenta corriente';
      this.occasionalCreditName = '';
      this.occasionalCreditPhone = '';
      this.goNueva();
    } catch (err: any) {
      if (this.isSessionReauthRequired(err)) return;
      if (this.tryOfferDeriveToCashier(err)) return;
      this.error = err?.error?.message ?? 'No se pudo cargar venta a cuenta corriente';
    }
  }

  private tryOfferDeriveToCashier(err: any): boolean {
    const message = `${err?.error?.message ?? err?.message ?? ''}`.toLowerCase();
    if (!this.isTotemMode || !message.includes('primero debes enviar el carrito a caja')) {
      return false;
    }

    this.confirmDialog = {
      message: 'Este carrito debe derivarse a caja antes de cobrar. ¿Querés derivarlo ahora?',
      onConfirm: async () => await this.sendToCashier()
    };

    return true;
  }

  async confirmQuickCreateProduct(): Promise<void> {
    if (!this.quickCreateModal) return;
    if (!this.quickCreateName.trim()) {
      this.error = 'Ingresa nombre del producto';
      return;
    }
    if (this.quickCreatePrice <= 0) {
      this.error = 'Ingresa precio valido';
      return;
    }

    this.clearMessages();
    try {
      const scannedCode = this.quickCreateModal.scannedCode;
      const created = await this.withBusy(() =>
        this.api.createProduct({
          name: this.quickCreateName.trim(),
          barcode: scannedCode,
          quickCode: scannedCode.length <= 32 ? scannedCode : undefined,
          saleType: 'Unit',
          allowsManualPrice: true,
          stockControl: true,
          defaultPrice: Number(this.quickCreatePrice),
          defaultPricePerKg: 0
        })
      );

      await this.withBusy(() =>
        this.api.addCartItem(this.cartId, {
          productId: created.id,
          productCode: scannedCode,
          productName: created.name,
          unitPrice: Number(created.defaultPrice || this.quickCreatePrice),
          quantity: 1,
          unit: 'Unit',
          discount: 0
        })
      );

      await this.reloadCart();
      this.scanCode = '';
      this.quickCreateModal = null;
      this.quickCreateName = '';
      this.quickCreatePrice = 0;
      this.message = 'Producto basico creado en estado pendiente';
      this.focusScan();
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo crear producto rapido';
    }
  }

  goNueva(): void {
    this.items = [];
    this.scanCode = '';
    this.qrAmount = 0;
    this.qrReference = '';
    this.qrPendingSaleId = null;
    this.occasionalCreditName = '';
    this.occasionalCreditPhone = '';
    this.occasionalCreditFormOpen = false;
    this.weightModal = null;
    if (this.qrPollTimer) clearInterval(this.qrPollTimer);
    void this.router.navigateByUrl('/pos/tablet/nueva');
  }

  private startQrPolling(): void {
    if (!this.qrPendingSaleId) return;
    if (this.qrPollTimer) clearInterval(this.qrPollTimer);

    this.qrPollTimer = setInterval(() => {
      void this.pollQrStatus();
    }, 3000);
  }

  private async pollQrStatus(): Promise<void> {
    if (!this.qrPendingSaleId) return;
    try {
      const sale = await this.api.getSaleById(this.qrPendingSaleId);
      const qr = sale.payments?.find(p => p.paymentMethod === 'QrMp');
      if (sale.status === 'Paid' || qr?.status === 'Confirmed') {
        this.message = `QR confirmado para venta #${sale.id}`;
        this.qrPendingSaleId = null;
        if (this.qrPollTimer) clearInterval(this.qrPollTimer);
      }
      if (qr?.status === 'Rejected') {
        this.error = `QR rechazado para venta #${sale.id}`;
        this.qrPendingSaleId = null;
        if (this.qrPollTimer) clearInterval(this.qrPollTimer);
      }
    } catch {
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

  private async ensureOperatorSession(): Promise<void> {
    const token = this.operatorSessionService.getSessionToken();
    if (token) {
      const pin = await this.operatorSessionService.requestPin();
      await this.withBusy(() => this.operatorSessionService.refresh(pin));
      return;
    }

    this.error = 'Sesión operativa vencida. Reingresá usuario, contraseña y PIN.';
    await this.router.navigateByUrl('/inicio/login?reason=operator-session-missing');
    throw new Error('OPERATOR_SESSION_REAUTH_REQUIRED');
  }

  private isSessionReauthRequired(err: any): boolean {
    return `${err?.message ?? ''}` === 'OPERATOR_SESSION_REAUTH_REQUIRED';
  }

  private async ensureActiveIdentity(): Promise<boolean> {
    const operator = this.operatorSessionService.getOperatorName();
    const confirmed = await this.activityService.ensureRecentIdentity({
      idleSeconds: 60,
      confirmationMessage: `Operadora ${operator} ¿sos vos?`,
      pinPrompt: () => this.operatorSessionService.requestPin()
    });

    if (!confirmed) {
      this.error = 'Accion cancelada por inactividad';
    }

    return confirmed;
  }

  private clearMessages(): void {
    this.message = '';
    this.error = '';
    this.showOpenCashAction = false;
    this.queueWithoutCashSession = false;
  }

  goCashOpen(): void {
    void this.router.navigateByUrl('/pos/caja/apertura');
  }

  customerStatusLabel(customer: CustomerRef): string {
    const status = customer.effectiveStatus || customer.status || 'Active';
    if (status === 'Critical') return 'Crítico';
    if (status === 'Pending') return 'Pendiente';
    if (status === 'Inactive') return 'Desactivado';
    return 'Activado';
  }

  toggleOccasionalCreditForm(): void {
    this.occasionalCreditFormOpen = !this.occasionalCreditFormOpen;
  }

  setPaymentMethod(method: 'Qr' | 'AccountCredit'): void {
    if (method === 'AccountCredit' && !this.canUseAccountCredit) return;
    this.paymentMethod = method;
    this.error = '';
    if (method === 'Qr') {
      this.occasionalCreditFormOpen = false;
    }
  }

  private async ensureCashSessionReady(): Promise<boolean> {
    if (this.isTotemMode) return true;

    try {
      await this.withBusy(() => this.api.getCurrentCashSession());
      this.showOpenCashAction = false;
      this.queueWithoutCashSession = false;
      return true;
    } catch (err: any) {
      const backendMessage = `${err?.error?.message ?? err?.message ?? ''}`;
      const normalized = backendMessage.toLowerCase();
      if (normalized.includes('no open cash session') || normalized.includes('no hay una caja abierta')) {
        this.message = 'No hay caja abierta: el cobro se registrará en cola y se impactará cuando se abra caja.';
        this.showOpenCashAction = true;
        this.queueWithoutCashSession = true;
        return true;
      }

      this.error = backendMessage || 'No se pudo validar la sesión de caja.';
      this.showOpenCashAction = false;
      this.queueWithoutCashSession = false;
      return false;
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
