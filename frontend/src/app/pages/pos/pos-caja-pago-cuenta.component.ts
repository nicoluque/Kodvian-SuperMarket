import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HealthService } from '../../core/services/health.service';
import { OperatorSessionService } from '../../core/services/operator-session.service';
import { CustomerRef, CustomerSummary, PosCajaService } from '../../core/services/pos-caja.service';
import { PosModuleNavComponent } from '../../shared/components/pos-module-nav.component';

interface PaymentDraft {
  paymentMethod: string;
  amount: number;
  reference?: string;
}

@Component({
  standalone: true,
  selector: 'app-pos-caja-pago-cuenta',
  imports: [CommonModule, FormsModule, PosModuleNavComponent],
  template: `
    <main class="page">
      <div class="hero-bg"></div>
      <app-pos-module-nav />
      <header class="hero">
        <h1>Pago de cuenta corriente</h1>
        <p>Busca cliente, arma medios de pago y confirma en una sola pantalla.</p>
      </header>

      <p *ngIf="!health.isOnline" class="alert error">Sin conexion: pagos de cuenta deshabilitados.</p>
      <p *ngIf="message" class="alert ok">{{ message }}</p>
      <p *ngIf="error" class="alert error">{{ error }}</p>

      <section class="card">
        <div class="search-row">
          <input placeholder="Buscar cliente" [(ngModel)]="query" (input)="filterCustomers()" />
          <button class="btn-secondary" [disabled]="isBusy || !health.isOnline" (click)="loadCustomers()">Recargar</button>
        </div>

        <div class="customer-list">
          <button *ngFor="let c of filtered" class="customer-item" [class.active]="selectedCustomer?.id === c.id" [disabled]="isBusy || !health.isOnline" (click)="selectCustomer(c)">
            <span>#{{ c.id }} {{ c.fullName }}</span>
            <small class="status-chip" [class.critical]="(c.effectiveStatus || c.status) === 'Critical'">{{ customerStatusLabel(c) }} · {{ c.creditUsedPct || 0 }}%</small>
          </button>
        </div>
        <p class="empty" *ngIf="filtered.length === 0">No se encontraron clientes con ese criterio.</p>
      </section>

      <section *ngIf="selectedCustomer && summary" class="card">
        <h2>{{ selectedCustomer.fullName }}</h2>
        <p class="summary">Deuda: {{ summary.totalDebt | number:'1.2-2' }} · Credito disponible: {{ summary.availableCredit | number:'1.2-2' }}</p>
        <p class="summary" *ngIf="selectedCustomer.isCritical" style="color:#8a5a00">Cliente en alerta de crédito (>=90%).</p>
        <p class="summary" *ngIf="selectedCustomer.isCreditBlocked" style="color:#9f1f1f">Cuenta corriente bloqueada por límite alcanzado o estado del cliente.</p>

        <div class="payment-row">
          <select [(ngModel)]="paymentMethod">
            <option value="Cash">Efectivo</option>
            <option value="Card">Tarjeta</option>
            <option value="Transfer">Transferencia</option>
            <option value="QrMp">QR</option>
          </select>
          <input type="number" placeholder="Monto" [(ngModel)]="amount" />
          <input placeholder="Referencia" [(ngModel)]="reference" />
          <button class="btn-secondary" [disabled]="isBusy || !health.isOnline" (click)="addPayment()">Agregar pago</button>
        </div>

        <div class="payments">
          <div *ngFor="let p of payments; let i = index" class="item">
            <span>{{ paymentMethodLabel(p.paymentMethod) }} {{ p.amount | number:'1.2-2' }}</span>
            <button class="btn-link" [disabled]="isBusy || !health.isOnline" (click)="removePayment(i)">Quitar</button>
          </div>
        </div>

        <button class="btn-primary" [disabled]="isBusy || payments.length === 0 || !health.isOnline" (click)="submitPayment()">Confirmar pago cuenta</button>
        <div class="print-links" *ngIf="lastPaymentMovementId">
          <a [href]="'/print/customer-payment/' + lastPaymentMovementId" target="_blank">Ver comprobante</a>
          <a [href]="'/print/customer-payment/' + lastPaymentMovementId + '?autoprint=1'" target="_blank">Imprimir</a>
          <a [href]="'/print/customer-payment/' + lastPaymentMovementId + '?autoprint=1&reprint=1'" target="_blank">Reimprimir</a>
        </div>
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
    `.card h2{margin:.2rem 0 .5rem;color:#1B4D3E}`,
    `.summary{margin:0 0 .8rem;color:#516860}`,
    `.search-row,.payment-row{display:flex;gap:.6rem;flex-wrap:wrap}`,
    `.customer-list{display:grid;grid-template-columns:repeat(auto-fill,minmax(220px,1fr));gap:.45rem;margin-top:.7rem}`,
    `.customer-item{text-align:left;background:#f7fbf9}`,
    `.customer-item:hover:not(:disabled){background:#eef8f4}`,
    `.customer-item.active{background:#dff4ec;border-color:#98d6c2;font-weight:700}`,
    `.status-chip{display:inline-flex;align-self:flex-start;padding:.15rem .45rem;border-radius:999px;font-size:.72rem;font-weight:700;background:#e8f1ed;color:#335f52}`,
    `.status-chip.critical{background:#fff0dc;color:#8a5a00}`,
    `.payments{display:flex;flex-direction:column;gap:.45rem;margin:.8rem 0}`,
    `.item{display:flex;justify-content:space-between;align-items:center;gap:.7rem;padding:.55rem .7rem;background:#f8fbfa;border-radius:10px}`,
    `.btn-link{background:transparent;border:0;color:#a12027;font-weight:700}`,
    `input,select,button{padding:.72rem .85rem;border-radius:10px;border:1px solid #d7e4de;font-size:.95rem}`,
    `input,select{background:#f7fbf9;color:#1B4D3E}`,
    `.btn-primary{background:#1b7f57;border-color:#1b7f57;color:#fff;font-weight:700}`,
    `.btn-secondary{background:#f2fbf8;color:#1B4D3E;font-weight:600}`,
    `button{cursor:pointer}`,
    `button:disabled{opacity:.6;cursor:not-allowed}`,
    `.print-links{display:flex;gap:.6rem;flex-wrap:wrap;margin-top:.7rem}`,
    `.print-links a{color:#1b7f57;font-weight:600;text-decoration:none}`,
    `.alert{position:relative;z-index:1;max-width:1000px;margin:0 auto .8rem;padding:.7rem .9rem;border-radius:10px}`,
    `.ok{background:#e9f7ef;color:#0a7a32}`,
    `.error{background:#fbe9ea;color:#b3261e}`,
    `@media (max-width:640px){.hero h1{font-size:1.65rem}}`
  ]
})
export class PosCajaPagoCuentaComponent {
  query = '';
  customers: CustomerRef[] = [];
  filtered: CustomerRef[] = [];
  selectedCustomer: CustomerRef | null = null;
  summary: CustomerSummary | null = null;

  paymentMethod = 'Cash';
  amount = 0;
  reference = '';
  payments: PaymentDraft[] = [];

  message = '';
  error = '';
  lastPaymentMovementId: number | null = null;
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
    this.payments = [];
    this.clearMessages();
    try {
      this.summary = await this.withBusy(() => this.api.getCustomerAccountSummary(c.id));
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo cargar resumen';
    }
  }

  addPayment(): void {
    if (this.amount <= 0) return;
    this.payments.push({ paymentMethod: this.paymentMethod, amount: Number(this.amount), reference: this.reference || undefined });
    this.amount = 0;
    this.reference = '';
  }

  removePayment(index: number): void {
    this.payments.splice(index, 1);
  }

  async submitPayment(): Promise<void> {
    if (!this.health.isOnline) return;
    if (!this.selectedCustomer || this.payments.length === 0) return;
    this.clearMessages();

    try {
      await this.withBusy(() => this.operatorSession.ensureSession());
      const total = this.payments.reduce((sum, p) => sum + p.amount, 0);
      const mixRef = this.payments.map(p => `${p.paymentMethod}:${p.amount}`).join(' | ');
      const payment: any = await this.withBusy(() => this.api.createCustomerAccountPayment(this.selectedCustomer!.id, { amount: total, reference: mixRef }));
      this.lastPaymentMovementId = payment?.id ?? null;
      this.message = `Pago registrado por ${total.toFixed(2)}`;
      this.payments = [];
      this.summary = await this.withBusy(() => this.api.getCustomerAccountSummary(this.selectedCustomer!.id));
    } catch (err: any) {
      this.error = err?.error?.message ?? err?.message ?? 'No se pudo registrar pago';
    }
  }

  paymentMethodLabel(method: string): string {
    if (method === 'Cash') return 'Efectivo';
    if (method === 'Card') return 'Tarjeta';
    if (method === 'Transfer') return 'Transferencia';
    if (method === 'QrMp') return 'QR';
    return method;
  }

  customerStatusLabel(customer: CustomerRef): string {
    const status = customer.effectiveStatus || customer.status || 'Active';
    if (status === 'Critical') return 'Crítico';
    if (status === 'Pending') return 'Pendiente';
    if (status === 'Inactive') return 'Desactivado';
    return 'Activado';
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
