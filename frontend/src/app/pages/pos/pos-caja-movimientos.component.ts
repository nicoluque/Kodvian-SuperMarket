import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HealthService } from '../../core/services/health.service';
import { CashMovement, PosCajaService } from '../../core/services/pos-caja.service';
import { OperatorSessionService } from '../../core/services/operator-session.service';
import { PosModuleNavComponent } from '../../shared/components/pos-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-pos-caja-movimientos',
  imports: [CommonModule, FormsModule, PosModuleNavComponent],
  template: `
    <main class="page">
      <div class="hero-bg">
        <div class="hero-shape shape-1"></div>
        <div class="hero-shape shape-2"></div>
      </div>

      <app-pos-module-nav />

      <header class="hero">
        <div>
          <h1>Movimientos de caja</h1>
          <p>Registra gastos, ingresos o correcciones del turno activo.</p>
        </div>
        <button class="btn-secondary" [disabled]="isBusy || !health.isOnline" (click)="loadMovements()">Recargar turno</button>
      </header>

      <p *ngIf="!health.isOnline" class="alert error">Sin conexion: movimientos remotos deshabilitados.</p>
      <p *ngIf="message" class="alert ok">{{ message }}</p>
      <p *ngIf="error" class="alert error">{{ error }}</p>

      <section class="card">
        <h2>Nuevo movimiento</h2>
        <div class="form-grid">
          <select [(ngModel)]="type">
            <option value="Expense">Gasto</option>
            <option value="Deposit">Ingreso</option>
            <option value="Withdrawal">Retiro</option>
            <option value="Correction">Correccion</option>
          </select>
          <select [(ngModel)]="method">
            <option value="Cash">Efectivo</option>
            <option value="Card">Tarjeta</option>
            <option value="Transfer">Transferencia</option>
          </select>
          <input placeholder="Motivo" [(ngModel)]="reason" />
          <input type="number" placeholder="Monto" [(ngModel)]="amount" />
        </div>

        <div class="numpad-wrap">
          <div class="numpad">
            <button *ngFor="let key of keys" (click)="pressKey(key)">{{ key }}</button>
            <button class="clear" (click)="clearAmount()">C</button>
          </div>
        </div>

        <button class="btn-primary" [disabled]="isBusy || amount <= 0 || !reason.trim() || !health.isOnline" (click)="createMovement()">Registrar movimiento</button>
        <div class="print-links" *ngIf="lastMovementId">
          <a [href]="'/print/cash-movement/' + lastMovementId" target="_blank">Ver comprobante</a>
          <a [href]="'/print/cash-movement/' + lastMovementId + '?autoprint=1'" target="_blank">Imprimir</a>
          <a [href]="'/print/cash-movement/' + lastMovementId + '?autoprint=1&reprint=1'" target="_blank">Reimprimir</a>
        </div>
      </section>

      <section class="card">
        <h2>Turno actual</h2>
        <div class="item" *ngFor="let m of movements">
          <span>{{ movementTypeLabel(m.type) }} · {{ paymentMethodLabel(m.method) }}</span>
          <strong [class.negative]="m.signedAmount < 0">{{ m.signedAmount | number:'1.2-2' }}</strong>
          <small>{{ m.reason }}</small>
        </div>
        <p class="empty" *ngIf="!isBusy && movements.length === 0">No hay movimientos cargados en este turno.</p>
      </section>
    </main>
  `,
  styles: [
    `.page{min-height:100vh;padding:0 0 2rem;position:relative}`,
    `.hero-bg{position:absolute;top:0;left:0;right:0;height:220px;background:linear-gradient(135deg,#1B4D3E 0%,#234F45 100%);overflow:hidden;z-index:0}`,
    `.hero-shape{position:absolute;border-radius:50%;filter:blur(80px);opacity:.35}`,
    `.shape-1{width:280px;height:280px;background:#BFEBF1;top:-110px;right:-80px}`,
    `.shape-2{width:220px;height:220px;background:#a8d8e0;bottom:-80px;left:-60px}`,
    `.hero{position:relative;z-index:1;max-width:1000px;margin:0 auto;padding:1.8rem 1.5rem 1.2rem;display:flex;justify-content:space-between;gap:1rem;align-items:flex-start;flex-wrap:wrap}`,
    `.hero h1{margin:0;color:#fff;font-size:2rem}`,
    `.hero p{margin:.4rem 0 0;color:rgba(255,255,255,.75)}`,
    `.card{position:relative;z-index:1;max-width:1000px;margin:0 auto 1rem;background:#fff;border:1px solid #dce6e2;border-radius:14px;padding:1rem 1.1rem;box-shadow:0 10px 24px rgba(0,0,0,.05)}`,
    `.card h2{margin:.2rem 0 1rem;color:#1B4D3E;font-size:1.2rem}`,
    `.form-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:.6rem;margin-bottom:.8rem}`,
    `input,select,button{padding:.75rem .85rem;border-radius:10px;border:1px solid #d7e4de;font-size:.95rem}`,
    `input,select{background:#f7fbf9;color:#1B4D3E}`,
    `.btn-primary{background:#1b7f57;border-color:#1b7f57;color:#fff;font-weight:700}`,
    `.btn-secondary{background:#f2fbf8;color:#1B4D3E;font-weight:600}`,
    `button{cursor:pointer}`,
    `button:disabled{opacity:.6;cursor:not-allowed}`,
    `.numpad-wrap{margin-bottom:.9rem}`,
    `.numpad{display:grid;grid-template-columns:repeat(4,1fr);gap:.45rem;max-width:380px}`,
    `.numpad .clear{background:#fdf2f2;border-color:#f3c8cb;color:#8f1d22}`,
    `.item{display:grid;grid-template-columns:1fr auto;gap:.35rem .7rem;padding:.7rem 0;border-bottom:1px solid #eef3f0}`,
    `.item strong.negative{color:#b3261e}`,
    `.item small{grid-column:1 / -1;color:#5d6f68}`,
    `.empty{margin:.5rem 0 0;color:#70857d}`,
    `.print-links{display:flex;gap:.6rem;flex-wrap:wrap;margin-top:.7rem}`,
    `.print-links a{color:#1b7f57;font-weight:600;text-decoration:none}`,
    `.alert{position:relative;z-index:1;max-width:1000px;margin:0 auto .8rem;padding:.7rem .9rem;border-radius:10px}`,
    `.ok{background:#e9f7ef;color:#0a7a32}`,
    `.error{background:#fbe9ea;color:#b3261e}`,
    `@media (max-width: 840px){.form-grid{grid-template-columns:1fr 1fr}}`,
    `@media (max-width: 640px){.form-grid{grid-template-columns:1fr}.hero h1{font-size:1.65rem}}`
  ]
})
export class PosCajaMovimientosComponent {
  movements: CashMovement[] = [];
  type = 'Expense';
  method = 'Cash';
  reason = '';
  amount = 0;
  keys = ['7', '8', '9', '00', '4', '5', '6', '.', '1', '2', '3', '0'];

  message = '';
  error = '';
  lastMovementId: number | null = null;
  private pending = 0;

  get isBusy(): boolean {
    return this.pending > 0;
  }

  constructor(private readonly api: PosCajaService, private readonly operatorSession: OperatorSessionService, public readonly health: HealthService) {
    void this.loadMovements();
  }

  async loadMovements(): Promise<void> {
    if (!this.health.isOnline) return;
    this.clearMessages();
    try {
      const session = await this.withBusy(() => this.api.getCurrentCashSession());
      this.movements = await this.withBusy(() => this.api.getCashMovements(session.id));
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo cargar movimientos';
    }
  }

  pressKey(key: string): void {
    const next = `${this.amount || ''}${key}`;
    const parsed = Number(next);
    if (!Number.isNaN(parsed)) this.amount = parsed;
  }

  clearAmount(): void {
    this.amount = 0;
  }

  async createMovement(): Promise<void> {
    if (!this.health.isOnline) return;
    if (this.amount <= 0 || !this.reason.trim()) return;
    this.clearMessages();

    try {
      await this.withBusy(() => this.operatorSession.ensureSession());
      const session = await this.withBusy(() => this.api.getCurrentCashSession());
      const movement = await this.withBusy(() =>
        this.api.createCashMovement(session.id, {
          method: this.method,
          amount: Number(this.amount),
          type: this.type,
          reason: this.reason.trim()
        })
      );
      this.lastMovementId = movement.id;
      this.message = 'Movimiento registrado';
      this.amount = 0;
      this.reason = '';
      await this.loadMovements();
    } catch (err: any) {
      this.error = err?.error?.message ?? err?.message ?? 'No se pudo registrar movimiento';
    }
  }

  movementTypeLabel(type: string): string {
    if (type === 'Expense') return 'Gasto';
    if (type === 'Deposit') return 'Ingreso';
    if (type === 'Withdrawal') return 'Retiro';
    if (type === 'Correction') return 'Correccion';
    return type;
  }

  paymentMethodLabel(method: string): string {
    if (method === 'Cash') return 'Efectivo';
    if (method === 'Card') return 'Tarjeta';
    if (method === 'Transfer') return 'Transferencia';
    if (method === 'Credit') return 'Credito';
    return method;
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
