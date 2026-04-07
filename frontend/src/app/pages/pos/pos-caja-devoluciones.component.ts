import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HealthService } from '../../core/services/health.service';
import { OperatorSessionService } from '../../core/services/operator-session.service';
import { PosCajaService, ReturnEligibleSale, SaleResponse } from '../../core/services/pos-caja.service';
import { PosModuleNavComponent } from '../../shared/components/pos-module-nav.component';

interface ReturnLineDraft {
  originalSaleItemId: number;
  productName: string;
  maxQty: number;
  qtyReturned: number;
  condition: 'Resellable' | 'Waste';
}

@Component({
  standalone: true,
  selector: 'app-pos-caja-devoluciones',
  imports: [CommonModule, FormsModule, PosModuleNavComponent],
  template: `
    <main class="page">
      <div class="hero-bg"></div>
      <app-pos-module-nav />
      <header class="hero">
        <h1>Devoluciones</h1>
        <p>Busca la venta, selecciona items y confirma la devolucion.</p>
      </header>

      <section class="card">
        <div class="search-row">
          <input type="number" placeholder="ID de venta" [(ngModel)]="saleId" />
          <input type="text" placeholder="Buscar en ultimas 48h (ID o cliente)" [(ngModel)]="recentSalesQuery" (input)="filterRecentSales()" />
          <button class="btn-primary" [disabled]="isBusy || !health.isOnline" (click)="loadSale()">Buscar venta</button>
          <button class="btn-secondary" [disabled]="isBusy || !health.isOnline" (click)="loadRecentSales()">Recargar ultimas 48h</button>
        </div>
        <label class="filter-check">
          <input type="checkbox" [(ngModel)]="onlyWithCustomerName" (ngModelChange)="filterRecentSales()" />
          Solo ventas con cliente identificado
        </label>
        <p class="hint">Se listan ventas de las ultimas 48 horas. Fuera de esa ventana no se aceptan devoluciones.</p>

        <div class="recent-sales" *ngIf="filteredRecentSales.length > 0">
          <button class="recent-sale" *ngFor="let s of filteredRecentSales" [disabled]="isBusy" (click)="pickRecentSale(s)">
            <strong>#{{ s.id }}</strong>
            <span>{{ s.createdAt | date:'dd/MM HH:mm' }} · {{ s.customerName || 'Consumidor final' }}</span>
            <span>Total {{ s.total | number:'1.2-2' }}</span>
            <div class="chip-row">
              <span class="status-chip" [class.paid]="s.status === 'Paid'" [class.completed]="s.status === 'Completed'">{{ saleStatusLabel(s.status) }}</span>
              <span class="age-chip" [class.age-mid]="saleAgeBucket(s.createdAt) === '24-48h'">{{ saleAgeLabel(s.createdAt) }}</span>
            </div>
          </button>
        </div>
        <p class="empty" *ngIf="!isBusy && filteredRecentSales.length === 0">No hay ventas elegibles para devolución en las ultimas 48 horas.</p>
      </section>

      <p *ngIf="!health.isOnline" class="alert error">Sin conexion: devoluciones deshabilitadas.</p>
      <p *ngIf="message" class="alert ok">{{ message }}</p>
      <p *ngIf="error" class="alert error">{{ error }}</p>

      <section *ngIf="sale" class="card">
        <h2>Venta #{{ sale.id }} · Total {{ sale.total | number:'1.2-2' }}</h2>

        <div class="line" *ngFor="let l of lines">
          <div class="line-name">{{ l.productName }} <small>max {{ l.maxQty }}</small></div>
          <input type="number" [(ngModel)]="l.qtyReturned" min="0" [max]="l.maxQty" />
          <select [(ngModel)]="l.condition">
            <option value="Resellable">Revendible</option>
            <option value="Waste">Merma</option>
          </select>
        </div>

        <button class="btn-primary" [disabled]="isBusy || !health.isOnline" (click)="submitReturn()">Confirmar devolucion</button>
        <div class="print-links" *ngIf="lastReturnId">
          <a [href]="'/print/return/' + lastReturnId" target="_blank">Ver comprobante</a>
          <a [href]="'/print/return/' + lastReturnId + '?autoprint=1'" target="_blank">Imprimir</a>
          <a [href]="'/print/return/' + lastReturnId + '?autoprint=1&reprint=1'" target="_blank">Reimprimir</a>
        </div>
      </section>

      <section class="card" *ngIf="!sale && saleId > 0 && !isBusy">
        <p class="empty">Carga la venta para ver sus items y procesar la devolucion.</p>
      </section>
    </main>
  `,
  styles: [
    `.page{min-height:100vh;padding:0 0 2rem;position:relative}`,
    `.hero-bg{position:absolute;top:0;left:0;right:0;height:190px;background:linear-gradient(135deg,#1B4D3E 0%,#234F45 100%);z-index:0}`,
    `.hero{position:relative;z-index:1;max-width:1000px;margin:0 auto;padding:1.8rem 1.5rem 1.1rem}`,
    `.hero h1{margin:0;color:#fff;font-size:2rem}`,
    `.hero p{margin:.35rem 0 0;color:rgba(255,255,255,.75)}`,
    `.card{position:relative;z-index:1;max-width:1000px;margin:0 auto 1rem;background:#fff;border:1px solid #dce6e2;border-radius:14px;padding:1rem 1.1rem}`,
    `.card h2{margin:.2rem 0 1rem;color:#1B4D3E;font-size:1.2rem}`,
    `.search-row{display:flex;gap:.6rem;flex-wrap:wrap}`,
    `.search-row input{flex:1 1 220px}`,
    `.filter-check{display:flex;align-items:center;gap:.45rem;margin-top:.6rem;color:#49665b;font-size:.88rem}`,
    `.filter-check input{width:16px;height:16px}`,
    `.btn-secondary{background:#f2fbf8;color:#1B4D3E;font-weight:600}`,
    `.recent-sales{display:grid;grid-template-columns:repeat(auto-fill,minmax(260px,1fr));gap:.5rem;margin-top:.8rem}`,
    `.recent-sale{text-align:left;display:flex;flex-direction:column;gap:.2rem;background:#f7fbf9}`,
    `.recent-sale:hover:not(:disabled){background:#e9f6f1}`,
    `.recent-sale strong{color:#1f4538}`,
    `.recent-sale span{font-size:.82rem;color:#5f736b}`,
    `.chip-row{display:flex;gap:.4rem;align-items:center;flex-wrap:wrap;margin-top:.25rem}`,
    `.status-chip{margin-top:.25rem;display:inline-flex;align-self:flex-start;padding:.18rem .48rem;border-radius:999px;font-size:.74rem;font-weight:700;background:#e8f1ed;color:#315f4f}`,
    `.status-chip.paid{background:#e6f5ec;color:#0b7a36}`,
    `.status-chip.completed{background:#e6effa;color:#1f4e8a}`,
    `.age-chip{display:inline-flex;padding:.18rem .48rem;border-radius:999px;font-size:.74rem;font-weight:700;background:#edf5ea;color:#3f6d42}`,
    `.age-chip.age-mid{background:#fff4df;color:#8a5a00}`,
    `.line{display:grid;grid-template-columns:1fr 130px 170px;gap:.6rem;align-items:center;padding:.55rem 0;border-bottom:1px solid #edf3f0}`,
    `.line-name{font-weight:600;color:#1f4538}`,
    `.line-name small{font-weight:500;color:#70857d}`,
    `input,select,button{padding:.72rem .85rem;border-radius:10px;border:1px solid #d7e4de;font-size:.95rem}`,
    `input,select{background:#f7fbf9;color:#1B4D3E}`,
    `.btn-primary{background:#1b7f57;border-color:#1b7f57;color:#fff;font-weight:700}`,
    `button{cursor:pointer}`,
    `button:disabled{opacity:.6;cursor:not-allowed}`,
    `.hint{margin:.65rem 0 0;font-size:.82rem;color:#60736c}`,
    `.empty{margin:.2rem 0;color:#6d827a}`,
    `.print-links{display:flex;gap:.6rem;flex-wrap:wrap;margin-top:.7rem}`,
    `.print-links a{color:#1b7f57;font-weight:600;text-decoration:none}`,
    `.alert{position:relative;z-index:1;max-width:1000px;margin:0 auto .8rem;padding:.7rem .9rem;border-radius:10px}`,
    `.ok{background:#e9f7ef;color:#0a7a32}`,
    `.error{background:#fbe9ea;color:#b3261e}`,
    `@media (max-width:760px){.line{grid-template-columns:1fr}.hero h1{font-size:1.65rem}}`
  ]
})
export class PosCajaDevolucionesComponent {
  saleId = 0;
  recentSalesQuery = '';
  onlyWithCustomerName = false;
  recentSales: ReturnEligibleSale[] = [];
  filteredRecentSales: ReturnEligibleSale[] = [];
  sale: SaleResponse | null = null;
  lines: ReturnLineDraft[] = [];
  lastReturnId: number | null = null;
  message = '';
  error = '';
  private pending = 0;

  get isBusy(): boolean {
    return this.pending > 0;
  }

  constructor(private readonly api: PosCajaService, private readonly operatorSession: OperatorSessionService, public readonly health: HealthService) {
    void this.loadRecentSales();
  }

  async loadRecentSales(): Promise<void> {
    if (!this.health.isOnline) return;
    try {
      const recent = await this.withBusy(() => this.api.getReturnEligibleSales(48));
      this.recentSales = recent;
      this.filterRecentSales();
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudieron cargar ventas elegibles para devolución';
    }
  }

  filterRecentSales(): void {
    const q = this.recentSalesQuery.trim().toLowerCase();
    this.filteredRecentSales = this.recentSales
      .filter(s => !this.onlyWithCustomerName || !!s.customerName)
      .filter(s => !q || `${s.id} ${s.customerName ?? ''}`.toLowerCase().includes(q))
      .slice(0, 40);
  }

  saleStatusLabel(status: string): string {
    if (status === 'Paid') return 'Pagada';
    if (status === 'Completed') return 'Completada';
    if (status === 'Pending') return 'Pendiente';
    return status;
  }

  saleAgeLabel(createdAt: string): string {
    return this.saleAgeBucket(createdAt) === 'lt24h' ? '<24h' : '24-48h';
  }

  saleAgeBucket(createdAt: string): 'lt24h' | '24-48h' {
    const created = new Date(createdAt).getTime();
    const diffHours = (Date.now() - created) / 3600000;
    return diffHours < 24 ? 'lt24h' : '24-48h';
  }

  pickRecentSale(sale: ReturnEligibleSale): void {
    this.saleId = sale.id;
    void this.loadSale();
  }

  async loadSale(): Promise<void> {
    if (!this.health.isOnline) return;
    if (!this.saleId) return;
    this.clearMessages();

    try {
      this.sale = await this.withBusy(() => this.api.getSaleById(this.saleId));
      this.lines = (this.sale.items ?? []).map(i => ({
        originalSaleItemId: i.id,
        productName: i.productName,
        maxQty: Number(i.quantity),
        qtyReturned: 0,
        condition: 'Resellable'
      }));
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo cargar venta';
    }
  }

  async submitReturn(): Promise<void> {
    if (!this.health.isOnline) return;
    if (!this.sale) return;

    const payloadLines = this.lines
      .filter(l => l.qtyReturned > 0)
      .map(l => ({ originalSaleItemId: l.originalSaleItemId, qtyReturned: Number(l.qtyReturned), condition: l.condition }));

    if (payloadLines.length === 0) return;

    this.clearMessages();
    try {
      await this.withBusy(() => this.operatorSession.ensureSession());
      const result: any = await this.withBusy(() =>
        this.api.createReturn(this.sale!.id, {
          refundPreference: 'Cash',
          lines: payloadLines
        })
      );
      this.lastReturnId = result?.id ?? null;
      this.message = 'Devolucion registrada';
      await this.loadSale();
    } catch (err: any) {
      this.error = err?.error?.message ?? err?.message ?? 'No se pudo registrar devolucion';
    }
  }

  private clearMessages(): void {
    this.message = '';
    this.error = '';
  }

  private async withBusy<T>(fn: () => Promise<T>): Promise<T> {
    this.pending += 1;
    try {
      return await fn();
    } finally {
      this.pending -= 1;
    }
  }
}
