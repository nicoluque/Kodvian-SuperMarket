import { CommonModule } from '@angular/common';
import { Component, OnDestroy, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { BoModuleNavComponent } from './bo-module-nav.component';
import { BoCustomersPageComponent } from './bo-customers-page.component';
import { PosModuleNavComponent } from './pos-module-nav.component';

type StockModuleId = 'summary' | 'adjustment' | 'movements' | 'claims' | 'transformations';

@Component({
  standalone: true,
  selector: 'app-placeholder-page',
  imports: [CommonModule, FormsModule, RouterLink, BoModuleNavComponent, BoCustomersPageComponent, PosModuleNavComponent],
  template: `
    <main class="placeholder-wrap" [class.stock-view]="isBo() && boSectionKey() === 'stock'">
      <div class="bg-orb orb-a" *ngIf="isBo() && boSectionKey() === 'stock'"></div>
      <div class="bg-orb orb-b" *ngIf="isBo() && boSectionKey() === 'stock'"></div>
      <app-pos-module-nav *ngIf="isPos()" />
      <app-bo-module-nav *ngIf="isBo()" />
      <header *ngIf="isBo() && boSectionKey() === 'stock'" class="stock-hero">
        <div>
          <h1>Stock inteligente</h1>
          <p>Control por local, reclamos a proveedor y transformaciones con trazabilidad completa.</p>
        </div>
      </header>
      <h1 *ngIf="!(isBo() && boSectionKey() === 'stock')">{{ title() }}</h1>
      <p *ngIf="!(isBo() && boSectionKey() === 'stock')">{{ emptyText() }}</p>

      <section *ngIf="isBo() && boSectionKey() === 'stock'" class="stock-shell" style="border:1px solid #ddd;border-radius:8px;padding:12px;display:flex;flex-direction:column;gap:14px">
        <nav class="stock-tabs" aria-label="Modulos de stock">
          <button
            *ngFor="let tab of stockModules"
            type="button"
            class="stock-tab"
            [class.active]="activeStockModule === tab.id"
            (click)="setStockModule(tab.id)">
            <span>{{ tab.label }}</span>
            <small class="stock-tab-badge" *ngIf="stockModuleBadge(tab.id) > 0">{{ stockModuleBadge(tab.id) }}</small>
          </button>
        </nav>

        <div *ngIf="activeStockModule === 'summary'">
        <h3 style="margin:0">Resumen de stock</h3>
        <div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center">
          <input [(ngModel)]="stockFilter" (ngModelChange)="resetPage('stock')" placeholder="Buscar producto" style="padding:8px;min-width:220px" />
          <button (click)="refreshStockSection()" [disabled]="loadingStock || loadingStockMovements">{{ (loadingStock || loadingStockMovements) ? 'Actualizando...' : 'Actualizar stock' }}</button>
        </div>
        <p *ngIf="stockError" style="color:#b3261e;margin:0">{{ stockError }}</p>
        <div class="stock-scroll" *ngIf="!stockError">
          <table style="width:100%;border-collapse:collapse;font-size:13px">
            <thead>
              <tr>
                <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Producto</th>
                <th style="text-align:right;border-bottom:1px solid #eee;padding:6px">Vendible</th>
                <th style="text-align:right;border-bottom:1px solid #eee;padding:6px">Reclamo</th>
                <th style="text-align:right;border-bottom:1px solid #eee;padding:6px">Merma</th>
                <th style="text-align:right;border-bottom:1px solid #eee;padding:6px">Total</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let row of pagedStock()">
                <td style="border-bottom:1px solid #f3f3f3;padding:6px">{{ row.productName || ('Producto #' + row.productId) }}</td>
                <td style="text-align:right;border-bottom:1px solid #f3f3f3;padding:6px">{{ row.vendibleQty ?? 0 }}</td>
                <td style="text-align:right;border-bottom:1px solid #f3f3f3;padding:6px">{{ row.reclamoQty ?? 0 }}</td>
                <td style="text-align:right;border-bottom:1px solid #f3f3f3;padding:6px">{{ row.mermaQty ?? 0 }}</td>
                <td style="text-align:right;border-bottom:1px solid #f3f3f3;padding:6px"><strong>{{ row.totalQty ?? 0 }}</strong></td>
              </tr>
            </tbody>
          </table>
        </div>
        <div *ngIf="!loadingStock && !stockError && filteredStockRows().length > 0" style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;color:#555;font-size:12px">
          <span>Mostrando {{ pageRangeLabel(filteredStockRows().length, stockPage) }}</span>
          <div style="display:flex;gap:6px;align-items:center">
            <button (click)="prevPage('stock')" [disabled]="stockPage <= 1">Anterior</button>
            <span>Página {{ stockPage }}/{{ totalPages(filteredStockRows().length) }}</span>
            <button (click)="nextPage('stock', filteredStockRows().length)" [disabled]="stockPage >= totalPages(filteredStockRows().length)">Siguiente</button>
          </div>
        </div>
        <p *ngIf="!loadingStock && filteredStockRows().length === 0" style="margin:0;color:#555">No hay productos para el filtro aplicado. Probá otro término o recargá stock.</p>
        </div>

        <div *ngIf="activeStockModule === 'adjustment'">
        <h3 style="margin:0">Ajuste manual</h3>
        <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:8px;align-items:end">
          <label style="display:flex;flex-direction:column;gap:4px">Producto
            <select [(ngModel)]="adjustmentProductId" style="padding:8px">
              <option [ngValue]="null">Seleccionar producto</option>
              <option *ngFor="let p of stockRows" [ngValue]="p.productId">{{ p.productName || ('Producto #' + p.productId) }}</option>
            </select>
          </label>
          <label style="display:flex;flex-direction:column;gap:4px">Bucket
            <select [(ngModel)]="adjustmentBucket" style="padding:8px">
              <option value="VENDIBLE">Vendible</option>
              <option value="RECLAMO">Reclamo</option>
              <option value="MERMA">Merma</option>
            </select>
          </label>
          <label style="display:flex;flex-direction:column;gap:4px">Cantidad delta
            <input type="number" step="0.001" [(ngModel)]="adjustmentDeltaQty" placeholder="Ej: -2 o 4.5" style="padding:8px" />
          </label>
          <label style="display:flex;flex-direction:column;gap:4px">Motivo
            <input [(ngModel)]="adjustmentNotes" placeholder="Motivo del ajuste" style="padding:8px" />
          </label>
          <button (click)="applyStockAdjustment()" [disabled]="loadingAdjustment">{{ loadingAdjustment ? 'Aplicando...' : 'Aplicar ajuste' }}</button>
        </div>
        <p *ngIf="adjustmentError" style="color:#b3261e;margin:0">{{ adjustmentError }}</p>
        <p *ngIf="adjustmentMessage" style="color:#0a7a32;margin:0">{{ adjustmentMessage }}</p>
        </div>

        <div *ngIf="activeStockModule === 'movements'">
        <h3 style="margin:0">Movimientos</h3>
        <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:8px;align-items:end">
          <label style="display:flex;flex-direction:column;gap:4px">Producto
            <select [(ngModel)]="movementsProductId" (ngModelChange)="resetPage('stockMovements')" style="padding:8px">
              <option [ngValue]="null">Todos</option>
              <option *ngFor="let p of stockRows" [ngValue]="p.productId">{{ p.productName || ('Producto #' + p.productId) }}</option>
            </select>
          </label>
          <label style="display:flex;flex-direction:column;gap:4px">Tipo
            <select [(ngModel)]="movementsType" (ngModelChange)="resetPage('stockMovements')" style="padding:8px">
              <option value="">Todos</option>
              <option value="Initial">Inicial</option>
              <option value="Purchase">Compra</option>
              <option value="Sale">Venta</option>
              <option value="SupplierClaim">Reclamo proveedor</option>
              <option value="Waste">Merma</option>
              <option value="Adjustment">Ajuste</option>
              <option value="Transformation">Transformacion</option>
            </select>
          </label>
          <label style="display:flex;flex-direction:column;gap:4px">Desde
            <input type="date" [(ngModel)]="movementsFrom" style="padding:8px" />
          </label>
          <label style="display:flex;flex-direction:column;gap:4px">Hasta
            <input type="date" [(ngModel)]="movementsTo" style="padding:8px" />
          </label>
          <button (click)="loadStockMovements()" [disabled]="loadingStockMovements">{{ loadingStockMovements ? 'Cargando...' : 'Buscar movimientos' }}</button>
        </div>
        <p *ngIf="stockMovementsError" style="color:#b3261e;margin:0">{{ stockMovementsError }}</p>
        <div class="stock-scroll" *ngIf="!stockMovementsError">
        <table style="width:100%;border-collapse:collapse;font-size:13px">
          <thead>
            <tr>
              <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Fecha</th>
              <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Producto</th>
              <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Tipo</th>
              <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Bucket</th>
              <th style="text-align:right;border-bottom:1px solid #eee;padding:6px">Delta</th>
              <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Motivo</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let m of pagedStockMovements()">
              <td style="border-bottom:1px solid #f3f3f3;padding:6px">{{ m.createdAt | date:'short' }}</td>
              <td style="border-bottom:1px solid #f3f3f3;padding:6px">{{ m.productName || ('Producto #' + m.productId) }}</td>
              <td style="border-bottom:1px solid #f3f3f3;padding:6px">{{ stockMovementTypeLabel(m.movementType) }}</td>
              <td style="border-bottom:1px solid #f3f3f3;padding:6px">{{ m.bucket }}</td>
              <td style="text-align:right;border-bottom:1px solid #f3f3f3;padding:6px">{{ m.deltaQty }}</td>
              <td style="border-bottom:1px solid #f3f3f3;padding:6px">{{ m.notes || '-' }}</td>
            </tr>
          </tbody>
        </table>
        </div>
        <div *ngIf="!loadingStockMovements && !stockMovementsError && stockMovements.length > 0" style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;color:#555;font-size:12px">
          <span>Mostrando {{ pageRangeLabel(stockMovements.length, stockMovementsPage) }}</span>
          <div style="display:flex;gap:6px;align-items:center">
            <button (click)="prevPage('stockMovements')" [disabled]="stockMovementsPage <= 1">Anterior</button>
            <span>Página {{ stockMovementsPage }}/{{ totalPages(stockMovements.length) }}</span>
            <button (click)="nextPage('stockMovements', stockMovements.length)" [disabled]="stockMovementsPage >= totalPages(stockMovements.length)">Siguiente</button>
          </div>
        </div>
        <p *ngIf="!loadingStockMovements && stockMovements.length === 0" style="margin:0;color:#555">No hay movimientos para los filtros seleccionados.</p>
        </div>

        <div *ngIf="activeStockModule === 'claims'" class="claims-section">
        <h3 style="margin:0">Reclamos a proveedor</h3>

        <div class="claims-summary-grid">
          <article class="claims-summary-card">
            <p>Total</p>
            <strong>{{ claims.length }}</strong>
          </article>
          <article class="claims-summary-card is-open">
            <p>Abiertos</p>
            <strong>{{ claimsOpenCount() }}</strong>
          </article>
          <article class="claims-summary-card is-pickup">
            <p>Retirados</p>
            <strong>{{ claimsPickupCount() }}</strong>
          </article>
          <article class="claims-summary-card is-solved">
            <p>Resueltos</p>
            <strong>{{ claimsSolvedCount() }}</strong>
          </article>
        </div>

        <div class="claims-settlement-kpis">
          <button type="button" class="settlement-kpi" [ngClass]="settlementKpiClass('credit')" (click)="setClaimsSettlementFilter('credit')">
            <span>Pend. crédito</span>
            <strong>{{ claimsPendingByModeCount('credit') }}</strong>
          </button>
          <button type="button" class="settlement-kpi" [ngClass]="settlementKpiClass('refund')" (click)="setClaimsSettlementFilter('refund')">
            <span>Pend. reembolso</span>
            <strong>{{ claimsPendingByModeCount('refund') }}</strong>
          </button>
          <button type="button" class="settlement-kpi" [ngClass]="settlementKpiClass('exchange')" (click)="setClaimsSettlementFilter('exchange')">
            <span>Pend. reposición</span>
            <strong>{{ claimsPendingByModeCount('exchange') }}</strong>
          </button>
          <button type="button" class="settlement-kpi clear" [ngClass]="settlementKpiClass('')" (click)="setClaimsSettlementFilter('')">
            <span>Ver todas</span>
            <strong>{{ claimsPendingCount() }}</strong>
          </button>
        </div>

        <div class="claims-priority-alert" *ngIf="claimsPendingByModeCount('refund') > 0">
          <strong>Atención:</strong>
          Hay {{ claimsPendingByModeCount('refund') }} reclamo{{ claimsPendingByModeCount('refund') === 1 ? '' : 's' }} pendiente{{ claimsPendingByModeCount('refund') === 1 ? '' : 's' }} de <b>reembolso</b>.
          <button type="button" (click)="setClaimsSettlementFilter('refund')">Ver ahora</button>
        </div>

        <div class="claims-workspace claims-workspace-single">
          <section class="claims-panel">
            <div class="claims-panel-head">
              <h4>Seguimiento</h4>
              <small>Filtrá por estado o proveedor para operar mas rapido.</small>
            </div>

            <div class="claims-top-actions">
              <button class="claims-open-modal" (click)="openClaimModal()">Nuevo reclamo</button>
              <span>Creá reclamos con varios productos y fotos de evidencia.</span>
            </div>

            <section class="claim-policy-admin">
              <header>
                <h5>Condición por proveedor</h5>
                <small>Define si el reclamo se resuelve por crédito, reembolso o reposición con mercadería.</small>
              </header>
              <div class="claim-policy-grid">
                <label class="claims-field">Proveedor
                  <select [(ngModel)]="policySupplierId" (ngModelChange)="onPolicySupplierChange()">
                    <option [ngValue]="null">Seleccionar proveedor</option>
                    <option *ngFor="let s of supplierOptions" [ngValue]="s.id">{{ s.name }}</option>
                  </select>
                </label>
                <label class="claims-field">Condición por defecto
                  <select [(ngModel)]="policySettlementMode" [disabled]="policySupplierId == null">
                    <option value="credit">Crédito</option>
                    <option value="refund">Reembolso</option>
                    <option value="exchange">Reposición con mercadería</option>
                  </select>
                </label>
                <label class="claims-field policy-check">
                  <input type="checkbox" [(ngModel)]="policyAllowOverride" [disabled]="policySupplierId == null" />
                  Permitir que admin cambie la condición en cada reclamo
                </label>
                <button type="button" class="claim-policy-save" (click)="saveSupplierPolicy()" [disabled]="savingPolicy || policySupplierId == null">{{ savingPolicy ? 'Guardando...' : 'Guardar condición' }}</button>
              </div>
              <p *ngIf="policyMessage" class="claim-policy-message">{{ policyMessage }}</p>
            </section>

            <div class="claims-filters-row">
              <select [(ngModel)]="claimsStatus" (ngModelChange)="resetPage('claims')">
                <option value="">Todos</option>
                <option value="Pending">Pendiente</option>
                <option value="PickedUp">Retirado</option>
                <option value="Credited">Acreditado</option>
                <option value="Replaced">Repuesto</option>
                <option value="Refunded">Reembolsado</option>
              </select>
              <select [(ngModel)]="claimsSettlementFilter" (ngModelChange)="resetPage('claims')">
                <option value="">Todas las condiciones</option>
                <option value="credit">Crédito</option>
                <option value="refund">Reembolso</option>
                <option value="exchange">Reposición</option>
              </select>
              <input [(ngModel)]="claimsFilter" (ngModelChange)="resetPage('claims')" placeholder="Buscar proveedor o estado" />
              <button (click)="loadClaims()" [disabled]="loadingClaims">{{ loadingClaims ? 'Actualizando...' : 'Actualizar reclamos' }}</button>
            </div>

            <p *ngIf="claimsError" class="claims-error">{{ claimsError }}</p>

            <div class="stock-scroll claims-table-desktop" *ngIf="!claimsError">
              <table class="claims-main-table" style="width:100%;border-collapse:collapse;font-size:13px">
                <thead>
                  <tr>
                    <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Reclamo</th>
                    <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Proveedor</th>
                    <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Estado</th>
                    <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Condicion</th>
                    <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Evidencias</th>
                    <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Fecha</th>
                    <th class="col-actions" style="text-align:left;border-bottom:1px solid #eee;padding:6px">Acciones</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let c of pagedClaims()">
                    <td style="border-bottom:1px solid #f3f3f3;padding:8px 6px">
                      <button type="button" class="claim-link-text" (click)="openClaimDetail(c.id)">#{{ c.id }}</button>
                    </td>
                    <td style="border-bottom:1px solid #f3f3f3;padding:8px 6px">
                      <button type="button" class="claim-link-text" (click)="openClaimDetail(c.id)">{{ c.supplierName || '-' }}</button>
                    </td>
                    <td style="border-bottom:1px solid #f3f3f3;padding:8px 6px"><span class="claim-status" [ngClass]="claimStatusClass(c.status)">{{ claimStatusLabel(c.status) }}</span></td>
                    <td style="border-bottom:1px solid #f3f3f3;padding:8px 6px"><span class="claim-settlement-chip" [ngClass]="claimSettlementBadgeClass(c)">{{ claimRequestedModeLabel(c) }}</span></td>
                    <td style="border-bottom:1px solid #f3f3f3;padding:8px 6px">
                      <span class="claim-evidence-chip">{{ claimEvidenceCount(c) }} foto{{ claimEvidenceCount(c) === 1 ? '' : 's' }}</span>
                    </td>
                    <td style="border-bottom:1px solid #f3f3f3;padding:8px 6px">{{ c.createdAt | date:'short' }}</td>
                    <td class="col-actions" style="border-bottom:1px solid #f3f3f3;padding:8px 6px">
                      <div class="claim-actions-cell">
                        <button type="button" class="claim-detail-btn claim-row-action" (click)="openClaimDetail(c.id)">Ver detalle</button>
                        <a *ngIf="claimFirstEvidenceUrl(c) as evidenceUrl" class="claim-link" [href]="evidenceUrl" target="_blank" rel="noopener">Ver foto</a>
                        <button *ngIf="canPickup(c.status)" class="claim-row-action is-primary" (click)="pickupClaim(c.id)" [disabled]="loadingClaims">Retirar</button>
                        <button *ngIf="canResolveCredit(c)" class="claim-row-action is-primary" (click)="creditClaim(c.id)" [disabled]="loadingClaims">Acreditar</button>
                        <button *ngIf="canResolveRefund(c)" class="claim-row-action is-primary" (click)="refundClaim(c.id)" [disabled]="loadingClaims">Reembolsar</button>
                        <button *ngIf="canResolveExchange(c)" class="claim-row-action is-primary" (click)="replaceClaim(c.id)" [disabled]="loadingClaims">Reponer</button>
                        <span *ngIf="!canPickup(c.status) && !canResolveCredit(c) && !canResolveRefund(c) && !canResolveExchange(c)" class="claim-action-muted">Sin acciones</span>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <div class="claims-cards-mobile" *ngIf="!claimsError">
              <article class="claim-card" *ngFor="let c of pagedClaims()">
                <div class="claim-card-head">
                  <strong><button type="button" class="claim-link-text" (click)="openClaimDetail(c.id)">Reclamo #{{ c.id }}</button></strong>
                  <span class="claim-status" [ngClass]="claimStatusClass(c.status)">{{ claimStatusLabel(c.status) }}</span>
                </div>
                <p><strong>Proveedor:</strong> {{ c.supplierName || '-' }}</p>
                <p><strong>Condicion:</strong> <span class="claim-settlement-chip" [ngClass]="claimSettlementBadgeClass(c)">{{ claimRequestedModeLabel(c) }}</span></p>
                <p><strong>Evidencias:</strong> {{ claimEvidenceCount(c) }} foto{{ claimEvidenceCount(c) === 1 ? '' : 's' }}</p>
                <p><strong>Fecha:</strong> {{ c.createdAt | date:'short' }}</p>
                <div class="claim-actions-cell">
                  <button type="button" class="claim-detail-btn claim-row-action" (click)="openClaimDetail(c.id)">Ver detalle</button>
                  <a *ngIf="claimFirstEvidenceUrl(c) as evidenceUrl" class="claim-link" [href]="evidenceUrl" target="_blank" rel="noopener">Ver foto</a>
                  <button *ngIf="canPickup(c.status)" class="claim-row-action is-primary" (click)="pickupClaim(c.id)" [disabled]="loadingClaims">Retirar</button>
                  <button *ngIf="canResolveCredit(c)" class="claim-row-action is-primary" (click)="creditClaim(c.id)" [disabled]="loadingClaims">Acreditar</button>
                  <button *ngIf="canResolveRefund(c)" class="claim-row-action is-primary" (click)="refundClaim(c.id)" [disabled]="loadingClaims">Reembolsar</button>
                  <button *ngIf="canResolveExchange(c)" class="claim-row-action is-primary" (click)="replaceClaim(c.id)" [disabled]="loadingClaims">Reponer</button>
                  <span *ngIf="!canPickup(c.status) && !canResolveCredit(c) && !canResolveRefund(c) && !canResolveExchange(c)" class="claim-action-muted">Sin acciones</span>
                </div>
              </article>
            </div>

            <div *ngIf="!loadingClaims && !claimsError && filteredClaimsRows().length > 0" style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;color:#555;font-size:12px">
              <span>Mostrando {{ pageRangeLabel(filteredClaimsRows().length, claimsPage) }}</span>
              <div style="display:flex;gap:6px;align-items:center">
                <button (click)="prevPage('claims')" [disabled]="claimsPage <= 1">Anterior</button>
                <span>Página {{ claimsPage }}/{{ totalPages(filteredClaimsRows().length) }}</span>
                <button (click)="nextPage('claims', filteredClaimsRows().length)" [disabled]="claimsPage >= totalPages(filteredClaimsRows().length)">Siguiente</button>
              </div>
            </div>
            <p *ngIf="!loadingClaims && filteredClaimsRows().length === 0" style="margin:0;color:#555">No hay reclamos para los filtros seleccionados.</p>
          </section>
        </div>

        <div class="claim-modal-overlay" *ngIf="isClaimModalOpen" (click)="closeClaimModal()">
          <section class="claim-modal" (click)="$event.stopPropagation()">
            <div class="claim-modal-head">
              <div>
                <h4>Nuevo reclamo</h4>
                <p>Registrá uno o varios productos con su evidencia fotográfica.</p>
              </div>
              <button type="button" class="claim-modal-close" (click)="closeClaimModal()">Cerrar</button>
            </div>

            <div class="claims-form-grid">
              <label class="claims-field">Proveedor
                <select [(ngModel)]="claimSupplierId" (ngModelChange)="onClaimSupplierChange()">
                  <option [ngValue]="null">Sin proveedor</option>
                  <option *ngFor="let s of supplierOptions" [ngValue]="s.id">{{ s.name }}</option>
                </select>
              </label>

              <label class="claims-field">Resolucion
                <select [(ngModel)]="claimResolutionType" [disabled]="isClaimSettlementLocked()">
                  <option value="credit">Crédito en próxima compra</option>
                  <option value="refund">Reembolso</option>
                  <option value="exchange">Reposición con otra mercadería</option>
                </select>
              </label>

              <p class="claim-policy-hint claim-field-full" *ngIf="selectedSupplierPolicyHint() as policyHint">{{ policyHint }}</p>

              <label class="claims-field claim-field-full">Motivo general
                <input [(ngModel)]="claimNotes" placeholder="Danado, sin gas, abollado, etc." />
              </label>
            </div>

            <div class="claim-lines-head">
              <strong>Productos reclamados</strong>
              <button type="button" class="claim-add-row" (click)="addClaimItem()">Agregar producto</button>
            </div>

            <div class="claim-line-grid" *ngFor="let item of claimItems; let i = index">
              <label class="claims-field">Producto
                <select [(ngModel)]="item.productId">
                  <option [ngValue]="null">Seleccionar producto</option>
                  <option *ngFor="let p of stockRows" [ngValue]="p.productId">{{ p.productName || ('Producto #' + p.productId) }}</option>
                </select>
              </label>

              <label class="claims-field">Cantidad
                <input type="number" step="0.001" [(ngModel)]="item.quantity" placeholder="Ej: 2" />
              </label>

              <label class="claims-field">Costo unitario
                <input type="number" step="0.01" [(ngModel)]="item.unitCost" placeholder="Ej: 1500" />
              </label>

              <label class="claims-field">Nota del producto
                <input [(ngModel)]="item.notes" placeholder="Detalle opcional" />
              </label>

              <button type="button" class="claim-remove-row" (click)="removeClaimItem(i)" [disabled]="claimItems.length === 1">Quitar</button>
            </div>

            <div class="claim-evidence-block">
              <label class="claims-field">Fotos de evidencia (max 5)
                <input type="file" accept="image/png,image/jpeg,image/webp" multiple (change)="onClaimEvidenceSelected($event)" />
              </label>
              <div class="claim-file-list" *ngIf="claimEvidenceFiles.length > 0">
                <article class="claim-file-item" *ngFor="let file of claimEvidenceFiles; let i = index">
                  <span>{{ file.name }}</span>
                  <button type="button" (click)="removeClaimEvidence(i)">Quitar</button>
                </article>
              </div>
            </div>

            <p *ngIf="claimsError" class="claims-error">{{ claimsError }}</p>

            <div class="claims-inline-errors" *ngIf="claimFormHint() as claimHint">
              {{ claimHint }}
            </div>

            <div class="claims-preview" *ngIf="claimCanPreview()">
              <span>{{ claimPreviewTitle() }}</span>
              <strong>Total estimado: {{ claimPreviewTotal() }}</strong>
            </div>

            <div class="claim-modal-actions">
              <button type="button" class="btn-secondary" (click)="closeClaimModal()">Cancelar</button>
              <button type="button" class="claims-submit" (click)="createClaimFromStock()" [disabled]="loadingClaims || !canSubmitClaimForm()">{{ loadingClaims ? 'Guardando...' : 'Crear reclamo' }}</button>
            </div>
          </section>
        </div>

        <div class="claim-detail-backdrop" *ngIf="isClaimDetailOpen" (click)="closeClaimDetail()">
          <aside class="claim-detail-drawer" (click)="$event.stopPropagation()">
            <header class="claim-detail-head">
              <div>
                <h4>Reclamo #{{ selectedClaimDetail?.id }}</h4>
                <p>{{ selectedClaimDetail?.supplierName || 'Proveedor no informado' }}</p>
              </div>
              <button type="button" class="claim-modal-close" (click)="closeClaimDetail()">Cerrar</button>
            </header>

            <div class="claim-detail-loading" *ngIf="loadingClaimDetail">Cargando detalle...</div>
            <p *ngIf="claimDetailError" class="claims-error">{{ claimDetailError }}</p>

            <div *ngIf="selectedClaimDetail && !loadingClaimDetail" class="claim-detail-body">
              <div class="claim-detail-top">
                <span class="claim-status" [ngClass]="claimStatusClass(selectedClaimDetail.status)">{{ claimStatusLabel(selectedClaimDetail.status) }}</span>
                <span>Condicion: {{ claimResolutionLabel(selectedClaimDetail) }}</span>
                <span>Creado: {{ selectedClaimDetail.createdAt | date:'short' }}</span>
                <span *ngIf="selectedClaimDetail.pickedUpAt">Retirado: {{ selectedClaimDetail.pickedUpAt | date:'short' }}</span>
                <span *ngIf="selectedClaimDetail.creditedAt">Resuelto: {{ selectedClaimDetail.creditedAt | date:'short' }}</span>
              </div>

              <section class="claim-detail-card">
                <h5>Seguimiento del reclamo</h5>
                <div class="claim-timeline">
                  <article class="claim-step is-done">
                    <span class="dot"></span>
                    <div>
                      <strong>Creado</strong>
                      <small>{{ selectedClaimDetail.createdAt | date:'short' }}</small>
                    </div>
                  </article>

                  <article class="claim-step" [class.is-done]="claimIsPicked(selectedClaimDetail)" [class.is-active]="claimIsPicked(selectedClaimDetail) && !claimIsResolved(selectedClaimDetail)">
                    <span class="dot"></span>
                    <div>
                      <strong>Retiro proveedor</strong>
                      <small>{{ selectedClaimDetail.pickedUpAt ? (selectedClaimDetail.pickedUpAt | date:'short') : 'Pendiente' }}</small>
                    </div>
                  </article>

                  <article class="claim-step" [class.is-done]="claimIsResolved(selectedClaimDetail)">
                    <span class="dot"></span>
                    <div>
                      <strong>{{ claimResolutionLabel(selectedClaimDetail) }}</strong>
                      <small>{{ selectedClaimDetail.creditedAt ? (selectedClaimDetail.creditedAt | date:'short') : 'Pendiente' }}</small>
                    </div>
                  </article>
                </div>
              </section>

              <section class="claim-detail-card">
                <h5>Productos reclamados</h5>
                <div class="stock-scroll">
                  <table class="claim-detail-table">
                    <thead>
                      <tr>
                        <th>Producto</th>
                        <th>Cantidad</th>
                        <th>Costo unitario</th>
                        <th>Subtotal</th>
                        <th>Nota</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr *ngFor="let item of selectedClaimDetail.items">
                        <td>{{ item.productName || ('Producto #' + item.productId) }}</td>
                        <td>{{ item.quantity }}</td>
                        <td>{{ formatCurrency(item.unitCostSnapshot) }}</td>
                        <td>{{ formatCurrency((item.quantity || 0) * (item.unitCostSnapshot || 0)) }}</td>
                        <td>{{ item.notes || '-' }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </section>

              <section class="claim-detail-card" *ngIf="selectedClaimDetail.notes">
                <h5>Motivo y notas</h5>
                <p>{{ selectedClaimDetail.notes }}</p>
              </section>

              <section class="claim-detail-card">
                <div class="claim-detail-evidence-head">
                  <h5>Evidencias</h5>
                  <span>{{ claimEvidenceCount(selectedClaimDetail) }} foto{{ claimEvidenceCount(selectedClaimDetail) === 1 ? '' : 's' }}</span>
                </div>
                <div class="claim-evidence-gallery" *ngIf="claimEvidenceCount(selectedClaimDetail) > 0; else noEvidenceTpl">
                  <article class="claim-evidence-thumb" *ngFor="let ev of selectedClaimDetail.evidences; let i = index">
                    <img [src]="ev.previewUrl || ev.downloadUrl" [alt]="ev.fileName || 'Evidencia'" />
                    <div>
                      <strong>{{ ev.fileName }}</strong>
                      <small>{{ formatFileSize(ev.fileSize) }}</small>
                    </div>
                    <div class="claim-evidence-actions">
                      <button type="button" (click)="openEvidenceViewer(selectedClaimDetail.evidences, i)">Ver</button>
                      <a [href]="ev.downloadUrl" target="_blank" rel="noopener">Descargar</a>
                    </div>
                  </article>
                </div>
                <ng-template #noEvidenceTpl>
                  <p class="claim-empty-evidence">No hay fotos adjuntas en este reclamo.</p>
                </ng-template>
              </section>

              <section class="claim-detail-card">
                <h5>Acciones</h5>
                <div class="claim-actions-cell">
                  <button *ngIf="canPickup(selectedClaimDetail.status)" (click)="pickupClaim(selectedClaimDetail.id)" [disabled]="loadingClaims">Retirar</button>
                  <button *ngIf="canResolveCredit(selectedClaimDetail)" (click)="creditClaim(selectedClaimDetail.id)" [disabled]="loadingClaims">Acreditar</button>
                  <button *ngIf="canResolveRefund(selectedClaimDetail)" (click)="refundClaim(selectedClaimDetail.id)" [disabled]="loadingClaims">Reembolsar</button>
                  <button *ngIf="canResolveExchange(selectedClaimDetail)" (click)="replaceClaim(selectedClaimDetail.id)" [disabled]="loadingClaims">Reponer</button>
                  <span *ngIf="!canPickup(selectedClaimDetail.status) && !canResolveCredit(selectedClaimDetail) && !canResolveRefund(selectedClaimDetail) && !canResolveExchange(selectedClaimDetail)" class="claim-action-muted">Sin acciones pendientes</span>
                </div>
              </section>
            </div>
          </aside>
        </div>

        <div class="claim-image-viewer" *ngIf="isEvidenceViewerOpen && selectedEvidence" (click)="closeEvidenceViewer()">
          <section class="claim-image-content" (click)="$event.stopPropagation()">
            <header>
              <strong>{{ selectedEvidence.fileName }}</strong>
              <button type="button" class="claim-modal-close" (click)="closeEvidenceViewer()">Cerrar</button>
            </header>
            <img [src]="selectedEvidence.previewUrl || selectedEvidence.downloadUrl" [alt]="selectedEvidence.fileName || 'Evidencia'" />
            <footer>
              <button type="button" (click)="previousEvidence()" [disabled]="!canGoPreviousEvidence()">Anterior</button>
              <a [href]="selectedEvidence.downloadUrl" target="_blank" rel="noopener">Descargar</a>
              <button type="button" (click)="nextEvidence()" [disabled]="!canGoNextEvidence()">Siguiente</button>
            </footer>
          </section>
        </div>

        <div class="claim-modal-overlay" *ngIf="isExchangeResolveModalOpen" (click)="closeExchangeResolveModal()">
          <section class="claim-modal" (click)="$event.stopPropagation()">
            <div class="claim-modal-head">
              <div>
                <h4>Reposición con mercadería</h4>
                <p>Indicá los productos que entrega el proveedor para cerrar el reclamo #{{ exchangeResolveClaimId }}.</p>
              </div>
              <button type="button" class="claim-modal-close" (click)="closeExchangeResolveModal()">Cerrar</button>
            </div>

            <div class="claim-line-grid" *ngFor="let line of exchangeResolveLines; let i = index">
              <label class="claims-field">Producto reposición
                <select [(ngModel)]="line.productId">
                  <option [ngValue]="null">Seleccionar producto</option>
                  <option *ngFor="let p of stockRows" [ngValue]="p.productId">{{ p.productName || ('Producto #' + p.productId) }}</option>
                </select>
              </label>
              <label class="claims-field">Cantidad
                <input type="number" step="0.001" [(ngModel)]="line.quantity" placeholder="Ej: 1" />
              </label>
              <label class="claims-field">Costo unitario
                <input type="number" step="0.01" [(ngModel)]="line.unitCost" placeholder="Ej: 1200" />
              </label>
              <label class="claims-field">Nota
                <input [(ngModel)]="line.notes" placeholder="Opcional" />
              </label>
              <button type="button" class="claim-remove-row" (click)="removeExchangeResolveLine(i)" [disabled]="exchangeResolveLines.length === 1">Quitar</button>
            </div>

            <div class="claim-actions-cell">
              <button type="button" class="claim-add-row" (click)="addExchangeResolveLine()">Agregar linea</button>
            </div>

            <p *ngIf="exchangeResolveError" class="claims-error">{{ exchangeResolveError }}</p>

            <div class="claim-modal-actions">
              <button type="button" class="btn-secondary" (click)="closeExchangeResolveModal()">Cancelar</button>
              <button type="button" class="claims-submit" (click)="confirmExchangeResolve()" [disabled]="loadingClaims">{{ loadingClaims ? 'Guardando...' : 'Confirmar reposición' }}</button>
            </div>
          </section>
        </div>
        </div>

        <div *ngIf="activeStockModule === 'transformations'" class="transformations-section">
        <div class="transformations-header">
          <h3>Transformaciones inteligentes</h3>
          <p>Cargá solo datos operativos. El sistema calcula factor, aprende rendimiento y gestiona recalibración en segundo plano.</p>
        </div>

        <section class="transform-card transform-apply-card">
          <div class="transform-card-head">
            <h4>Nueva transformación</h4>
            <small>Completa proveedor, productos y cantidades. La nota es opcional.</small>
          </div>
          <div class="transform-form-grid">
            <label class="transform-field">Proveedor
              <select [(ngModel)]="transformationSupplierId" (ngModelChange)="onTransformationContextChange()">
                <option [ngValue]="null">Sin proveedor (fallback automático)</option>
                <option *ngFor="let s of supplierOptions" [ngValue]="s.id">{{ s.name }}</option>
              </select>
            </label>
            <label class="transform-field">Producto origen
              <select [(ngModel)]="transformationSourceProductId" (ngModelChange)="onTransformationContextChange()">
                <option [ngValue]="null">Seleccionar origen</option>
                <option *ngFor="let p of stockRows" [ngValue]="p.productId">{{ p.productName || ('Producto #' + p.productId) }}</option>
              </select>
            </label>
            <label class="transform-field">Cantidad origen
              <input type="number" step="0.001" [(ngModel)]="transformationSourceQty" (ngModelChange)="onTransformationSourceQtyChange()" placeholder="Ej: 10" />
            </label>
            <label class="transform-field">Producto a recibir
              <select [(ngModel)]="transformationTargetProductId" (ngModelChange)="onTransformationContextChange()">
                <option [ngValue]="null">Seleccionar producto recibido</option>
                <option *ngFor="let p of stockRows" [ngValue]="p.productId">{{ p.productName || ('Producto #' + p.productId) }}</option>
              </select>
            </label>
            <label class="transform-field">Cantidad a recibir
              <input type="number" step="0.001" [(ngModel)]="transformationTargetQty" (ngModelChange)="onTransformationTargetQtyChange()" placeholder="Ej: 5" />
            </label>
            <label class="transform-field transform-field-full">Nota (opcional)
              <input [(ngModel)]="transformationNotes" placeholder="Detalle de lote, merma o contexto" />
            </label>
          </div>

          <p *ngIf="shouldShowTransformationApplyHint() && transformationApplyHint()" class="transform-feedback error">{{ transformationApplyHint() }}</p>

          <div class="transform-suggestion" *ngIf="transformationSuggestion">
            <span><b>Sugerencia:</b> factor {{ transformationSuggestion.suggestedYieldFactor }} · {{ transformationSuggestion.source }}</span>
            <span><b>Confianza:</b> {{ transformationSuggestion.confidence }} · {{ transformationSuggestion.sampleCount }} muestra{{ transformationSuggestion.sampleCount === 1 ? '' : 's' }}</span>
            <span *ngIf="transformationObservedFactor() > 0"><b>Factor observado:</b> {{ transformationObservedFactor() }}</span>
            <span *ngIf="transformationDeviationPct() != null"><b>Desvío:</b> {{ transformationDeviationPct() }}%</span>
          </div>
          <div class="transform-preview" *ngIf="transformationSourceQty && transformationTargetQty">
            <span>Impacto de stock</span>
            <strong>Se descuenta {{ transformationSourceQty }} de origen y se suma {{ transformationTargetQty }} de destino</strong>
          </div>
          <button class="transform-main-cta" (click)="applyTransformation()" [disabled]="loadingTransformations || !!transformationApplyHint()">{{ loadingTransformations ? 'Aplicando...' : 'Aplicar transformación' }}</button>
        </section>

        <p *ngIf="transformationsError" class="transform-feedback error">{{ transformationsError }}</p>
        <p *ngIf="transformationsMessage" class="transform-feedback success">{{ transformationsMessage }}</p>

        <details class="transform-advanced" *ngIf="transformationTemplates.length > 0 || transformationRecalibrations.length > 0">
          <summary>Avanzado (admin)</summary>

          <section class="transform-card">
            <div class="transform-card-head">
              <h4>Automatización de rendimiento</h4>
              <small>Configurá cuándo el sistema propone o deja lista una recalibración de plantilla.</small>
            </div>
            <div class="transform-policy-grid">
              <label class="transform-field policy-check-inline">
                <input type="checkbox" [(ngModel)]="transformationYieldPolicy.autoUpdateEnabled" />
                Activar recalibración inteligente
              </label>
              <label class="transform-field policy-check-inline">
                <input type="checkbox" [(ngModel)]="transformationYieldPolicy.requireAdminApproval" [disabled]="!transformationYieldPolicy.autoUpdateEnabled" />
                Requerir aprobación de admin
              </label>
              <label class="transform-field">Muestras mínimas
                <input type="number" min="3" max="100" [(ngModel)]="transformationYieldPolicy.minSampleCount" [disabled]="!transformationYieldPolicy.autoUpdateEnabled" />
              </label>
              <label class="transform-field">Volatilidad máxima
                <input type="number" step="0.01" min="0.01" max="1" [(ngModel)]="transformationYieldPolicy.maxVolatility" [disabled]="!transformationYieldPolicy.autoUpdateEnabled" />
              </label>
              <label class="transform-field">Desvío mínimo (%)
                <input type="number" step="0.1" min="0" max="50" [(ngModel)]="transformationYieldPolicy.minDeviationPct" [disabled]="!transformationYieldPolicy.autoUpdateEnabled" />
              </label>
              <label class="transform-field">Desvío máximo (%)
                <input type="number" step="0.1" min="1" max="100" [(ngModel)]="transformationYieldPolicy.maxDeviationPct" [disabled]="!transformationYieldPolicy.autoUpdateEnabled" />
              </label>
            </div>
            <p class="transform-policy-note">El sistema ignora desvíos menores al umbral mínimo para evitar ruido operativo.</p>
            <div class="transform-card-actions">
              <button (click)="saveTransformationYieldPolicy()" [disabled]="loadingTransformationPolicy">{{ loadingTransformationPolicy ? 'Guardando...' : 'Guardar política' }}</button>
            </div>
            <p *ngIf="transformationPolicyMessage" class="transform-feedback success">{{ transformationPolicyMessage }}</p>
          </section>

          <section class="transform-card" *ngIf="transformationRecalibrations.length > 0">
            <div class="transform-card-head">
              <h4>Recalibraciones pendientes</h4>
              <small>Revisá y aprobá propuestas antes de actualizar plantillas automáticamente.</small>
            </div>
            <div class="stock-scroll">
              <table class="transform-table">
                <thead>
                  <tr>
                    <th>Contexto</th>
                    <th style="text-align:right">Actual</th>
                    <th style="text-align:right">Propuesto</th>
                    <th style="text-align:right">Desvío</th>
                    <th style="text-align:right">Muestras</th>
                    <th>Acciones</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let r of transformationRecalibrations">
                    <td>Prov: {{ r.supplierId ?? '-' }} · {{ productNameById(r.sourceProductId) }} -> {{ productNameById(r.targetProductId) }}</td>
                    <td style="text-align:right">{{ r.currentYieldFactor }}</td>
                    <td style="text-align:right">{{ r.proposedYieldFactor }}</td>
                    <td style="text-align:right">{{ r.deviationPct }}%</td>
                    <td style="text-align:right">{{ r.sampleCount }}</td>
                    <td class="transform-row-actions">
                      <button class="transform-use-btn" (click)="approveTransformationRecalibration(r.id)">Aprobar</button>
                      <button class="transform-reject-btn" (click)="rejectTransformationRecalibration(r.id)">Rechazar</button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
            <p *ngIf="transformationRecalibrationsError" class="transform-feedback error">{{ transformationRecalibrationsError }}</p>
          </section>

          <section class="transform-card" *ngIf="transformationTemplates.length > 0">
            <div class="transform-card-head">
              <h4>Plantillas guardadas</h4>
              <small>Base técnica para sugerencias y fallback automático.</small>
            </div>
            <div class="stock-scroll">
              <table class="transform-table">
                <thead>
                  <tr>
                    <th>Proveedor</th>
                    <th>Origen</th>
                    <th>Destino</th>
                    <th style="text-align:right">Factor</th>
                    <th>Actualizado</th>
                    <th>Acción</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let t of transformationTemplates">
                    <td>{{ t.supplierId ?? '-' }}</td>
                    <td>{{ productNameById(t.sourceProductId) }}</td>
                    <td>{{ productNameById(t.targetProductId) }}</td>
                    <td style="text-align:right">{{ t.yieldFactor }}</td>
                    <td>{{ t.updatedAt | date:'short' }}</td>
                    <td><button class="transform-use-btn" (click)="useTransformationTemplate(t)">Usar</button></td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>
        </details>

        <section class="transform-card">
          <div class="transform-card-head">
            <h4>Historial de transformaciones</h4>
            <small>Filtrá por fecha para auditar operaciones y variaciones de stock.</small>
          </div>
          <div class="transform-history-filters">
            <label class="transform-field">Desde
              <input type="date" [(ngModel)]="transformationHistoryFrom" />
            </label>
            <label class="transform-field">Hasta
              <input type="date" [(ngModel)]="transformationHistoryTo" />
            </label>
            <button (click)="loadTransformationHistory()" [disabled]="loadingTransformationHistory">{{ loadingTransformationHistory ? 'Cargando...' : 'Buscar historial' }}</button>
          </div>
          <p *ngIf="transformationHistoryError" class="transform-feedback error">{{ transformationHistoryError }}</p>
          <div class="stock-scroll" *ngIf="!transformationHistoryError">
          <table class="transform-table">
            <thead>
              <tr>
                <th>Fecha</th>
                <th>Producto</th>
                <th style="text-align:right">Delta</th>
                <th>Detalle</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let h of pagedTransformationHistory()">
                <td>{{ h.createdAt | date:'short' }}</td>
                <td>{{ h.productName || ('Producto #' + h.productId) }}</td>
                <td style="text-align:right">{{ h.deltaQty }}</td>
                <td>{{ formatTransformationHistoryDetail(h.notes) }}</td>
              </tr>
            </tbody>
          </table>
          </div>
          <div *ngIf="!loadingTransformationHistory && !transformationHistoryError && transformationHistory.length > 0" style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;color:#555;font-size:12px">
            <span>Mostrando {{ pageRangeLabel(transformationHistory.length, transformationHistoryPage) }}</span>
            <div style="display:flex;gap:6px;align-items:center">
              <button (click)="prevPage('transformations')" [disabled]="transformationHistoryPage <= 1">Anterior</button>
              <span>Página {{ transformationHistoryPage }}/{{ totalPages(transformationHistory.length) }}</span>
              <button (click)="nextPage('transformations', transformationHistory.length)" [disabled]="transformationHistoryPage >= totalPages(transformationHistory.length)">Siguiente</button>
            </div>
          </div>
          <p *ngIf="!loadingTransformationHistory && transformationHistory.length === 0" style="margin:0;color:#555">Sin transformaciones para el rango seleccionado.</p>
        </section>
        </div>
      </section>

      <app-bo-customers-page *ngIf="isBo() && boSectionKey() === 'clientes'" />

      <section *ngIf="isBo() && boSectionKey() === 'reclamos'" style="border:1px solid #ddd;border-radius:8px;padding:12px;display:flex;flex-direction:column;gap:8px">
        <div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center">
          <select [(ngModel)]="claimsStatus" (ngModelChange)="resetPage('claims')" style="padding:8px">
            <option value="">Todos los estados</option>
            <option value="Open">Abierto</option>
            <option value="PickedUp">Retirado</option>
            <option value="Credited">Acreditado</option>
            <option value="Refunded">Reembolsado</option>
          </select>
          <input [(ngModel)]="claimsFilter" (ngModelChange)="resetPage('claims')" placeholder="Buscar proveedor" style="padding:8px;min-width:220px" />
          <button (click)="loadClaims()" [disabled]="loadingClaims">{{ loadingClaims ? 'Actualizando...' : 'Actualizar reclamos' }}</button>
        </div>
        <p *ngIf="claimsError" style="color:#b3261e;margin:0">{{ claimsError }}</p>
        <table *ngIf="!claimsError" style="width:100%;border-collapse:collapse;font-size:13px">
          <thead>
            <tr>
              <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Reclamo</th>
              <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Proveedor</th>
              <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Estado</th>
              <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Fecha</th>
              <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let c of pagedClaims()">
              <td style="border-bottom:1px solid #f3f3f3;padding:6px">#{{ c.id }}</td>
              <td style="border-bottom:1px solid #f3f3f3;padding:6px">{{ c.supplierName || '-' }}</td>
              <td style="border-bottom:1px solid #f3f3f3;padding:6px">{{ claimStatusLabel(c.status) }}</td>
              <td style="border-bottom:1px solid #f3f3f3;padding:6px">{{ c.createdAt | date:'short' }}</td>
              <td style="border-bottom:1px solid #f3f3f3;padding:6px">
                <button (click)="pickupClaim(c.id)" [disabled]="loadingClaims || !canPickup(c.status)">Retirar</button>
                <button (click)="creditClaim(c.id)" [disabled]="loadingClaims || !canCredit(c.status)">Acreditar</button>
              </td>
            </tr>
          </tbody>
        </table>
        <div *ngIf="!loadingClaims && !claimsError && filteredClaimsRows().length > 0" style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;color:#555;font-size:12px">
          <span>Mostrando {{ pageRangeLabel(filteredClaimsRows().length, claimsPage) }}</span>
          <div style="display:flex;gap:6px;align-items:center">
            <button (click)="prevPage('claims')" [disabled]="claimsPage <= 1">Anterior</button>
            <span>Página {{ claimsPage }}/{{ totalPages(filteredClaimsRows().length) }}</span>
            <button (click)="nextPage('claims', filteredClaimsRows().length)" [disabled]="claimsPage >= totalPages(filteredClaimsRows().length)">Siguiente</button>
          </div>
        </div>
        <p *ngIf="!loadingClaims && filteredClaimsRows().length === 0" style="margin:0;color:#555">No hay reclamos para el filtro aplicado.</p>
      </section>

      <section *ngIf="isBo() && boSectionKey() === 'rrhh'" style="border:1px solid #ddd;border-radius:8px;padding:12px;display:flex;flex-direction:column;gap:8px">
        <div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center">
          <label style="display:flex;gap:6px;align-items:center">Desde <input type="date" [(ngModel)]="rrhhFrom" style="padding:8px" /></label>
          <label style="display:flex;gap:6px;align-items:center">Hasta <input type="date" [(ngModel)]="rrhhTo" style="padding:8px" /></label>
          <button (click)="loadRrhh()" [disabled]="loadingRrhh">{{ loadingRrhh ? 'Actualizando...' : 'Actualizar RRHH' }}</button>
        </div>
        <p *ngIf="rrhhError" style="color:#b3261e;margin:0">{{ rrhhError }}</p>
        <table *ngIf="!rrhhError" style="width:100%;border-collapse:collapse;font-size:13px">
          <thead>
            <tr>
              <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Colaborador</th>
              <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Fecha</th>
              <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Tipo</th>
              <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Descripcion</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let i of pagedRrhhInconsistencies()">
              <td style="border-bottom:1px solid #f3f3f3;padding:6px">{{ i.usuarioName || ('Usuario #' + i.usuarioId) }}</td>
              <td style="border-bottom:1px solid #f3f3f3;padding:6px">{{ i.date | date:'shortDate' }}</td>
              <td style="border-bottom:1px solid #f3f3f3;padding:6px">{{ rrhhTypeLabel(i.inconsistencyType) }}</td>
              <td style="border-bottom:1px solid #f3f3f3;padding:6px">{{ i.description }}</td>
            </tr>
          </tbody>
        </table>
        <div *ngIf="!loadingRrhh && !rrhhError && rrhhInconsistencies.length > 0" style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;color:#555;font-size:12px">
          <span>Mostrando {{ pageRangeLabel(rrhhInconsistencies.length, rrhhPage) }}</span>
          <div style="display:flex;gap:6px;align-items:center">
            <button (click)="prevPage('rrhh')" [disabled]="rrhhPage <= 1">Anterior</button>
            <span>Página {{ rrhhPage }}/{{ totalPages(rrhhInconsistencies.length) }}</span>
            <button (click)="nextPage('rrhh', rrhhInconsistencies.length)" [disabled]="rrhhPage >= totalPages(rrhhInconsistencies.length)">Siguiente</button>
          </div>
        </div>
        <p *ngIf="!loadingRrhh && rrhhInconsistencies.length === 0" style="margin:0;color:#555">Sin inconsistencias en el rango seleccionado.</p>
      </section>

      <section *ngIf="isBo() && boSectionKey() === 'kanban'" style="border:1px solid #ddd;border-radius:8px;padding:12px;display:flex;flex-direction:column;gap:8px">
        <div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center">
          <label style="display:flex;gap:6px;align-items:center"><input type="checkbox" [(ngModel)]="kanbanRequiredOnly" /> Solo obligatorias</label>
          <button (click)="loadKanban()" [disabled]="loadingKanban">{{ loadingKanban ? 'Actualizando...' : 'Actualizar tareas' }}</button>
        </div>
        <p *ngIf="kanbanError" style="color:#b3261e;margin:0">{{ kanbanError }}</p>
        <div *ngFor="let t of pagedKanbanTasks()" style="border:1px solid #eee;border-radius:6px;padding:8px;display:flex;flex-direction:column;gap:6px">
          <div style="display:flex;justify-content:space-between;gap:8px;flex-wrap:wrap">
            <strong>{{ t.title }}</strong>
            <span>{{ kanbanStatusLabel(t.status) }}</span>
          </div>
          <p style="margin:0;color:#555">{{ t.description || 'Sin descripcion' }}</p>
          <small style="color:#666">Checklist: {{ doneChecklistCount(t) }}/{{ t.checklistItems?.length || 0 }} completados</small>
          <button (click)="markKanbanDone(t.id)" [disabled]="loadingKanban || !canMarkTaskDone(t.status)">Marcar completada</button>
        </div>
        <div *ngIf="!loadingKanban && !kanbanError && kanbanTasks.length > 0" style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;color:#555;font-size:12px">
          <span>Mostrando {{ pageRangeLabel(kanbanTasks.length, kanbanPage) }}</span>
          <div style="display:flex;gap:6px;align-items:center">
            <button (click)="prevPage('kanban')" [disabled]="kanbanPage <= 1">Anterior</button>
            <span>Página {{ kanbanPage }}/{{ totalPages(kanbanTasks.length) }}</span>
            <button (click)="nextPage('kanban', kanbanTasks.length)" [disabled]="kanbanPage >= totalPages(kanbanTasks.length)">Siguiente</button>
          </div>
        </div>
        <p *ngIf="!loadingKanban && kanbanTasks.length === 0" style="margin:0;color:#555">Sin tareas para mostrar.</p>
      </section>

      <section *ngIf="isBo()" style="display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:10px">
        <article *ngFor="let card of boCards()" style="border:1px solid #ddd;border-radius:8px;padding:10px;display:flex;flex-direction:column;gap:8px">
          <strong>{{ card.title }}</strong>
          <p style="margin:0;color:#555">{{ card.text }}</p>
          <a [routerLink]="card.to" style="text-decoration:none;color:#1f7f57">{{ card.cta }}</a>
        </article>
      </section>
    </main>
  `,
  styles: [
    `:host{display:block;max-width:100%;overflow-x:hidden}`,
    `.placeholder-wrap{position:relative;padding:24px;font-family:'Montserrat','Segoe UI',sans-serif;display:flex;flex-direction:column;gap:12px;max-width:100%;box-sizing:border-box}`,
    `.placeholder-wrap.stock-view{min-height:100vh;background:linear-gradient(160deg,#fdf7ef 0%,#fff8ee 46%,#f2faf7 100%);padding-bottom:40px;overflow-x:clip}`,
    `.bg-orb{position:absolute;border-radius:999px;filter:blur(4px);pointer-events:none;opacity:.55}`,
    `.orb-a{width:320px;height:320px;right:0;top:80px;transform:translateX(22%);background:radial-gradient(circle,#ffcf9d 0%,rgba(255,207,157,0) 68%)}`,
    `.orb-b{width:300px;height:300px;left:0;top:240px;transform:translateX(-35%);background:radial-gradient(circle,#9fe6c6 0%,rgba(159,230,198,0) 70%)}`,
    `.stock-hero{position:relative;z-index:1;display:flex;justify-content:space-between;align-items:flex-end;gap:16px;background:linear-gradient(120deg,#0e5a42,#247a58 54%,#3a9b75);color:#fff;border-radius:22px;padding:22px 24px;box-shadow:0 18px 40px rgba(22,66,46,.22);width:100%;box-sizing:border-box}`,
    `.stock-hero h1{margin:0;font-size:32px;letter-spacing:.2px}`,
    `.stock-hero p{margin:8px 0 0;color:rgba(255,255,255,.9);max-width:760px}`,
    `.stock-shell{position:relative;background:rgba(255,255,255,.92);border:1px solid rgba(16,94,67,.16)!important;border-radius:22px!important;box-shadow:0 20px 45px rgba(33,73,57,.15);width:100%;max-width:100%;box-sizing:border-box;overflow:hidden}`,
    `.stock-tabs{position:sticky;top:0;z-index:2;display:flex;gap:8px;flex-wrap:wrap;padding:6px 2px 10px;background:linear-gradient(180deg,rgba(255,255,255,.98),rgba(255,255,255,.9));backdrop-filter:blur(4px)}`,
    `.stock-tab{display:inline-flex;align-items:center;gap:10px;border:1px solid #cfe5db;background:#f4fbf8;color:#23644b;border-radius:999px;padding:10px 16px;font-weight:700;font-size:15px;cursor:pointer}`,
    `.stock-tab.active{background:linear-gradient(135deg,#2f8e67,#1f6f50);border-color:#1f6f50;color:#fff;box-shadow:0 8px 16px rgba(31,111,80,.24)}`,
    `.stock-tab-badge{display:inline-flex;align-items:center;justify-content:center;min-width:26px;height:26px;padding:0 8px;border-radius:999px;background:rgba(35,100,75,.15);color:inherit;font-size:12px;font-weight:800}`,
    `.stock-tab.active .stock-tab-badge{background:rgba(255,255,255,.24)}`,
    `.stock-view h3{margin-top:4px;padding-bottom:6px;border-bottom:1px solid #e8f0ec;color:#16553f}`,
    `.stock-view h4{color:#22614a}`,
    `.stock-view input,.stock-view select{border:1px solid #d5e6dd;border-radius:10px;background:#fff;outline:none}`,
    `.stock-view input:focus,.stock-view select:focus{border-color:#2f8e67;box-shadow:0 0 0 3px rgba(47,142,103,.12)}`,
    `.stock-view button{border:1px solid #2c7f5d;background:linear-gradient(135deg,#2f8e67,#1f6f50);color:#fff;border-radius:10px;padding:8px 12px;font-weight:600;cursor:pointer}`,
    `.stock-view button[disabled]{opacity:.6;cursor:not-allowed}`,
    `.claims-section{display:flex;flex-direction:column;gap:18px;font-size:15px}`,
    `.claims-section>h3{font-size:34px;line-height:1.08;letter-spacing:.2px;margin:0;padding-bottom:10px}`,
    `.claims-summary-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:12px}`,
    `.claims-summary-card{border:1px solid #d8e8e1;background:#f5fbf8;border-radius:16px;padding:16px 18px;display:flex;flex-direction:column;gap:8px;min-height:108px;justify-content:center}`,
    `.claims-summary-card p{margin:0;color:#456d5d;font-size:15px;font-weight:700;letter-spacing:.24px;text-transform:uppercase}`,
    `.claims-summary-card strong{font-size:40px;line-height:1;color:#1d5e45}`,
    `.claims-summary-card.is-open{background:#fff7ed;border-color:#f6d3ad}`,
    `.claims-summary-card.is-pickup{background:#eef6ff;border-color:#cbe1fa}`,
    `.claims-summary-card.is-solved{background:#eefaf2;border-color:#c7e9d2}`,
    `.claims-settlement-kpis{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:8px}`,
    `.settlement-kpi{border:1px solid #d3e3db;background:#fff;border-radius:12px;padding:10px 12px;display:flex;justify-content:space-between;align-items:center;gap:8px;cursor:pointer;color:#285744}`,
    `.settlement-kpi span{font-size:13px;font-weight:700}`,
    `.settlement-kpi strong{font-size:22px;line-height:1}`,
    `.settlement-kpi.active{border-color:#2f8e67;background:#ecf7f1;box-shadow:0 0 0 2px rgba(47,142,103,.12)}`,
    `.settlement-kpi.credit{background:#f2f7ff;border-color:#d7e4fa;color:#1b4f8b}`,
    `.settlement-kpi.refund{background:#fff4f3;border-color:#f0d2cf;color:#8b2f2a}`,
    `.settlement-kpi.exchange{background:#f2faf3;border-color:#cfe4d1;color:#285f3e}`,
    `.settlement-kpi.has-pending{box-shadow:0 8px 18px rgba(29,95,70,.08)}`,
    `.settlement-kpi.clear{background:#f8fbfa}`,
    `.claims-priority-alert{display:flex;gap:8px;align-items:center;flex-wrap:wrap;border:1px solid #f0cfc7;background:#fff6f3;color:#8a2f2a;border-radius:12px;padding:10px 12px}`,
    `.claims-priority-alert strong{font-size:14px}`,
    `.claims-priority-alert b{font-weight:800}`,
    `.claims-priority-alert button{margin-left:auto;min-height:34px;padding:6px 12px;border-radius:9px;font-size:13px}`,
    `.claims-workspace{display:grid;grid-template-columns:minmax(520px,1fr) minmax(640px,1.25fr);gap:18px;align-items:start}`,
    `.claims-workspace-single{grid-template-columns:minmax(0,1fr)}`,
    `.claims-panel{border:1px solid #dbe9e2;background:#fff;border-radius:18px;padding:18px;display:flex;flex-direction:column;gap:14px;box-shadow:0 10px 24px rgba(23,73,54,.08)}`,
    `.claims-create-panel{border-color:#cfe2d8;box-shadow:0 14px 28px rgba(23,73,54,.1)}`,
    `.claims-panel-head{display:flex;flex-direction:column;gap:2px}`,
    `.claims-panel-head h4{margin:0;color:#1d5f46;font-size:28px;line-height:1.08}`,
    `.claims-panel-head small{color:#507666;font-size:15px}`,
    `.claims-form-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(280px,1fr));gap:14px}`,
    `.claims-field{display:flex;flex-direction:column;gap:7px;color:#224e3d;font-weight:700;font-size:17px}`,
    `.claim-policy-hint{margin:0;padding:10px 12px;border:1px dashed #c9ddd3;border-radius:10px;background:#f7fcf9;color:#3c6756;font-size:14px}`,
    `.claims-panel input,.claims-panel select{min-height:54px;padding:12px 14px;font-size:17px}`,
    `.claim-field-full{grid-column:1 / -1}`,
    `.claims-inline-errors{border:1px solid #efc8c5;background:#fff6f5;color:#9f2f28;border-radius:14px;padding:14px;font-size:16px}`,
    `.claims-preview{display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;border:1px solid #d3e8dd;background:#f2faf6;border-radius:14px;padding:14px;color:#245b45;font-size:16px}`,
    `.claims-preview strong{font-size:19px}`,
    `.claims-submit{width:100%;min-height:60px;font-size:22px;font-weight:800}`,
    `.claims-filters-row{display:grid;grid-template-columns:180px 220px minmax(260px,1fr) auto;gap:10px;align-items:center}`,
    `.claims-top-actions{display:flex;justify-content:space-between;gap:10px;flex-wrap:wrap;align-items:center}`,
    `.claims-open-modal{min-height:46px;padding:10px 18px;font-size:15px}`,
    `.claims-top-actions span{color:#416a59;font-size:14px}`,
    `.claim-policy-admin{border:1px solid #d8e9e1;background:#f8fcfa;border-radius:14px;padding:12px;display:flex;flex-direction:column;gap:8px}`,
    `.claim-policy-admin header{display:flex;flex-direction:column;gap:2px}`,
    `.claim-policy-admin h5{margin:0;color:#1d5f46;font-size:20px}`,
    `.claim-policy-admin small{color:#4f7364}`,
    `.claim-policy-grid{display:grid;grid-template-columns:2fr 1.5fr 2fr auto;gap:10px;align-items:end}`,
    `.policy-check{display:flex;flex-direction:row;gap:8px;align-items:center;font-size:14px}`,
    `.policy-check input{min-height:18px;min-width:18px}`,
    `.claim-policy-save{min-height:44px;padding:8px 14px}`,
    `.claim-policy-message{margin:0;color:#2b6450;font-size:13px}`,
    `.claims-error{margin:0;color:#b3261e}`,
    `.claim-status{display:inline-flex;align-items:center;padding:6px 12px;border-radius:999px;font-size:13px;font-weight:800}`,
    `.claim-status-open{background:#fff2df;color:#8b4b0e}`,
    `.claim-status-pickup{background:#e8f1ff;color:#1d4b86}`,
    `.claim-status-solved{background:#e8f7ee;color:#1d5f3d}`,
    `.claim-status-default{background:#edf2ef;color:#355748}`,
    `.claim-settlement-chip{display:inline-flex;align-items:center;border-radius:999px;padding:5px 10px;font-size:12px;font-weight:700;white-space:nowrap}`,
    `.claim-settlement-credit{background:#eaf4ff;color:#1b4f8b}`,
    `.claim-settlement-refund{background:#fff1f0;color:#8b2f2a}`,
    `.claim-settlement-exchange{background:#eef8ee;color:#2a6540}`,
    `.claims-main-table .col-actions{width:1%;white-space:nowrap}`,
    `.claim-actions-cell{display:flex;gap:8px;flex-wrap:wrap;align-items:center}`,
    `.claim-actions-cell button{min-height:44px;padding:10px 15px;font-size:15px}`,
    `.claims-main-table .claim-actions-cell{gap:6px}`,
    `.claim-row-action{min-height:36px!important;padding:6px 12px!important;font-size:13px!important;border-radius:10px!important;font-weight:700!important}`,
    `.claims-main-table .claim-row-action{min-width:132px;display:inline-flex;align-items:center;justify-content:center}`,
    `.claim-row-action.is-primary{background:linear-gradient(135deg,#2f8e67,#247555)!important;border-color:#247555!important}`,
    `.claims-main-table .claim-link{padding:6px 10px;font-size:12px;border-radius:10px}`,
    `.claim-link-text{border:none;background:transparent;padding:0;color:#1e5f47;font-size:inherit;font-weight:700;cursor:pointer;text-align:left}`,
    `.claim-link-text:hover{text-decoration:underline}`,
    `.claim-detail-btn{background:#e8f4ef;border:1px solid #cde0d6;color:#1d5f46}`,
    `.claim-evidence-chip{display:inline-flex;align-items:center;border-radius:999px;background:#ebf4f0;color:#1d5f46;padding:4px 10px;font-size:12px;font-weight:700}`,
    `.claim-link{display:inline-flex;align-items:center;text-decoration:none;color:#1d5f46;border:1px solid #cde0d6;background:#f3faf7;border-radius:999px;padding:7px 11px;font-size:12px;font-weight:700}`,
    `.claim-link:hover{background:#e8f4ef}`,
    `.claim-action-muted{font-size:13px;color:#6f857b}`,
    `.claims-cards-mobile{display:none;gap:8px}`,
    `.claim-card{border:1px solid #dce9e3;border-radius:14px;padding:14px;background:#fff;display:flex;flex-direction:column;gap:10px}`,
    `.claim-card-head{display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap}`,
    `.claim-card p{margin:0;color:#385c4d;font-size:14px}`,
    `.claim-modal-overlay{position:fixed;inset:0;background:rgba(10,20,16,.52);display:grid;place-items:center;padding:16px;z-index:180}`,
    `.claim-modal{width:min(1160px,97vw);max-height:92vh;overflow:auto;background:#fff;border:1px solid #dcebe3;border-radius:20px;padding:24px;display:flex;flex-direction:column;gap:16px;box-shadow:0 26px 54px rgba(20,62,46,.3)}`,
    `.claim-modal-head{display:flex;justify-content:space-between;gap:12px;align-items:flex-start;flex-wrap:wrap}`,
    `.claim-modal-head h4{margin:0;color:#1e5f47;font-size:34px;line-height:1.08}`,
    `.claim-modal-head p{margin:8px 0 0;color:#4f7364;font-size:17px}`,
    `.claim-modal-close{min-height:44px;padding:9px 16px;font-size:16px;background:#eef6f2;color:#1d5f46;border:1px solid #cfe2d8}`,
    `.claim-modal .claims-form-grid{grid-template-columns:repeat(auto-fit,minmax(300px,1fr));gap:14px;align-items:end}`,
    `.claim-modal .claims-field{font-size:16px}`,
    `.claim-modal input,.claim-modal select{width:100%;min-height:50px;padding:11px 12px;font-size:16px;box-sizing:border-box}`,
    `.claim-lines-head{display:flex;justify-content:space-between;gap:10px;align-items:center;flex-wrap:wrap}`,
    `.claim-lines-head strong{font-size:28px;color:#1e5f47;line-height:1.1}`,
    `.claim-add-row{min-height:46px;padding:9px 16px;font-size:16px;background:#ebf5f0;color:#1d5f46;border:1px solid #c9dfd4}`,
    `.claim-line-grid{display:grid;grid-template-columns:minmax(220px,2fr) minmax(140px,1fr) minmax(150px,1fr) minmax(240px,2fr) auto;gap:12px;align-items:end;border:1px solid #dceae3;border-radius:14px;padding:16px;background:#fbfefd}`,
    `.claim-line-grid>.claims-field{min-width:0}`,
    `.claim-remove-row{min-height:44px;padding:9px 12px;font-size:15px;background:#fff3f3;color:#8a2a2a;border:1px solid #f0c7c7}`,
    `.claim-evidence-block{display:flex;flex-direction:column;gap:10px;border:1px dashed #b8d4c6;border-radius:14px;padding:16px;background:#f7fcf9}`,
    `.claim-file-list{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:8px}`,
    `.claim-file-item{display:flex;justify-content:space-between;align-items:center;gap:8px;border:1px solid #d9e8e1;background:#fff;border-radius:11px;padding:10px 12px}`,
    `.claim-file-item span{font-size:14px;color:#385d4f;word-break:break-word}`,
    `.claim-file-item button{min-height:38px;padding:7px 12px;font-size:14px}`,
    `.claim-modal-actions{display:flex;justify-content:flex-end;gap:12px;flex-wrap:wrap;align-items:stretch;padding-top:4px}`,
    `.claim-modal-actions .btn-secondary{min-width:180px;min-height:56px;padding:0 18px;border-radius:14px;font-size:19px;font-weight:700;display:inline-flex;align-items:center;justify-content:center;line-height:1}`,
    `.claim-modal-actions .claims-submit{width:auto;min-width:340px;min-height:56px;padding:0 22px;border-radius:14px;font-size:20px;font-weight:800;display:inline-flex;align-items:center;justify-content:center;line-height:1}`,
    `.claim-detail-backdrop{position:fixed;inset:0;z-index:190;background:rgba(12,26,20,.45);display:grid;place-items:center;padding:16px}`,
    `.claim-detail-drawer{width:min(980px,96vw);max-height:92vh;background:#f8fcfa;border:1px solid #cde0d6;border-radius:18px;box-shadow:0 22px 44px rgba(12,36,26,.24);padding:18px;display:flex;flex-direction:column;gap:12px;overflow:auto}`,
    `.claim-detail-head{display:flex;justify-content:space-between;align-items:flex-start;gap:10px;position:sticky;top:0;background:#f8fcfa;padding-bottom:8px;z-index:3}`,
    `.claim-detail-head h4{margin:0;color:#1c5d45;font-size:30px}`,
    `.claim-detail-head p{margin:4px 0 0;color:#4d7061}`,
    `.claim-detail-loading{padding:12px;border:1px solid #d8e8e1;background:#eef8f3;border-radius:12px;color:#2c604c;font-weight:700}`,
    `.claim-detail-body{display:flex;flex-direction:column;gap:12px}`,
    `.claim-detail-top{display:flex;gap:8px;flex-wrap:wrap;color:#365d4e;font-size:14px}`,
    `.claim-detail-card{border:1px solid #dbe9e2;background:#fff;border-radius:14px;padding:12px;display:flex;flex-direction:column;gap:8px}`,
    `.claim-detail-card h5{margin:0;color:#1d5f46;font-size:22px}`,
    `.claim-detail-card p{margin:0;color:#365d4e}`,
    `.claim-timeline{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:10px}`,
    `.claim-step{display:flex;gap:8px;align-items:flex-start;border:1px solid #d8e6df;background:#f8fcfa;border-radius:12px;padding:10px}`,
    `.claim-step .dot{width:12px;height:12px;border-radius:999px;background:#c2d8cd;margin-top:4px;flex:0 0 12px}`,
    `.claim-step strong{display:block;color:#2c5948;font-size:14px}`,
    `.claim-step small{display:block;color:#5f7a6d}`,
    `.claim-step.is-active{border-color:#9bcbb3;background:#eff8f3}`,
    `.claim-step.is-active .dot{background:#2f8e67;box-shadow:0 0 0 3px rgba(47,142,103,.14)}`,
    `.claim-step.is-done{border-color:#a7d6bf;background:#edf8f2}`,
    `.claim-step.is-done .dot{background:#2f8e67}`,
    `.claim-detail-table{width:100%;border-collapse:collapse;min-width:620px}`,
    `.claim-detail-table th,.claim-detail-table td{padding:8px;border-bottom:1px solid #ecf3ef;text-align:left}`,
    `.claim-detail-table th{background:#f4faf7;color:#1f6048}`,
    `.claim-detail-evidence-head{display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap}`,
    `.claim-detail-evidence-head span{background:#edf6f2;border:1px solid #d5e7df;border-radius:999px;padding:4px 10px;color:#245b45;font-size:12px;font-weight:700}`,
    `.claim-evidence-gallery{display:grid;grid-template-columns:repeat(auto-fill,minmax(180px,1fr));gap:8px}`,
    `.claim-evidence-thumb{border:1px solid #dbe8e2;border-radius:12px;padding:8px;background:#fff;display:flex;flex-direction:column;gap:6px}`,
    `.claim-evidence-thumb img{width:100%;height:130px;object-fit:cover;border-radius:8px;border:1px solid #dbe8e2;background:#f3f7f5}`,
    `.claim-evidence-thumb strong{font-size:13px;color:#214f3e;word-break:break-word}`,
    `.claim-evidence-thumb small{color:#587766}`,
    `.claim-evidence-actions{display:flex;gap:6px;flex-wrap:wrap}`,
    `.claim-evidence-actions button,.claim-evidence-actions a{border:1px solid #cfe2d8;background:#eff7f3;color:#1d5f46;border-radius:10px;padding:6px 10px;font-size:12px;font-weight:700;text-decoration:none}`,
    `.claim-empty-evidence{color:#5d796c}`,
    `.claim-image-viewer{position:fixed;inset:0;z-index:200;background:rgba(8,18,14,.72);display:grid;place-items:center;padding:16px}`,
    `.claim-image-content{width:min(1040px,96vw);max-height:92vh;background:#fff;border:1px solid #d6e6de;border-radius:16px;padding:12px;display:flex;flex-direction:column;gap:10px}`,
    `.claim-image-content header{display:flex;justify-content:space-between;gap:10px;align-items:center}`,
    `.claim-image-content img{width:100%;max-height:72vh;object-fit:contain;background:#f2f7f4;border-radius:12px}`,
    `.claim-image-content footer{display:flex;justify-content:space-between;gap:8px;flex-wrap:wrap}`,
    `.claim-image-content footer a{border:1px solid #cde0d6;background:#f3faf7;color:#1d5f46;border-radius:10px;padding:8px 12px;font-weight:700;text-decoration:none}`,
    `.transformations-section{display:flex;flex-direction:column;gap:14px}`,
    `.transformations-header{display:flex;flex-direction:column;gap:4px}`,
    `.transformations-header h3{margin:0;color:#185c44;font-size:34px}`,
    `.transformations-header p{margin:0;color:#4f7464;font-size:17px}`,
    `.transformations-layout{display:grid;grid-template-columns:minmax(520px,1.2fr) minmax(340px,.8fr);gap:12px;align-items:start}`,
    `.transform-card{border:1px solid #d9e9e2;background:#fff;border-radius:16px;padding:14px;display:flex;flex-direction:column;gap:10px;box-shadow:0 8px 18px rgba(24,86,63,.06)}`,
    `.transform-card-head{display:flex;flex-direction:column;gap:2px}`,
    `.transform-card-head h4{margin:0;color:#1f6048;font-size:28px;line-height:1.08}`,
    `.transform-card-head small{color:#57796a;font-size:14px}`,
    `.transform-form-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:10px;align-items:end}`,
    `.transform-policy-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:10px;align-items:end}`,
    `.transform-policy-note{margin:0;color:#5b776a;font-size:13px}`,
    `.policy-check-inline{display:flex;flex-direction:row;align-items:center;gap:8px;font-size:13px}`,
    `.policy-check-inline input{min-width:18px;min-height:18px}`,
    `.transform-field{display:flex;flex-direction:column;gap:5px;color:#23523f;font-size:14px;font-weight:700}`,
    `.transform-field-full{grid-column:1 / -1}`,
    `.transform-field input,.transform-field select{min-height:44px;padding:10px 12px;font-size:15px}`,
    `.transform-card-actions{display:flex;justify-content:flex-end}`,
    `.transform-card-actions button{min-height:42px;padding:8px 14px}`,
    `.transform-apply-card{background:linear-gradient(180deg,#ffffff,#f7fcfa)}`,
    `.transform-apply-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px}`,
    `.transform-suggestion{display:flex;gap:8px;flex-wrap:wrap;align-items:center;border:1px dashed #c9ddd3;background:#f7fcf9;border-radius:10px;padding:8px 10px;color:#2e5d4a;font-size:13px}`,
    `.transform-preview{display:flex;justify-content:space-between;gap:8px;flex-wrap:wrap;border:1px solid #d2e7dc;background:#eef9f3;border-radius:12px;padding:10px;color:#245c45}`,
    `.transform-preview strong{font-size:16px}`,
    `.transform-main-cta{min-height:48px;font-size:18px;font-weight:800}`,
    `.transform-feedback{margin:0;border-radius:10px;padding:10px 12px;font-size:14px}`,
    `.transform-feedback.error{border:1px solid #efc8c5;background:#fff6f5;color:#9f2f28}`,
    `.transform-feedback.success{border:1px solid #b8dec8;background:#effaf3;color:#17683f}`,
    `.transform-table{width:100%;border-collapse:collapse;font-size:14px}`,
    `.transform-table th{padding:9px;border-bottom:1px solid #e8f1ed;text-align:left;color:#1f6048;background:#f4faf7}`,
    `.transform-table td{padding:9px;border-bottom:1px solid #eef4f1;color:#315649}`,
    `.transform-use-btn{min-height:34px;padding:6px 12px;font-size:13px}`,
    `.transform-row-actions{display:flex;gap:6px;flex-wrap:wrap}`,
    `.transform-reject-btn{min-height:34px;padding:6px 12px;font-size:13px;background:#fff3f3;border-color:#e8bcbc;color:#8b2f2a}`,
    `.transform-history-filters{display:grid;grid-template-columns:repeat(2,minmax(0,1fr)) auto;gap:10px;align-items:end}`,
    `.claims-panel table{font-size:15px !important}`,
    `.claims-panel table th,.claims-panel table td{padding:13px 10px !important}`,
    `.stock-view table{background:#fff;border-radius:12px;overflow:hidden}`,
    `.stock-scroll{width:100%;max-width:100%;overflow:auto}`,
    `.stock-scroll{scrollbar-width:thin;scrollbar-color:#2f8e67 #dfece6}`,
    `.stock-scroll::-webkit-scrollbar{height:10px;width:10px}`,
    `.stock-scroll::-webkit-scrollbar-track{background:#dfece6;border-radius:999px}`,
    `.stock-scroll::-webkit-scrollbar-thumb{background:linear-gradient(180deg,#40a179,#2f8e67);border-radius:999px;border:2px solid #dfece6}`,
    `.stock-scroll::-webkit-scrollbar-thumb:hover{background:linear-gradient(180deg,#2f8e67,#256f52)}`,
    `@media (max-width: 1320px){.claims-workspace{grid-template-columns:1fr}.claims-filters-row{grid-template-columns:1fr 1fr minmax(220px,1fr) auto}.claim-policy-grid{grid-template-columns:1fr 1fr}.claim-policy-save{width:fit-content}.transformations-layout{grid-template-columns:1fr}.transform-history-filters{grid-template-columns:1fr 1fr auto}.claim-modal{width:min(1080px,96vw)}.claim-line-grid{grid-template-columns:1fr 1fr 1fr 1fr auto}}`,
    `@media (max-width: 900px){.stock-hero h1{font-size:25px}.stock-hero{padding:16px 18px}.placeholder-wrap.stock-view{padding:16px}.stock-tabs{overflow:auto;flex-wrap:nowrap;padding-bottom:12px}.stock-tab{white-space:nowrap;font-size:14px;padding:9px 14px}.claims-section>h3{font-size:30px}.claims-panel{padding:14px}.claims-panel-head h4{font-size:28px}.claims-panel-head small{font-size:16px}.claims-form-grid{grid-template-columns:1fr}.claims-field{font-size:15px}.claims-panel input,.claims-panel select{min-height:48px;font-size:15px}.claim-policy-grid{grid-template-columns:1fr}.claim-policy-save{width:100%}.claims-filters-row{grid-template-columns:1fr}.claims-table-desktop{display:none}.claims-cards-mobile{display:flex;flex-direction:column}.transformations-header h3{font-size:28px}.transform-card{padding:12px}.transform-card-head h4{font-size:22px}.transform-form-grid,.transform-apply-grid,.transform-history-filters{grid-template-columns:1fr}.transform-main-cta{font-size:17px}.claim-modal{padding:16px}.claim-modal-head h4{font-size:30px}.claim-modal-head p{font-size:16px}.claim-lines-head strong{font-size:24px}.claim-line-grid{grid-template-columns:1fr}.claim-modal-actions .btn-secondary{width:100%;min-width:0;font-size:18px}.claim-modal-actions .claims-submit{width:100%;min-width:0;font-size:20px}.claim-detail-backdrop{padding:0}.claim-detail-drawer{width:100vw;max-height:100vh;border-radius:0;padding:14px}.claim-detail-head h4{font-size:24px}.claim-timeline{grid-template-columns:1fr}.claim-detail-table{min-width:520px}.claim-image-content{padding:10px}}`
  ]
})
export class PlaceholderPageComponent implements OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);
  readonly title = computed(() => (this.route.snapshot.data['title'] as string) ?? 'Pantalla');
  readonly isPos = computed(() => this.router.url.startsWith('/pos/'));
  readonly isBo = computed(() => this.router.url.startsWith('/bo/'));
  readonly boSectionKey = computed(() => {
    const url = this.router.url.toLowerCase();
    if (url.includes('/bo/productos')) return 'productos';
    if (url.includes('/bo/stock')) return 'stock';
    if (url.includes('/bo/clientes')) return 'clientes';
    if (url.includes('/bo/claims')) return 'reclamos';
    if (url.includes('/bo/rrhh')) return 'rrhh';
    if (url.includes('/bo/kanban')) return 'kanban';
    return 'general';
  });

  readonly emptyText = computed(() => {
    const key = this.boSectionKey();
    if (this.isBo()) {
      if (key === 'productos') return 'Vista operativa minima para altas de catalogo, precios y stock inicial.';
      if (key === 'stock') return 'Vista operativa mínima para control de stock crítico y ajustes.';
      if (key === 'clientes') return 'Vista operativa minima para gestion de clientes y cuenta corriente.';
      if (key === 'reclamos') return 'Vista operativa mínima para reclamos y créditos de proveedor.';
      if (key === 'rrhh') return 'Vista operativa minima para fichadas, extras e inconsistencias.';
      if (key === 'kanban') return 'Vista operativa minima para tareas de turno y checklist.';
    }

    const t = this.title().toLowerCase();
    if (t.includes('venta')) return 'Pantalla de venta en preparacion para el modo operativo activo.';
    if (t.includes('cobro')) return 'Pantalla de cobro en preparacion para el modo operativo activo.';
    if (t.includes('cierre')) return 'Pantalla de cierre en preparacion para el modo operativo activo.';
    return 'Módulo operativo mínimo activo. Usá las acciones sugeridas para continuar.';
  });

  readonly boCards = computed(() => {
    const key = this.boSectionKey();
    if (key === 'productos') {
      return [
        { title: 'Carga inicial', text: 'Importa catalogo masivo y valida errores antes de confirmar.', cta: 'Abrir importaciones', to: '/bo/importaciones' },
        { title: 'Stock de arranque', text: 'Carga stock inicial por local para salir a operar.', cta: 'Abrir stock inicial', to: '/bo/importaciones/stock-inicial' },
        { title: 'Control gerencial', text: 'Revisa reportes y exporta datos para auditoria.', cta: 'Abrir exportaciones', to: '/bo/exportaciones' }
      ];
    }

    if (key === 'stock') {
      return [
        { title: 'Stock inicial', text: 'Completa o corrige stock de apertura del local.', cta: 'Ir a stock inicial', to: '/bo/importaciones/stock-inicial' },
        { title: 'Exportes de control', text: 'Descarga stock y ventas para control diario.', cta: 'Ir a exportaciones', to: '/bo/exportaciones' },
        { title: 'Operación diaria', text: 'Usa checklist para validar apertura/cierre.', cta: 'Ir a checklist', to: '/bo/operacion/checklist' }
      ];
    }

    if (key === 'clientes') {
      return [
        { title: 'Cobros en caja', text: 'Gestiona pagos de cuenta corriente desde POS.', cta: 'Abrir pago de cuenta', to: '/pos/caja/pago-cuenta' },
        { title: 'Reportes', text: 'Exporta informacion para seguimiento de deuda.', cta: 'Abrir exportaciones', to: '/bo/exportaciones' },
        { title: 'Capacitación', text: 'Repasa flujo comercial para operador y encargado.', cta: 'Abrir capacitación', to: '/bo/capacitacion' }
      ];
    }

    if (key === 'reclamos') {
      return [
        { title: 'Compras', text: 'Revisa y crea borradores de compra para reposición.', cta: 'Abrir compras sugeridas', to: '/bo/compras/sugeridas' },
        { title: 'Descargas operativas', text: 'Accede a material de contingencia y soporte.', cta: 'Abrir descargas', to: '/bo/operacion/descargas' },
        { title: 'Control de cierres', text: 'Verifica tareas pendientes por turno.', cta: 'Abrir checklist', to: '/bo/operacion/checklist' }
      ];
    }

    if (key === 'rrhh') {
      return [
        { title: 'Inconsistencias', text: 'Revisa fichadas y desvios de jornada desde API RRHH.', cta: 'Abrir dashboard', to: '/bo/dashboard' },
        { title: 'Puesta en marcha', text: 'Asegura alta de usuarios y roles operativos.', cta: 'Abrir puesta en marcha', to: '/bo/operacion/puesta-en-marcha' },
        { title: 'Formación', text: 'Capacita personal por rol y valida adopción.', cta: 'Abrir capacitación', to: '/bo/capacitacion' }
      ];
    }

    if (key === 'kanban') {
      return [
        { title: 'Tareas de turno', text: 'Gestiona tareas obligatorias para habilitar cierre.', cta: 'Abrir checklist', to: '/bo/operacion/checklist' },
        { title: 'Onboarding', text: 'Usa alta guiada para habilitar flujo completo.', cta: 'Abrir onboarding', to: '/bo/onboarding' },
        { title: 'Control local', text: 'Revisa estado del sistema y contingencia.', cta: 'Abrir descargas', to: '/bo/operacion/descargas' }
      ];
    }

    return [
      { title: 'Lo diario', text: 'Monitorea indicadores clave del local.', cta: 'Ir al dashboard', to: '/bo/dashboard' },
      { title: 'Compras', text: 'Planifica reposición desde sugerencias.', cta: 'Ir a compras sugeridas', to: '/bo/compras/sugeridas' },
      { title: 'Reportes', text: 'Descarga reportes para seguimiento.', cta: 'Ir a exportaciones', to: '/bo/exportaciones' }
    ];
  });

  stockRows: any[] = [];
  stockFilter = '';
  stockPage = 1;
  loadingStock = false;
  stockError = '';
  stockMovements: any[] = [];
  stockMovementsPage = 1;
  loadingStockMovements = false;
  stockMovementsError = '';
  movementsProductId: number | null = null;
  movementsType = '';
  movementsFrom = '';
  movementsTo = '';
  adjustmentProductId: number | null = null;
  adjustmentBucket = 'VENDIBLE';
  adjustmentDeltaQty = '';
  adjustmentNotes = '';
  loadingAdjustment = false;
  adjustmentError = '';
  adjustmentMessage = '';
  claimSupplierId: number | null = null;
  claimResolutionType: 'credit' | 'refund' | 'exchange' = 'credit';
  claimNotes = '';
  isClaimModalOpen = false;
  claimItems: Array<{ productId: number | null; quantity: string; unitCost: string; notes: string }> = [
    { productId: null, quantity: '', unitCost: '', notes: '' }
  ];
  claimEvidenceFiles: File[] = [];
  supplierOptions: Array<{ id: number; name: string; claimSettlementModeDefault?: string; allowClaimSettlementOverride?: boolean }> = [];
  policySupplierId: number | null = null;
  policySettlementMode: 'credit' | 'refund' | 'exchange' = 'credit';
  policyAllowOverride = false;
  savingPolicy = false;
  policyMessage = '';
  transformationTemplates: any[] = [];
  transformationSupplierId: number | null = null;
  transformationSourceProductId: number | null = null;
  transformationTargetProductId: number | null = null;
  transformationYieldFactor = '';
  transformationNotes = '';
  loadingTransformations = false;
  transformationsError = '';
  transformationsMessage = '';
  transformationSourceQty = '';
  transformationTargetQty = '';
  transformationSuggestion: { suggestedYieldFactor: number; confidence: string; sampleCount: number; source: string } | null = null;
  loadingTransformationSuggestion = false;
  transformationYieldFactorEdited = false;
  transformationSubmitAttempted = false;
  transformationYieldPolicy = {
    autoUpdateEnabled: false,
    requireAdminApproval: true,
    minSampleCount: 12,
    maxVolatility: 0.12,
    maxDeviationPct: 15,
    minDeviationPct: 3
  };
  loadingTransformationPolicy = false;
  transformationPolicyMessage = '';
  transformationRecalibrations: any[] = [];
  loadingTransformationRecalibrations = false;
  transformationRecalibrationsError = '';
  transformationHistory: any[] = [];
  transformationHistoryFrom = '';
  transformationHistoryTo = '';
  loadingTransformationHistory = false;
  transformationHistoryError = '';
  transformationHistoryPage = 1;

  claims: any[] = [];
  claimsFilter = '';
  claimsStatus = '';
  claimsSettlementFilter: '' | 'credit' | 'refund' | 'exchange' = '';
  claimsPage = 1;
  loadingClaims = false;
  claimsError = '';
  isClaimDetailOpen = false;
  loadingClaimDetail = false;
  claimDetailError = '';
  selectedClaimDetail: any | null = null;
  isEvidenceViewerOpen = false;
  evidenceViewerItems: any[] = [];
  evidenceViewerIndex = 0;
  isExchangeResolveModalOpen = false;
  exchangeResolveClaimId: number | null = null;
  exchangeResolveLines: Array<{ productId: number | null; quantity: string; unitCost: string; notes: string }> = [
    { productId: null, quantity: '', unitCost: '', notes: '' }
  ];
  exchangeResolveError = '';

  rrhhFrom = '';
  rrhhTo = '';
  rrhhInconsistencies: any[] = [];
  rrhhPage = 1;
  loadingRrhh = false;
  rrhhError = '';

  kanbanTasks: any[] = [];
  kanbanRequiredOnly = true;
  kanbanPage = 1;
  loadingKanban = false;
  kanbanError = '';
  readonly stockModules: Array<{ id: StockModuleId; label: string }> = [
    { id: 'summary', label: 'Resumen' },
    { id: 'adjustment', label: 'Ajuste manual' },
    { id: 'movements', label: 'Movimientos' },
    { id: 'claims', label: 'Reclamos proveedor' },
    { id: 'transformations', label: 'Transformaciones' }
  ];
  activeStockModule: StockModuleId = 'summary';
  private readonly pageSize = 12;

  constructor() {
    const section = this.boSectionKey();
    if (section === 'stock') {
      const savedModule = localStorage.getItem('bo_stock_active_module') as StockModuleId | null;
      if (savedModule && this.stockModules.some(m => m.id === savedModule)) {
        this.activeStockModule = savedModule;
      }
      void this.loadStock();
      void this.loadStockMovements();
      void this.loadClaims();
      void this.loadSuppliers();
      void this.loadTransformationTemplates();
      void this.loadTransformationYieldPolicy();
      void this.loadTransformationRecalibrations();
      void this.loadTransformationHistory();
    }
    if (section === 'reclamos') {
      void this.loadClaims();
    }
    if (section === 'rrhh') {
      void this.loadRrhh();
    }
    if (section === 'kanban') {
      void this.loadKanban();
    }
  }

  setStockModule(module: StockModuleId): void {
    this.activeStockModule = module;
    localStorage.setItem('bo_stock_active_module', module);

    if (module === 'summary' && this.stockRows.length === 0 && !this.loadingStock) {
      void this.loadStock();
    }
    if (module === 'movements' && this.stockMovements.length === 0 && !this.loadingStockMovements) {
      void this.loadStockMovements();
    }
    if (module === 'claims' && this.claims.length === 0 && !this.loadingClaims) {
      void this.loadClaims();
    }
    if (module === 'transformations') {
      if (this.transformationTemplates.length === 0 && !this.loadingTransformations) {
        void this.loadTransformationTemplates();
      }
      if (!this.loadingTransformationPolicy) {
        void this.loadTransformationYieldPolicy();
      }
      if (!this.loadingTransformationRecalibrations) {
        void this.loadTransformationRecalibrations();
      }
      if (this.transformationHistory.length === 0 && !this.loadingTransformationHistory) {
        void this.loadTransformationHistory();
      }
      if (this.transformationSourceProductId && this.transformationTargetProductId) {
        void this.loadTransformationSuggestion();
      }
    }
  }

  stockModuleBadge(module: StockModuleId): number {
    if (module === 'summary') return this.filteredStockRows().length;
    if (module === 'movements') return this.stockMovements.length;
    if (module === 'claims') return this.claims.filter(c => `${c?.status ?? ''}`.toLowerCase() === 'pending').length;
    if (module === 'transformations') {
      return this.transformationRecalibrations.length > 0
        ? this.transformationRecalibrations.length
        : this.transformationTemplates.length;
    }
    return 0;
  }

  async loadStock(): Promise<void> {
    this.loadingStock = true;
    this.stockError = '';
    try {
      const response: any = await firstValueFrom(this.http.get('/api/v1/stock/report'));
      const balances = Array.isArray(response?.balances) ? response.balances : [];
      this.stockRows = balances.map((b: any) => ({
        productId: b.productId,
        productName: b.productName,
        vendibleQty: Number(b.vendibleQty ?? b.vendible ?? 0),
        reclamoQty: Number(b.reclamoQty ?? b.reclamo ?? 0),
        mermaQty: Number(b.mermaQty ?? b.merma ?? 0),
        totalQty: Number((b.vendibleQty ?? b.vendible ?? 0) + (b.reclamoQty ?? b.reclamo ?? 0) + (b.mermaQty ?? b.merma ?? 0))
      }));
      if (!this.adjustmentProductId && this.stockRows.length > 0) {
        this.adjustmentProductId = this.stockRows[0].productId;
      }
      this.stockPage = 1;
    } catch (err: any) {
      this.stockError = this.formatError(err, 'No se pudo cargar stock');
    } finally {
      this.loadingStock = false;
    }
  }

  filteredStockRows(): any[] {
    const q = this.stockFilter.trim().toLowerCase();
    if (!q) return this.stockRows;
    return this.stockRows.filter(r => `${r.productName ?? ''}`.toLowerCase().includes(q));
  }

  pagedStock(): any[] {
    return this.paginate(this.filteredStockRows(), this.stockPage);
  }

  async loadStockMovements(): Promise<void> {
    this.loadingStockMovements = true;
    this.stockMovementsError = '';
    try {
      const params: string[] = [];
      if (this.movementsProductId != null) params.push(`productId=${encodeURIComponent(String(this.movementsProductId))}`);
      if (this.movementsType) params.push(`movementType=${encodeURIComponent(this.movementsType)}`);
      if (this.movementsFrom) params.push(`from=${encodeURIComponent(this.movementsFrom)}`);
      if (this.movementsTo) params.push(`to=${encodeURIComponent(this.movementsTo)}`);
      const query = params.length ? `?${params.join('&')}` : '';
      const rows: any = await firstValueFrom(this.http.get(`/api/v1/stock/movements${query}`));
      this.stockMovements = Array.isArray(rows) ? rows : [];
      this.stockMovementsPage = 1;
    } catch (err: any) {
      this.stockMovementsError = this.formatError(err, 'No se pudieron cargar movimientos de stock');
    } finally {
      this.loadingStockMovements = false;
    }
  }

  pagedStockMovements(): any[] {
    return this.paginate(this.stockMovements, this.stockMovementsPage);
  }

  stockMovementTypeLabel(kind: string): string {
    const k = `${kind ?? ''}`.toLowerCase();
    if (k === 'initial') return 'Inicial';
    if (k === 'purchase') return 'Compra';
    if (k === 'sale') return 'Venta';
    if (k === 'supplierclaim') return 'Reclamo proveedor';
    if (k === 'waste') return 'Merma';
    if (k === 'adjustment') return 'Ajuste';
    if (k === 'transformation') return 'Transformación';
    return kind || '-';
  }

  async applyStockAdjustment(): Promise<void> {
    this.adjustmentError = '';
    this.adjustmentMessage = '';

    if (!this.adjustmentProductId) {
      this.adjustmentError = 'Selecciona un producto para ajustar.';
      return;
    }

    const delta = Number(this.adjustmentDeltaQty);
    if (!Number.isFinite(delta) || delta === 0) {
      this.adjustmentError = 'Ingresa un delta valido distinto de 0.';
      return;
    }

    const notes = this.adjustmentNotes.trim();
    if (!notes) {
      this.adjustmentError = 'Ingresa un motivo para el ajuste.';
      return;
    }

    this.loadingAdjustment = true;
    try {
      await firstValueFrom(this.http.post('/api/v1/stock/movement', {
        productId: this.adjustmentProductId,
        bucket: this.adjustmentBucket,
        deltaQty: delta,
        movementType: 'Adjustment',
        notes
      }));

      this.adjustmentDeltaQty = '';
      this.adjustmentNotes = '';
      this.adjustmentMessage = 'Ajuste aplicado correctamente.';
      await this.refreshStockSection();
    } catch (err: any) {
      this.adjustmentError = this.formatError(err, 'No se pudo aplicar el ajuste');
    } finally {
      this.loadingAdjustment = false;
    }
  }

  async refreshStockSection(): Promise<void> {
    await this.loadStock();
    await this.loadStockMovements();
    await this.loadTransformationTemplates();
    await this.loadTransformationHistory();
  }

  async loadTransformationTemplates(): Promise<void> {
    this.loadingTransformations = true;
    this.transformationsError = '';
    try {
      const rows: any = await firstValueFrom(this.http.get('/api/v1/stock/transformations/templates'));
      this.transformationTemplates = Array.isArray(rows) ? rows : [];
    } catch (err: any) {
      this.transformationsError = this.formatError(err, 'No se pudieron cargar templates de transformación');
    } finally {
      this.loadingTransformations = false;
    }
  }

  async loadTransformationYieldPolicy(): Promise<void> {
    this.loadingTransformationPolicy = true;
    this.transformationPolicyMessage = '';
    try {
      const row: any = await firstValueFrom(this.http.get('/api/v1/stock/transformations/yield-policy'));
      this.transformationYieldPolicy = {
        autoUpdateEnabled: !!row?.autoUpdateEnabled,
        requireAdminApproval: !!row?.requireAdminApproval,
        minSampleCount: Number(row?.minSampleCount ?? 12),
        maxVolatility: Number(row?.maxVolatility ?? 0.12),
        maxDeviationPct: Number(row?.maxDeviationPct ?? 15),
        minDeviationPct: Number(row?.minDeviationPct ?? 3)
      };
    } catch {
      // noop
    } finally {
      this.loadingTransformationPolicy = false;
    }
  }

  async saveTransformationYieldPolicy(): Promise<void> {
    this.loadingTransformationPolicy = true;
    this.transformationPolicyMessage = '';
    try {
      await firstValueFrom(this.http.put('/api/v1/stock/transformations/yield-policy', this.transformationYieldPolicy));
      this.transformationPolicyMessage = 'Política de rendimiento actualizada.';
      await this.loadTransformationRecalibrations();
    } catch (err: any) {
      this.transformationPolicyMessage = this.formatError(err, 'No se pudo actualizar la política de rendimiento.');
    } finally {
      this.loadingTransformationPolicy = false;
    }
  }

  async loadTransformationRecalibrations(): Promise<void> {
    this.loadingTransformationRecalibrations = true;
    this.transformationRecalibrationsError = '';
    try {
      const rows: any = await firstValueFrom(this.http.get('/api/v1/stock/transformations/yield-recalibrations?status=Pending'));
      this.transformationRecalibrations = Array.isArray(rows) ? rows : [];
    } catch (err: any) {
      this.transformationRecalibrationsError = this.formatError(err, 'No se pudieron cargar recalibraciones pendientes.');
    } finally {
      this.loadingTransformationRecalibrations = false;
    }
  }

  async approveTransformationRecalibration(id: number): Promise<void> {
    try {
      await firstValueFrom(this.http.post(`/api/v1/stock/transformations/yield-recalibrations/${id}/approve`, {}));
      await this.loadTransformationTemplates();
      await this.loadTransformationRecalibrations();
      this.transformationsMessage = 'Recalibración aprobada y plantilla actualizada.';
    } catch (err: any) {
      this.transformationsError = this.formatError(err, 'No se pudo aprobar la recalibración.');
    }
  }

  async rejectTransformationRecalibration(id: number): Promise<void> {
    try {
      await firstValueFrom(this.http.post(`/api/v1/stock/transformations/yield-recalibrations/${id}/reject`, {}));
      await this.loadTransformationRecalibrations();
      this.transformationsMessage = 'Recalibración rechazada.';
    } catch (err: any) {
      this.transformationsError = this.formatError(err, 'No se pudo rechazar la recalibración.');
    }
  }

  async loadTransformationHistory(): Promise<void> {
    this.loadingTransformationHistory = true;
    this.transformationHistoryError = '';
    try {
      const params: string[] = ['movementType=Transformation'];
      if (this.transformationHistoryFrom) params.push(`from=${encodeURIComponent(this.transformationHistoryFrom)}`);
      if (this.transformationHistoryTo) params.push(`to=${encodeURIComponent(this.transformationHistoryTo)}`);
      const query = params.length ? `?${params.join('&')}` : '';
      const rows: any = await firstValueFrom(this.http.get(`/api/v1/stock/movements${query}`));
      this.transformationHistory = Array.isArray(rows) ? rows : [];
      this.transformationHistoryPage = 1;
    } catch (err: any) {
      this.transformationHistoryError = this.formatError(err, 'No se pudo cargar el historial de transformaciones');
    } finally {
      this.loadingTransformationHistory = false;
    }
  }

  pagedTransformationHistory(): any[] {
    return this.paginate(this.transformationHistory, this.transformationHistoryPage);
  }

  async saveTransformationTemplate(): Promise<void> {
    this.transformationsError = '';
    this.transformationsMessage = '';

    if (!this.transformationSourceProductId || !this.transformationTargetProductId) {
      this.transformationsError = 'Selecciona producto origen y destino.';
      return;
    }

    if (this.transformationSourceProductId === this.transformationTargetProductId) {
      this.transformationsError = 'Origen y destino deben ser distintos.';
      return;
    }

    const factor = Number(this.transformationYieldFactor);
    if (!Number.isFinite(factor) || factor <= 0) {
      this.transformationsError = 'Ingresa un factor de rendimiento valido mayor a 0.';
      return;
    }

    this.loadingTransformations = true;
    try {
      await firstValueFrom(this.http.post('/api/v1/stock/transformations/templates', {
        supplierId: this.normalizeOptionalNumber(this.transformationSupplierId),
        sourceProductId: this.transformationSourceProductId,
        targetProductId: this.transformationTargetProductId,
        yieldFactor: factor,
        notes: this.transformationNotes.trim() || null
      }));
      this.transformationsMessage = 'Template guardado correctamente.';
      await this.loadTransformationTemplates();
    } catch (err: any) {
      this.transformationsError = this.formatError(err, 'No se pudo guardar el template');
      this.loadingTransformations = false;
    }
  }

  useTransformationTemplate(template: any): void {
    this.transformationSupplierId = template?.supplierId ?? null;
    this.transformationSourceProductId = template?.sourceProductId ?? null;
    this.transformationTargetProductId = template?.targetProductId ?? null;
    this.transformationYieldFactor = `${template?.yieldFactor ?? ''}`;
    this.transformationNotes = template?.notes ?? '';
    this.transformationYieldFactorEdited = false;
    void this.loadTransformationSuggestion();
    if (!this.transformationTargetQty && this.transformationSourceQty) {
      const source = Number(this.transformationSourceQty);
      const factor = Number(this.transformationYieldFactor);
      if (Number.isFinite(source) && source > 0 && Number.isFinite(factor) && factor > 0) {
        this.transformationTargetQty = `${Math.round(source * factor * 1000) / 1000}`;
      }
    }
  }

  onTransformationContextChange(): void {
    this.transformationYieldFactorEdited = false;
    void this.loadTransformationSuggestion();
  }

  onTransformationSourceQtyChange(): void {
    this.syncObservedYieldFactor();
    void this.loadTransformationSuggestion();
  }

  onTransformationTargetQtyChange(): void {
    this.syncObservedYieldFactor();
  }

  async loadTransformationSuggestion(): Promise<void> {
    if (!this.transformationSourceProductId || !this.transformationTargetProductId || this.transformationSourceProductId === this.transformationTargetProductId) {
      this.transformationSuggestion = null;
      return;
    }

    this.loadingTransformationSuggestion = true;
    try {
      const params: string[] = [
        `sourceProductId=${encodeURIComponent(String(this.transformationSourceProductId))}`,
        `targetProductId=${encodeURIComponent(String(this.transformationTargetProductId))}`
      ];
      if (this.transformationSupplierId != null) {
        params.push(`supplierId=${encodeURIComponent(String(this.transformationSupplierId))}`);
      }
      const sourceQty = Number(this.transformationSourceQty);
      if (Number.isFinite(sourceQty) && sourceQty > 0) {
        params.push(`sourceQty=${encodeURIComponent(String(sourceQty))}`);
      }

      const result: any = await firstValueFrom(this.http.get(`/api/v1/stock/transformations/yield-suggestion?${params.join('&')}`));
      this.transformationSuggestion = {
        suggestedYieldFactor: Number(result?.suggestedYieldFactor ?? 1),
        confidence: `${result?.confidence ?? 'Baja'}`,
        sampleCount: Number(result?.sampleCount ?? 0),
        source: `${result?.source ?? 'Default'}`
      };

      if (!this.transformationYieldFactorEdited || !this.transformationYieldFactor.trim()) {
        this.transformationYieldFactor = `${this.transformationSuggestion.suggestedYieldFactor}`;
      }
    } catch {
      this.transformationSuggestion = null;
    } finally {
      this.loadingTransformationSuggestion = false;
    }
  }

  recalculateTransformationTarget(): void {
    const source = Number(this.transformationSourceQty);
    const factor = Number(this.transformationYieldFactor);
    if (!Number.isFinite(source) || source <= 0 || !Number.isFinite(factor) || factor <= 0) return;
    this.transformationTargetQty = `${Math.round(source * factor * 1000) / 1000}`;
  }

  async applyTransformation(): Promise<void> {
    this.transformationsError = '';
    this.transformationsMessage = '';
    this.transformationSubmitAttempted = true;

    const applyHint = this.transformationApplyHint();
    if (applyHint) {
      this.transformationsError = applyHint;
      return;
    }

    const sourceQty = Number(this.transformationSourceQty);
    const targetQty = Number(this.transformationTargetQty);
    const observedYieldFactor = sourceQty > 0 ? targetQty / sourceQty : NaN;
    const yieldFactor = Number.isFinite(observedYieldFactor) && observedYieldFactor > 0
      ? observedYieldFactor
      : Number(this.transformationYieldFactor);

    if (!Number.isFinite(sourceQty) || sourceQty <= 0) {
      this.transformationsError = 'Ingresa cantidad origen valida mayor a 0.';
      return;
    }

    if (!Number.isFinite(targetQty) || targetQty <= 0) {
      this.transformationsError = 'Ingresa cantidad destino valida mayor a 0.';
      return;
    }

    this.loadingTransformations = true;
    try {
      await firstValueFrom(this.http.post('/api/v1/stock/transformations/apply', {
        supplierId: this.normalizeOptionalNumber(this.transformationSupplierId),
        sourceProductId: this.transformationSourceProductId,
        targetProductId: this.transformationTargetProductId,
        sourceQty,
        targetQty,
        yieldFactor: Number.isFinite(yieldFactor) && yieldFactor > 0 ? yieldFactor : null,
        suggestedYieldFactor: this.transformationSuggestion?.suggestedYieldFactor ?? null,
        usedSuggestedFactor: this.wasSuggestedYieldUsed(yieldFactor),
        suggestionConfidence: this.transformationSuggestion?.confidence ?? null,
        suggestionSampleCount: this.transformationSuggestion?.sampleCount ?? null,
        suggestionSource: this.transformationSuggestion?.source ?? null,
        notes: this.transformationNotes.trim() || null
      }));

      this.transformationsMessage = 'Transformación aplicada correctamente.';
      this.transformationSourceQty = '';
      this.transformationTargetQty = '';
      this.transformationYieldFactorEdited = false;
      this.transformationSubmitAttempted = false;
      await this.refreshStockSection();
      await this.loadTransformationRecalibrations();
    } catch (err: any) {
      this.transformationsError = this.formatError(err, 'No se pudo aplicar la transformación');
      this.loadingTransformations = false;
    }
  }

  private wasSuggestedYieldUsed(currentYieldFactor: number): boolean {
    if (!this.transformationSuggestion) return false;
    if (!Number.isFinite(currentYieldFactor) || currentYieldFactor <= 0) return false;
    return Math.abs(currentYieldFactor - this.transformationSuggestion.suggestedYieldFactor) <= 0.0005;
  }

  transformationObservedFactor(): number {
    const source = Number(this.transformationSourceQty);
    const target = Number(this.transformationTargetQty);
    if (!Number.isFinite(source) || source <= 0 || !Number.isFinite(target) || target <= 0) return 0;
    return Math.round((target / source) * 1000000) / 1000000;
  }

  transformationDeviationPct(): number | null {
    const observed = this.transformationObservedFactor();
    const suggested = this.transformationSuggestion?.suggestedYieldFactor;
    if (!suggested || suggested <= 0 || observed <= 0) return null;
    const value = Math.abs((observed - suggested) / suggested) * 100;
    return Math.round(value * 1000) / 1000;
  }

  private syncObservedYieldFactor(): void {
    const observed = this.transformationObservedFactor();
    if (observed > 0) {
      this.transformationYieldFactor = `${observed}`;
      this.transformationYieldFactorEdited = true;
    }
  }

  formatTransformationHistoryDetail(notes: string | null | undefined): string {
    const text = `${notes ?? ''}`.trim();
    if (!text) return '-';

    const parsed = text.match(/transformacion stock(?:\s+proveedor:(\d+))?\s+origen:([\d.,]+)\s+destino:([\d.,]+)\s+factor:([\d.,]+)(?:\.\s*(.*))?$/i);
    if (parsed) {
      const [, supplierIdRaw, sourceQtyRaw, targetQtyRaw, factorRaw, extraNotesRaw] = parsed;
      const detailParts: string[] = ['Transformación de stock'];

      if (supplierIdRaw) detailParts.push(`Proveedor: ${this.transformationSupplierLabelById(Number(supplierIdRaw))}`);
      detailParts.push(`Origen: ${sourceQtyRaw}`);
      detailParts.push(`Destino: ${targetQtyRaw}`);
      detailParts.push(`Factor: ${factorRaw}`);

      const extraNotes = `${extraNotesRaw ?? ''}`.trim();
      if (extraNotes) detailParts.push(`Nota: ${extraNotes}`);

      return detailParts.join(' · ');
    }

    return text.replace(/proveedor:(\d+)/gi, (_match, supplierIdRaw: string) => {
      return `proveedor: ${this.transformationSupplierLabelById(Number(supplierIdRaw))}`;
    });
  }

  private transformationSupplierLabelById(supplierId: number): string {
    const supplier = this.supplierOptions.find(s => Number(s?.id) === supplierId);
    if (supplier?.name) return `${supplier.name} (#${supplierId})`;
    return `Proveedor #${supplierId}`;
  }

  transformationApplyHint(): string {
    if (!this.transformationSourceProductId) return 'Seleccioná producto origen.';
    if (!this.transformationTargetProductId) return 'Seleccioná producto a recibir.';
    if (this.transformationSourceProductId === this.transformationTargetProductId) return 'Origen y destino deben ser productos distintos.';

    const sourceQty = Number(this.transformationSourceQty);
    if (!Number.isFinite(sourceQty) || sourceQty <= 0) return 'Ingresá cantidad origen válida mayor a 0.';

    const targetQty = Number(this.transformationTargetQty);
    if (!Number.isFinite(targetQty) || targetQty <= 0) return 'Ingresá cantidad a recibir válida mayor a 0.';

    return '';
  }

  shouldShowTransformationApplyHint(): boolean {
    if (this.transformationSubmitAttempted) return true;

    return this.transformationSourceProductId != null
      || this.transformationTargetProductId != null
      || `${this.transformationSourceQty}`.trim().length > 0
      || `${this.transformationTargetQty}`.trim().length > 0;
  }

  productNameById(productId: number): string {
    const found = this.stockRows.find(p => p.productId === productId);
    return found?.productName || `Producto #${productId}`;
  }

  async loadClaims(): Promise<void> {
    this.loadingClaims = true;
    this.claimsError = '';
    try {
      const query = this.claimsStatus ? `?status=${encodeURIComponent(this.claimsStatus)}` : '';
      const rows: any = await firstValueFrom(this.http.get(`/api/v1/stock/claims${query}`));
      this.claims = Array.isArray(rows) ? rows : [];
      this.claimsPage = 1;
    } catch (err: any) {
      this.claimsError = this.formatError(err, 'No se pudo cargar reclamos');
    } finally {
      this.loadingClaims = false;
    }
  }

  async loadSuppliers(): Promise<void> {
    try {
      const rows: any = await firstValueFrom(this.http.get('/api/v1/suppliers'));
      this.supplierOptions = Array.isArray(rows) ? rows : [];
      this.onClaimSupplierChange();
      this.onPolicySupplierChange();
    } catch {
      this.supplierOptions = [];
    }
  }

  onPolicySupplierChange(): void {
    this.policyMessage = '';
    const supplier = this.supplierOptions.find(s => Number(s.id) === Number(this.policySupplierId));
    if (!supplier) {
      this.policySettlementMode = 'credit';
      this.policyAllowOverride = false;
      return;
    }

    this.policySettlementMode = this.policyModeFromSupplier(supplier.claimSettlementModeDefault);
    this.policyAllowOverride = !!supplier.allowClaimSettlementOverride;
  }

  async saveSupplierPolicy(): Promise<void> {
    if (this.policySupplierId == null) return;
    this.savingPolicy = true;
    this.policyMessage = '';
    try {
      await firstValueFrom(this.http.put(`/api/v1/suppliers/${this.policySupplierId}`, {
        claimSettlementModeDefault: this.apiSettlementMode(this.policySettlementMode),
        allowClaimSettlementOverride: this.policyAllowOverride
      }));
      await this.loadSuppliers();
      this.policyMessage = 'Condicion del proveedor actualizada correctamente.';
    } catch (err: any) {
      this.policyMessage = this.formatError(err, 'No se pudo actualizar la condición del proveedor.');
    } finally {
      this.savingPolicy = false;
    }
  }

  filteredClaimsRows(): any[] {
    let rows = [...this.claims];
    if (this.claimsSettlementFilter) {
      rows = rows.filter(c => this.claimSettlementMode(c) === this.claimsSettlementFilter);
    }

    const q = this.claimsFilter.trim().toLowerCase();
    if (!q) return rows;
    return rows.filter(c => `${c.supplierName ?? ''} ${c.status ?? ''} ${this.claimRequestedModeLabel(c)}`.toLowerCase().includes(q));
  }

  pagedClaims(): any[] {
    return this.paginate(this.filteredClaimsRows(), this.claimsPage);
  }

  claimsOpenCount(): number {
    return this.claims.filter(c => this.claimStatusClass(c?.status) === 'claim-status-open').length;
  }

  claimsPickupCount(): number {
    return this.claims.filter(c => this.claimStatusClass(c?.status) === 'claim-status-pickup').length;
  }

  claimsSolvedCount(): number {
    return this.claims.filter(c => this.claimStatusClass(c?.status) === 'claim-status-solved').length;
  }

  claimsPendingCount(): number {
    return this.claims.filter(c => this.isClaimPendingResolution(c)).length;
  }

  claimsPendingByModeCount(mode: 'credit' | 'refund' | 'exchange'): number {
    return this.claims.filter(c => this.isClaimPendingResolution(c) && this.claimSettlementMode(c) === mode).length;
  }

  setClaimsSettlementFilter(mode: '' | 'credit' | 'refund' | 'exchange'): void {
    this.claimsSettlementFilter = mode;
    this.resetPage('claims');
  }

  settlementKpiClass(mode: '' | 'credit' | 'refund' | 'exchange'): string {
    const classes: string[] = [];
    if (this.claimsSettlementFilter === mode) classes.push('active');

    if (mode === 'credit' || mode === 'refund' || mode === 'exchange') {
      classes.push(mode);
      if (this.claimsPendingByModeCount(mode) > 0) classes.push('has-pending');
    }

    return classes.join(' ');
  }

  claimStatusClass(status: string): string {
    const s = `${status ?? ''}`.toLowerCase();
    if (s === 'open' || s === 'created' || s === 'pending') return 'claim-status-open';
    if (s === 'pickedup' || s === 'picked_up' || s === 'retirado') return 'claim-status-pickup';
    if (s === 'credited' || s === 'replaced' || s === 'refunded') return 'claim-status-solved';
    return 'claim-status-default';
  }

  canSubmitClaimForm(): boolean {
    return this.validClaimItems().length > 0 && this.claimHintText() === '';
  }

  claimCanPreview(): boolean {
    return this.validClaimItems().length > 0;
  }

  claimPreviewTitle(): string {
    const count = this.validClaimItems().length;
    return `${count} producto${count === 1 ? '' : 's'} en reclamo`;
  }

  claimPreviewTotal(): string {
    const total = this.validClaimItems().reduce((acc, item) => acc + (item.quantity * item.unitCostSnapshot), 0);
    return this.formatCurrency(total);
  }

  claimFormHint(): string {
    return this.claimHintText();
  }

  openClaimModal(): void {
    this.claimsError = '';
    this.isClaimModalOpen = true;
    this.updateBodyScrollLock();
  }

  closeClaimModal(): void {
    this.claimsError = '';
    this.isClaimModalOpen = false;
    this.resetClaimModalForm();
    this.updateBodyScrollLock();
  }

  addClaimItem(): void {
    this.claimItems.push({ productId: null, quantity: '', unitCost: '', notes: '' });
  }

  removeClaimItem(index: number): void {
    if (this.claimItems.length <= 1) return;
    this.claimItems.splice(index, 1);
  }

  onClaimEvidenceSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    if (!files.length) return;

    const allowed = ['image/jpeg', 'image/png', 'image/webp'];
    const maxSize = 6 * 1024 * 1024;

    for (const file of files) {
      if (this.claimEvidenceFiles.length >= 5) break;
      if (!allowed.includes(file.type)) continue;
      if (file.size > maxSize) continue;
      this.claimEvidenceFiles.push(file);
    }

    input.value = '';
  }

  removeClaimEvidence(index: number): void {
    this.claimEvidenceFiles.splice(index, 1);
  }

  claimEvidenceCount(claim: any): number {
    const list = Array.isArray(claim?.evidences) ? claim.evidences : [];
    return list.length;
  }

  claimFirstEvidenceUrl(claim: any): string | null {
    const list = Array.isArray(claim?.evidences) ? claim.evidences : [];
    if (!list.length) return null;
    return list[0]?.previewUrl || list[0]?.downloadUrl || null;
  }

  get selectedEvidence(): any | null {
    if (!this.evidenceViewerItems.length) return null;
    if (this.evidenceViewerIndex < 0 || this.evidenceViewerIndex >= this.evidenceViewerItems.length) return null;
    return this.evidenceViewerItems[this.evidenceViewerIndex];
  }

  async openClaimDetail(id: number): Promise<void> {
    this.isClaimDetailOpen = true;
    this.updateBodyScrollLock();
    this.loadingClaimDetail = true;
    this.claimDetailError = '';
    this.selectedClaimDetail = null;
    this.closeEvidenceViewer();
    try {
      const detail: any = await firstValueFrom(this.http.get(`/api/v1/stock/claims/${id}`));
      this.selectedClaimDetail = detail;
    } catch (err: any) {
      this.claimDetailError = this.formatError(err, `No se pudo cargar el detalle del reclamo #${id}`);
    } finally {
      this.loadingClaimDetail = false;
    }
  }

  closeClaimDetail(): void {
    this.isClaimDetailOpen = false;
    this.loadingClaimDetail = false;
    this.claimDetailError = '';
    this.selectedClaimDetail = null;
    this.closeEvidenceViewer();
    this.updateBodyScrollLock();
  }

  openEvidenceViewer(evidences: any[], index: number): void {
    this.evidenceViewerItems = Array.isArray(evidences) ? evidences : [];
    this.evidenceViewerIndex = Math.max(0, Math.min(index, this.evidenceViewerItems.length - 1));
    this.isEvidenceViewerOpen = this.evidenceViewerItems.length > 0;
    this.updateBodyScrollLock();
  }

  closeEvidenceViewer(): void {
    this.isEvidenceViewerOpen = false;
    this.evidenceViewerItems = [];
    this.evidenceViewerIndex = 0;
    this.updateBodyScrollLock();
  }

  canGoPreviousEvidence(): boolean {
    return this.evidenceViewerIndex > 0;
  }

  canGoNextEvidence(): boolean {
    return this.evidenceViewerIndex < this.evidenceViewerItems.length - 1;
  }

  previousEvidence(): void {
    if (this.canGoPreviousEvidence()) this.evidenceViewerIndex -= 1;
  }

  nextEvidence(): void {
    if (this.canGoNextEvidence()) this.evidenceViewerIndex += 1;
  }

  formatFileSize(bytes: number): string {
    const size = Number(bytes);
    if (!Number.isFinite(size) || size <= 0) return '-';
    if (size < 1024) return `${Math.round(size)} B`;
    if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`;
    return `${(size / (1024 * 1024)).toFixed(1)} MB`;
  }

  canPickup(status: string): boolean {
    const s = `${status ?? ''}`.toLowerCase();
    return s === 'open' || s === 'created' || s === 'pending';
  }

  claimSettlementMode(claim: any): 'credit' | 'refund' | 'exchange' {
    const mode = `${claim?.requestedSettlementMode ?? claim?.resolvedSettlementMode ?? ''}`.toLowerCase();
    if (mode === 'refund') return 'refund';
    if (mode === 'exchangegoods' || mode === 'replace' || mode === 'replaced') return 'exchange';
    return 'credit';
  }

  claimRequestedModeLabel(claim: any): string {
    const mode = this.claimSettlementMode(claim);
    if (mode === 'refund') return 'Reembolso';
    if (mode === 'exchange') return 'Reposición';
    return 'Crédito';
  }

  claimSettlementBadgeClass(claim: any): string {
    const mode = this.claimSettlementMode(claim);
    if (mode === 'refund') return 'claim-settlement-refund';
    if (mode === 'exchange') return 'claim-settlement-exchange';
    return 'claim-settlement-credit';
  }

  private isClaimPendingResolution(claim: any): boolean {
    const s = `${claim?.status ?? ''}`.toLowerCase();
    return s === 'open' || s === 'created' || s === 'pending' || s === 'pickedup' || s === 'picked_up' || s === 'retirado';
  }

  canResolveCredit(claim: any): boolean {
    return this.canCredit(claim?.status) && this.claimSettlementMode(claim) === 'credit';
  }

  canResolveRefund(claim: any): boolean {
    return this.canCredit(claim?.status) && this.claimSettlementMode(claim) === 'refund';
  }

  canResolveExchange(claim: any): boolean {
    return this.canCredit(claim?.status) && this.claimSettlementMode(claim) === 'exchange';
  }

  canCredit(status: string): boolean {
    const s = `${status ?? ''}`.toLowerCase();
    return s === 'pickedup' || s === 'picked_up' || s === 'retirado';
  }

  canReplace(status: string): boolean {
    const s = `${status ?? ''}`.toLowerCase();
    return s === 'pickedup' || s === 'picked_up' || s === 'retirado';
  }

  isClaimSettlementLocked(): boolean {
    const selected = this.selectedSupplier();
    if (!selected) return false;
    return !selected.allowClaimSettlementOverride;
  }

  selectedSupplierPolicyHint(): string {
    const selected = this.selectedSupplier();
    if (!selected) return 'Sin proveedor seleccionado: podés definir la resolución del reclamo.';
    const modeLabel = this.uiSettlementLabel(this.policyModeFromSupplier(selected.claimSettlementModeDefault));
    if (selected.allowClaimSettlementOverride) return `Política del proveedor: ${modeLabel}. Podés cambiarla para este reclamo.`;
    return `Política del proveedor: ${modeLabel}. Solo se puede resolver con esta condición.`;
  }

  onClaimSupplierChange(): void {
    const selected = this.selectedSupplier();
    if (!selected) return;
    const policyMode = this.policyModeFromSupplier(selected.claimSettlementModeDefault);
    if (!selected.allowClaimSettlementOverride || this.claimResolutionType !== policyMode) {
      this.claimResolutionType = policyMode;
    }
  }

  claimStatusLabel(status: string): string {
    const s = `${status ?? ''}`.toLowerCase();
    if (s === 'open' || s === 'created' || s === 'pending') return 'Abierto';
    if (s === 'pickedup' || s === 'picked_up') return 'Retirado';
    if (s === 'credited') return 'Acreditado';
    if (s === 'replaced') return 'Repuesto';
    if (s === 'refunded') return 'Reembolsado';
    return status || '-';
  }

  claimIsPicked(claim: any): boolean {
    const status = `${claim?.status ?? ''}`.toLowerCase();
    return !!claim?.pickedUpAt || status === 'pickedup' || status === 'picked_up' || status === 'credited' || status === 'replaced';
  }

  claimIsResolved(claim: any): boolean {
    const status = `${claim?.status ?? ''}`.toLowerCase();
    return !!claim?.creditedAt || status === 'credited' || status === 'replaced' || status === 'refunded';
  }

  claimResolutionLabel(claim: any): string {
    const mode = this.claimSettlementMode(claim);
    if (mode === 'exchange') return 'Reposición mercadería';
    if (mode === 'refund') return 'Reembolso';
    return 'Crédito aplicado';
  }

  async createClaimFromStock(): Promise<void> {
    this.claimsError = '';
    const hint = this.claimHintText();
    if (hint) {
      this.claimsError = hint;
      return;
    }

    const items = this.validClaimItems();
    if (items.length === 0) {
      this.claimsError = 'Agrega al menos un producto valido para crear el reclamo.';
      return;
    }

    const supplierId = this.claimSupplierId != null ? Number(this.claimSupplierId) : null;
    const resolutionText = this.claimResolutionType === 'credit'
      ? 'Resolución prevista: crédito en próxima compra.'
      : this.claimResolutionType === 'refund'
        ? 'Resolución prevista: reembolso.'
        : 'Resolución prevista: reposición con otra mercadería.';
    const notes = [this.claimNotes.trim(), resolutionText].filter(x => !!x).join(' ');

    const form = new FormData();
    if (Number.isFinite(supplierId as number)) {
      form.append('supplierId', String(supplierId));
    }
    form.append('hasReceipt', 'false');
    form.append('notes', notes);
    form.append('settlementMode', this.apiSettlementMode(this.claimResolutionType));
    form.append('itemsJson', JSON.stringify(items));
    for (const file of this.claimEvidenceFiles) {
      form.append('evidenceFiles', file, file.name);
    }

    this.loadingClaims = true;
    try {
      await firstValueFrom(this.http.post('/api/v1/stock/claims/with-evidence', form));

      this.closeClaimModal();
      await this.loadClaims();
    } catch (err: any) {
      this.claimsError = this.formatError(err, 'No se pudo crear el reclamo');
      this.loadingClaims = false;
    }
  }

  async pickupClaim(id: number): Promise<void> {
    this.loadingClaims = true;
    this.claimsError = '';
    try {
      await firstValueFrom(this.http.post(`/api/v1/stock/claims/${id}/pickup`, {}));
      await this.loadClaims();
      if (this.selectedClaimDetail?.id === id) await this.openClaimDetail(id);
    } catch (err: any) {
      this.claimsError = this.formatError(err, `No se pudo retirar reclamo #${id}`);
      this.loadingClaims = false;
    }
  }

  async creditClaim(id: number): Promise<void> {
    this.loadingClaims = true;
    this.claimsError = '';
    try {
      await firstValueFrom(this.http.post(`/api/v1/stock/claims/${id}/credit`, {}));
      await this.loadClaims();
      if (this.selectedClaimDetail?.id === id) await this.openClaimDetail(id);
      await this.refreshStockSection();
    } catch (err: any) {
      this.claimsError = this.formatError(err, `No se pudo acreditar reclamo #${id}`);
      this.loadingClaims = false;
    }
  }

  replaceClaim(id: number): void {
    this.exchangeResolveError = '';
    this.exchangeResolveClaimId = id;
    this.exchangeResolveLines = [{ productId: null, quantity: '', unitCost: '', notes: '' }];
    this.isExchangeResolveModalOpen = true;
    this.updateBodyScrollLock();
  }

  addExchangeResolveLine(): void {
    this.exchangeResolveLines.push({ productId: null, quantity: '', unitCost: '', notes: '' });
  }

  removeExchangeResolveLine(index: number): void {
    if (this.exchangeResolveLines.length <= 1) return;
    this.exchangeResolveLines.splice(index, 1);
  }

  closeExchangeResolveModal(): void {
    this.isExchangeResolveModalOpen = false;
    this.exchangeResolveClaimId = null;
    this.exchangeResolveError = '';
    this.exchangeResolveLines = [{ productId: null, quantity: '', unitCost: '', notes: '' }];
    this.updateBodyScrollLock();
  }

  async confirmExchangeResolve(): Promise<void> {
    if (!this.exchangeResolveClaimId) {
      this.exchangeResolveError = 'No se encontro el reclamo a resolver.';
      return;
    }

    const lines = this.exchangeResolveLines
      .map(l => ({
        productId: l.productId,
        quantity: Number(l.quantity),
        unitCostSnapshot: Number(l.unitCost),
        notes: l.notes.trim() || null
      }))
      .filter(l => l.productId != null && Number.isFinite(l.quantity) && l.quantity > 0)
      .map(l => ({
        productId: l.productId,
        quantity: l.quantity,
        unitCostSnapshot: Number.isFinite(l.unitCostSnapshot) && l.unitCostSnapshot >= 0 ? l.unitCostSnapshot : 0,
        notes: l.notes
      }));

    if (lines.length === 0) {
      this.exchangeResolveError = 'Agrega al menos una línea válida para la reposición.';
      return;
    }

    this.loadingClaims = true;
    this.claimsError = '';
    try {
      await firstValueFrom(this.http.post(`/api/v1/stock/claims/${this.exchangeResolveClaimId}/resolve/exchange`, { lines }));
      await this.loadClaims();
      if (this.selectedClaimDetail?.id === this.exchangeResolveClaimId) await this.openClaimDetail(this.exchangeResolveClaimId);
      await this.refreshStockSection();
      this.closeExchangeResolveModal();
    } catch (err: any) {
      this.exchangeResolveError = this.formatError(err, `No se pudo registrar reposición para reclamo #${this.exchangeResolveClaimId}`);
      this.loadingClaims = false;
    }
  }

  async refundClaim(id: number): Promise<void> {
    this.loadingClaims = true;
    this.claimsError = '';
    try {
      await firstValueFrom(this.http.post(`/api/v1/stock/claims/${id}/resolve/refund`, { amount: 0 }));
      await this.loadClaims();
      if (this.selectedClaimDetail?.id === id) await this.openClaimDetail(id);
      await this.refreshStockSection();
    } catch (err: any) {
      this.claimsError = this.formatError(err, `No se pudo registrar reembolso para reclamo #${id}`);
      this.loadingClaims = false;
    }
  }

  async loadRrhh(): Promise<void> {
    this.loadingRrhh = true;
    this.rrhhError = '';
    try {
      const params: string[] = [];
      if (this.rrhhFrom) params.push(`from=${encodeURIComponent(this.rrhhFrom)}`);
      if (this.rrhhTo) params.push(`to=${encodeURIComponent(this.rrhhTo)}`);
      const query = params.length ? `?${params.join('&')}` : '';
      const rows: any = await firstValueFrom(this.http.get(`/api/v1/rrhh/inconsistencies${query}`));
      this.rrhhInconsistencies = Array.isArray(rows) ? rows : [];
      this.rrhhPage = 1;
    } catch (err: any) {
      this.rrhhError = (err?.status === 403)
        ? 'Sin permisos para ver RRHH. Ingresar con rol Supervisor o Administrador.'
        : this.formatError(err, 'No se pudo cargar RRHH');
    } finally {
      this.loadingRrhh = false;
    }
  }

  rrhhTypeLabel(kind: string): string {
    const k = `${kind ?? ''}`.toLowerCase();
    if (k.includes('missingentry') || k.includes('sinentrada')) return 'Sin entrada';
    if (k.includes('missingexit') || k.includes('sinsalida')) return 'Sin salida';
    if (k.includes('odd')) return 'Cantidad impar de fichadas';
    return kind || 'Inconsistencia';
  }

  async loadKanban(): Promise<void> {
    this.loadingKanban = true;
    this.kanbanError = '';
    try {
      const query = this.kanbanRequiredOnly ? '?requiredOnly=true' : '';
      const rows: any = await firstValueFrom(this.http.get(`/api/v1/kanban/tasks${query}`));
      this.kanbanTasks = Array.isArray(rows) ? rows : [];
      this.kanbanPage = 1;
    } catch (err: any) {
      this.kanbanError = this.formatError(err, 'No se pudo cargar tareas Kanban');
    } finally {
      this.loadingKanban = false;
    }
  }

  kanbanStatusLabel(status: string): string {
    const s = `${status ?? ''}`.toLowerCase();
    if (s === 'todo') return 'Pendiente';
    if (s === 'inprogress') return 'En curso';
    if (s === 'done') return 'Completada';
    return status || '-';
  }

  canMarkTaskDone(status: string): boolean {
    return `${status ?? ''}`.toLowerCase() !== 'done';
  }

  doneChecklistCount(task: any): number {
    const list = Array.isArray(task?.checklistItems) ? task.checklistItems : [];
    return list.filter((x: any) => !!x?.isDone).length;
  }

  pagedKanbanTasks(): any[] {
    return this.paginate(this.kanbanTasks, this.kanbanPage);
  }

  pagedRrhhInconsistencies(): any[] {
    return this.paginate(this.rrhhInconsistencies, this.rrhhPage);
  }

  async markKanbanDone(taskId: number): Promise<void> {
    this.loadingKanban = true;
    this.kanbanError = '';
    try {
      await firstValueFrom(this.http.patch(`/api/v1/kanban/tasks/${taskId}/status`, { status: 'Done' }));
      await this.loadKanban();
    } catch (err: any) {
      this.kanbanError = this.formatError(err, `No se pudo actualizar tarea #${taskId}`);
      this.loadingKanban = false;
    }
  }

  totalPages(totalRows: number): number {
    return Math.max(1, Math.ceil(totalRows / this.pageSize));
  }

  pageRangeLabel(totalRows: number, page: number): string {
    if (totalRows <= 0) return '0 resultados';
    const start = (page - 1) * this.pageSize + 1;
    const end = Math.min(totalRows, start + this.pageSize - 1);
    return `${start}-${end} de ${totalRows}`;
  }

  prevPage(section: 'stock' | 'stockMovements' | 'transformations' | 'claims' | 'rrhh' | 'kanban'): void {
    if (section === 'stock') this.stockPage = Math.max(1, this.stockPage - 1);
    if (section === 'stockMovements') this.stockMovementsPage = Math.max(1, this.stockMovementsPage - 1);
    if (section === 'transformations') this.transformationHistoryPage = Math.max(1, this.transformationHistoryPage - 1);
    if (section === 'claims') this.claimsPage = Math.max(1, this.claimsPage - 1);
    if (section === 'rrhh') this.rrhhPage = Math.max(1, this.rrhhPage - 1);
    if (section === 'kanban') this.kanbanPage = Math.max(1, this.kanbanPage - 1);
  }

  nextPage(section: 'stock' | 'stockMovements' | 'transformations' | 'claims' | 'rrhh' | 'kanban', totalRows: number): void {
    const max = this.totalPages(totalRows);
    if (section === 'stock') this.stockPage = Math.min(max, this.stockPage + 1);
    if (section === 'stockMovements') this.stockMovementsPage = Math.min(max, this.stockMovementsPage + 1);
    if (section === 'transformations') this.transformationHistoryPage = Math.min(max, this.transformationHistoryPage + 1);
    if (section === 'claims') this.claimsPage = Math.min(max, this.claimsPage + 1);
    if (section === 'rrhh') this.rrhhPage = Math.min(max, this.rrhhPage + 1);
    if (section === 'kanban') this.kanbanPage = Math.min(max, this.kanbanPage + 1);
  }

  resetPage(section: 'stock' | 'stockMovements' | 'claims'): void {
    if (section === 'stock') this.stockPage = 1;
    if (section === 'stockMovements') this.stockMovementsPage = 1;
    if (section === 'claims') this.claimsPage = 1;
  }

  private paginate<T>(rows: T[], page: number): T[] {
    const safeRows = Array.isArray(rows) ? rows : [];
    const start = (Math.max(1, page) - 1) * this.pageSize;
    return safeRows.slice(start, start + this.pageSize);
  }

  private formatError(err: any, fallback: string): string {
    if (err?.status === 0) return 'No hay conexión con el servidor. Verificá red local y API.';
    if (err?.status === 401) return 'Sesión vencida o no autorizada. Volvé a iniciar sesión.';
    if (err?.status === 403) return 'No tenés permisos para esta acción.';

    const message = typeof err?.error?.message === 'string' ? err.error.message.trim() : '';
    const details = typeof err?.error?.details === 'string' ? err.error.details.trim() : '';

    if (message && message !== 'Database update failed') return message;
    if (details) return this.formatDbDetails(details);
    if (message) return message;
    return fallback;
  }

  private formatDbDetails(details: string): string {
    const lower = details.toLowerCase();

    if (lower.includes('ix_productstocks_productid_bucket') && lower.includes('duplicate key')) {
      return 'No se pudo aplicar porque ya existe un registro de stock global para ese producto. Reintentá luego de recargar; si persiste, hay que ajustar el índice de stock por sucursal.';
    }

    if (lower.includes('duplicate key value violates unique constraint')) {
      return 'No se pudo guardar porque ya existe un registro con esos datos.';
    }

    if (lower.includes('violates foreign key constraint')) {
      return 'No se pudo guardar porque uno de los datos relacionados ya no existe o no está disponible.';
    }

    if (lower.includes('value too long for type character varying')) {
      return 'No se pudo guardar porque uno de los textos supera el largo permitido.';
    }

    return `No se pudo guardar por una validación de base de datos. ${details}`;
  }

  private validClaimItems(): Array<{ productId: number; quantity: number; unitCostSnapshot: number; notes: string | null }> {
    return this.claimItems
      .map(item => ({
        productId: item.productId,
        quantity: Number(item.quantity),
        unitCost: Number(item.unitCost),
        notes: item.notes.trim()
      }))
      .filter(item => item.productId != null && Number.isFinite(item.quantity) && item.quantity > 0 && Number.isFinite(item.unitCost) && item.unitCost >= 0)
      .map(item => ({
        productId: item.productId as number,
        quantity: item.quantity,
        unitCostSnapshot: item.unitCost,
        notes: item.notes || null
      }));
  }

  private claimHintText(): string {
    if (!this.claimItems.length) return 'Agrega al menos un producto al reclamo.';

    for (const item of this.claimItems) {
      if (item.productId == null && (item.quantity.trim() || item.unitCost.trim())) return 'Selecciona producto en cada linea cargada.';
      if (item.productId != null && !item.quantity.trim()) return 'Ingresa la cantidad del producto seleccionado.';
      if (item.productId != null && Number(item.quantity) <= 0) return 'La cantidad debe ser mayor a 0 en todos los productos.';
      if (item.productId != null && !item.unitCost.trim()) return 'Ingresa el costo unitario de cada producto.';
      if (item.productId != null && Number(item.unitCost) < 0) return 'El costo unitario no puede ser negativo.';
    }

    if (this.validClaimItems().length === 0) return 'Agrega al menos un producto valido para crear el reclamo.';
    return '';
  }

  private selectedSupplier(): any | null {
    if (this.claimSupplierId == null) return null;
    return this.supplierOptions.find(s => Number(s?.id) === Number(this.claimSupplierId)) ?? null;
  }

  private policyModeFromSupplier(value: string | undefined): 'credit' | 'refund' | 'exchange' {
    const mode = `${value ?? ''}`.toLowerCase();
    if (mode === 'refund') return 'refund';
    if (mode === 'exchangegoods' || mode === 'replace') return 'exchange';
    return 'credit';
  }

  private uiSettlementLabel(mode: 'credit' | 'refund' | 'exchange'): string {
    if (mode === 'refund') return 'Reembolso';
    if (mode === 'exchange') return 'Reposición con mercadería';
    return 'Crédito';
  }

  private apiSettlementMode(mode: 'credit' | 'refund' | 'exchange'): string {
    if (mode === 'refund') return 'Refund';
    if (mode === 'exchange') return 'ExchangeGoods';
    return 'Credit';
  }

  private resetClaimModalForm(): void {
    this.claimSupplierId = null;
    this.claimNotes = '';
    this.claimResolutionType = 'credit';
    this.claimItems = [{ productId: null, quantity: '', unitCost: '', notes: '' }];
    this.claimEvidenceFiles = [];
  }

  private updateBodyScrollLock(): void {
    if (typeof document === 'undefined') return;
    const shouldLock = this.isClaimModalOpen || this.isClaimDetailOpen || this.isEvidenceViewerOpen || this.isExchangeResolveModalOpen;
    document.body.style.overflow = shouldLock ? 'hidden' : '';
  }

  ngOnDestroy(): void {
    if (typeof document !== 'undefined') {
      document.body.style.overflow = '';
    }
  }

  formatCurrency(value: number): string {
    if (!Number.isFinite(value) || value < 0) return '-';
    return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 2 }).format(value);
  }

  private normalizeOptionalNumber(value: number | null): number | null {
    if (value == null) return null;
    const parsed = Number(value);
    if (!Number.isFinite(parsed) || parsed <= 0) return null;
    return parsed;
  }
}
