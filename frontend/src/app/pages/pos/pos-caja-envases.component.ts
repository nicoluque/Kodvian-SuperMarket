import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HealthService } from '../../core/services/health.service';
import { CustomerContainerSummary, CustomerRef, PosCajaService } from '../../core/services/pos-caja.service';
import { OperatorSessionService } from '../../core/services/operator-session.service';
import { PosModuleNavComponent } from '../../shared/components/pos-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-pos-caja-envases',
  imports: [CommonModule, FormsModule, PosModuleNavComponent],
  template: `
    <main class="page">
      <div class="hero-bg"></div>
      <app-pos-module-nav />
      <header class="hero">
        <h1>Envases</h1>
        <p>Registra devoluciones de envases con control por cliente.</p>
      </header>

      <p *ngIf="!health.isOnline" class="alert error">Sin conexion: envases deshabilitados.</p>
      <p *ngIf="message" class="alert ok">{{ message }}</p>
      <p *ngIf="error" class="alert error">{{ error }}</p>

      <section class="card">
        <div class="search-row">
          <input placeholder="Buscar cliente" [(ngModel)]="query" (input)="filterCustomers()" />
          <button class="btn-secondary" [disabled]="isBusy || !health.isOnline" (click)="loadCustomers()">Recargar</button>
        </div>

        <div class="customer-list">
          <button *ngFor="let c of filtered" class="customer-item" [class.active]="selectedCustomer?.id === c.id" [disabled]="isBusy || !health.isOnline" (click)="selectCustomer(c)">#{{ c.id }} {{ c.fullName }}</button>
        </div>
        <p class="empty" *ngIf="filtered.length === 0">No se encontraron clientes con ese criterio.</p>
      </section>

      <section *ngIf="selectedCustomer && summary" class="card">
        <h2>{{ selectedCustomer.fullName }}</h2>
        <div class="item" *ngFor="let o of summary.owedByType">
          <span>{{ o.containerTypeName }} <small>(debe {{ o.owedQty }})</small></span>
          <input type="number" placeholder="Cant." [(ngModel)]="qtyByType[o.containerTypeId]" />
          <button class="btn-primary" [disabled]="isBusy || !health.isOnline" (click)="returnContainer(o.containerTypeId)">Registrar</button>
        </div>
        <p class="empty" *ngIf="summary.owedByType.length === 0">Este cliente no tiene envases pendientes.</p>
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
    `.card h2{margin:.2rem 0 .7rem;color:#1B4D3E}`,
    `.search-row{display:flex;gap:.6rem;flex-wrap:wrap}`,
    `.customer-list{display:grid;grid-template-columns:repeat(auto-fill,minmax(220px,1fr));gap:.45rem;margin-top:.7rem}`,
    `.customer-item{text-align:left;background:#f7fbf9}`,
    `.customer-item:hover:not(:disabled){background:#eef8f4}`,
    `.customer-item.active{background:#dff4ec;border-color:#98d6c2;font-weight:700}`,
    `.item{display:grid;grid-template-columns:1fr 110px 150px;gap:.6rem;align-items:center;padding:.55rem 0;border-bottom:1px solid #edf3f0}`,
    `.item small{color:#70857d}`,
    `input,button{padding:.72rem .85rem;border-radius:10px;border:1px solid #d7e4de;font-size:.95rem}`,
    `input{background:#f7fbf9;color:#1B4D3E}`,
    `.btn-primary{background:#1b7f57;border-color:#1b7f57;color:#fff;font-weight:700}`,
    `.btn-secondary{background:#f2fbf8;color:#1B4D3E;font-weight:600}`,
    `button{cursor:pointer}`,
    `button:disabled{opacity:.6;cursor:not-allowed}`,
    `.empty{margin:.6rem 0 0;color:#70857d}`,
    `.alert{position:relative;z-index:1;max-width:1000px;margin:0 auto .8rem;padding:.7rem .9rem;border-radius:10px}`,
    `.ok{background:#e9f7ef;color:#0a7a32}`,
    `.error{background:#fbe9ea;color:#b3261e}`,
    `@media (max-width:760px){.item{grid-template-columns:1fr}.hero h1{font-size:1.65rem}}`
  ]
})
export class PosCajaEnvasesComponent {
  query = '';
  customers: CustomerRef[] = [];
  filtered: CustomerRef[] = [];
  selectedCustomer: CustomerRef | null = null;
  summary: CustomerContainerSummary | null = null;
  qtyByType: Record<number, number> = {};
  message = '';
  error = '';
  private pending = 0;

  get isBusy(): boolean {
    return this.pending > 0;
  }

  constructor(private readonly api: PosCajaService, private readonly operatorSession: OperatorSessionService, public readonly health: HealthService) {
    void this.loadCustomers();
  }

  async loadCustomers(): Promise<void> {
    if (!this.health.isOnline) return;
    this.clearMessages();
    try {
      this.customers = await this.withBusy(() => this.api.getCustomers());
      this.filterCustomers();
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo cargar clientes';
    }
  }

  filterCustomers(): void {
    const q = this.query.trim().toLowerCase();
    this.filtered = this.customers.filter(c => !q || `${c.id} ${c.fullName} ${c.dni ?? ''}`.toLowerCase().includes(q)).slice(0, 20);
  }

  async selectCustomer(c: CustomerRef): Promise<void> {
    if (!this.health.isOnline) return;
    this.selectedCustomer = c;
    this.qtyByType = {};
    this.clearMessages();
    try {
      this.summary = await this.withBusy(() => this.api.getCustomerContainerSummary(c.id));
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo cargar resumen de envases';
    }
  }

  async returnContainer(containerTypeId: number): Promise<void> {
    if (!this.health.isOnline) return;
    if (!this.selectedCustomer) return;
    const qty = Number(this.qtyByType[containerTypeId] ?? 0);
    if (qty <= 0) return;

    this.clearMessages();
    try {
      await this.withBusy(() => this.operatorSession.ensureSession());
      await this.withBusy(() => this.api.registerContainerReturn(this.selectedCustomer!.id, { containerTypeId, qty }));
      this.message = 'Envase registrado';
      this.summary = await this.withBusy(() => this.api.getCustomerContainerSummary(this.selectedCustomer!.id));
      this.qtyByType[containerTypeId] = 0;
    } catch (err: any) {
      this.error = err?.error?.message ?? err?.message ?? 'No se pudo registrar envase';
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
