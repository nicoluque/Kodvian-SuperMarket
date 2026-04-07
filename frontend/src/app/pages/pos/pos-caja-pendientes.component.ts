import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DialogService } from '../../core/services/dialog.service';
import { HealthService } from '../../core/services/health.service';
import { OperatorSessionService } from '../../core/services/operator-session.service';
import { PendingTransferSale, PosCajaService } from '../../core/services/pos-caja.service';
import { PosModuleNavComponent } from '../../shared/components/pos-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-pos-caja-pendientes',
  imports: [CommonModule, FormsModule, RouterLink, PosModuleNavComponent],
  template: `
    <main class="pending-container">
      <div class="hero-bg">
        <div class="hero-shape shape-1"></div>
        <div class="hero-shape shape-2"></div>
      </div>

      <app-pos-module-nav />

      <header class="hero">
        <div class="hero-content">
          <h1>Centro de pendientes</h1>
          <p class="hero-subtitle">Gestion de transferencias del turno y seguimiento de pendientes heredados.</p>
        </div>
        <div class="hero-actions">
          <button class="btn-secondary" [disabled]="isBusy || !health.isOnline" (click)="reloadAll()">Actualizar</button>
          <a class="btn-ghost" routerLink="/pos/caja/inbox">Ir a bandeja</a>
        </div>
      </header>

      <section class="summary-section">
        <div class="summary-grid">
          <article class="summary-card">
            <span class="summary-label">Transferencias del turno</span>
            <strong class="summary-value">{{ pendingTransfers.length }}</strong>
          </article>
          <article class="summary-card">
            <span class="summary-label">Transferencias heredadas</span>
            <strong class="summary-value">{{ inheritedTransfers.length }}</strong>
          </article>
          <article class="summary-card warning">
            <span class="summary-label">Tareas bloqueantes</span>
            <strong class="summary-value">{{ missingRequiredTasks.length }}</strong>
          </article>
        </div>
      </section>

      <section class="content-section">
        <div class="alert warning" *ngIf="!health.isOnline">Sin conexion: confirmacion y cancelacion de transferencias deshabilitada.</div>
        <div class="alert success" *ngIf="message">{{ message }}</div>
        <div class="alert error" *ngIf="error">{{ error }}</div>

        <section class="card">
          <div class="card-header">
            <h2>Tareas del turno</h2>
          </div>
          <div class="task-grid">
            <div class="task-item">
              <span>Cigarrillos contado</span>
              <strong [class.bad]="!hasCigaretteCount">{{ hasCigaretteCount ? 'Si' : 'No' }}</strong>
            </div>
            <div class="task-item">
              <span>Bloqueo por cigarrillos</span>
              <strong [class.bad]="blockedByCigarettesCount">{{ blockedByCigarettesCount ? 'Activo' : 'No' }}</strong>
            </div>
          </div>
          <ul class="task-list" *ngIf="missingRequiredTasks.length > 0">
            <li *ngFor="let t of missingRequiredTasks">{{ taskLabel(t) }}</li>
          </ul>
          <p class="empty" *ngIf="missingRequiredTasks.length === 0">Sin tareas bloqueantes activas.</p>
        </section>

        <section class="card">
          <div class="card-header">
            <h2>Transferencias del turno</h2>
          </div>
          <div class="list-container" *ngIf="pendingTransfers.length > 0">
            <div class="list-header">
              <span>Venta</span>
              <span>Total</span>
              <span>Acciones</span>
            </div>
            <div class="list-row" *ngFor="let p of pendingTransfers">
              <div>
                <strong>#{{ p.saleId }}</strong>
                <small>{{ p.createdAt | date:'short' }}</small>
              </div>
              <strong class="amount">{{ p.total | number:'1.2-2' }}</strong>
              <div class="row-actions">
                <button class="btn-action confirm" [disabled]="isBusy || !health.isOnline" (click)="confirmTransfer(p)">Confirmar</button>
                <button class="btn-action cancel" [disabled]="isBusy || !health.isOnline" (click)="cancelTransfer(p)">Cancelar</button>
              </div>
            </div>
          </div>
          <p class="empty" *ngIf="!isBusy && pendingTransfers.length === 0">Sin pendientes de transferencia en este turno.</p>
        </section>

        <section class="card">
          <div class="card-header">
            <h2>Transferencias heredadas</h2>
          </div>
          <div class="list-container" *ngIf="inheritedTransfers.length > 0">
            <div class="list-header">
              <span>Venta</span>
              <span>Total</span>
              <span>Acciones</span>
            </div>
            <div class="list-row inherited" *ngFor="let p of inheritedTransfers">
              <div>
                <strong>#{{ p.saleId }}</strong>
                <small>Generada {{ p.createdAt | date:'short' }}</small>
              </div>
              <strong class="amount">{{ p.total | number:'1.2-2' }}</strong>
              <div class="row-actions">
                <button class="btn-action confirm" [disabled]="isBusy || !health.isOnline" (click)="confirmTransfer(p)">Confirmar</button>
                <button class="btn-action cancel" [disabled]="isBusy || !health.isOnline" (click)="cancelTransfer(p)">Cancelar</button>
              </div>
            </div>
          </div>
          <p class="empty" *ngIf="!isBusy && inheritedTransfers.length === 0">Sin transferencias heredadas de otros turnos.</p>
        </section>
      </section>
    </main>
  `,
  styles: [
    `.pending-container{min-height:100vh;position:relative;padding-bottom:1.5rem;background:#f3f5f7}`,
    `.hero-bg{position:absolute;inset:0 0 auto 0;height:340px;background:linear-gradient(135deg,#1B4D3E 0%,#2a5f4f 100%);z-index:0;overflow:hidden}`,
    `.hero-shape{position:absolute;border-radius:50%;background:rgba(255,255,255,.07)}`,
    `.shape-1{width:320px;height:320px;top:-150px;right:8%}`,
    `.shape-2{width:240px;height:240px;top:90px;left:5%}`,
    `.hero,.summary-section,.content-section{position:relative;z-index:1;max-width:1000px;margin:0 auto;padding:0 1.5rem}`,
    `.hero{display:flex;justify-content:space-between;gap:1rem;align-items:flex-start;padding-top:.6rem;color:#fff}`,
    `.hero-content h1{margin:0;font-size:2.1rem;font-weight:800}`,
    `.hero-subtitle{margin:.45rem 0 0;color:#d4e4de}`,
    `.hero-actions{display:flex;gap:.65rem;flex-wrap:wrap}`,
    `.btn-secondary,.btn-ghost{min-height:42px;border-radius:12px;padding:0 .95rem;font-weight:700;font-size:.9rem;display:inline-flex;align-items:center;justify-content:center;text-decoration:none}`,
    `.btn-secondary{border:1px solid rgba(255,255,255,.35);background:rgba(255,255,255,.17);color:#fff;cursor:pointer}`,
    `.btn-secondary:disabled{opacity:.6;cursor:not-allowed}`,
    `.btn-ghost{border:1px dashed rgba(255,255,255,.45);background:transparent;color:#fff}`,
    `.summary-section{margin-top:1.2rem}`,
    `.summary-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:.9rem}`,
    `.summary-card{background:#fff;border-radius:14px;border:1px solid #e6ecea;padding:.85rem .95rem;display:flex;flex-direction:column;gap:.25rem;box-shadow:0 3px 10px rgba(0,0,0,.06)}`,
    `.summary-card.warning{background:#fff9ec;border-color:#f1dfb2}`,
    `.summary-label{font-size:.76rem;text-transform:uppercase;letter-spacing:.05em;color:#6d7c84;font-weight:700}`,
    `.summary-value{font-size:1.25rem;color:#1B4D3E;font-weight:800}`,
    `.content-section{margin-top:1rem;display:flex;flex-direction:column;gap:.9rem}`,
    `.alert{border-radius:10px;padding:.7rem .85rem;font-size:.9rem;font-weight:600}`,
    `.alert.warning{background:#fff3cd;color:#856404}`,
    `.alert.success{background:#e7f8ef;color:#1f7a45}`,
    `.alert.error{background:#fde8e7;color:#8f2f2f}`,
    `.card{background:#fff;border:1px solid #e5ecea;border-radius:14px;box-shadow:0 3px 12px rgba(0,0,0,.05);overflow:hidden}`,
    `.card-header{padding:1rem 1.1rem;border-bottom:1px solid #edf2f1}`,
    `.card-header h2{margin:0;color:#1B4D3E;font-size:1.25rem}`,
    `.task-grid{display:grid;grid-template-columns:1fr 1fr;gap:.7rem;padding:1rem 1.1rem .6rem}`,
    `.task-item{background:#f8fbfa;border:1px solid #e4ece9;border-radius:10px;padding:.7rem .8rem;display:flex;justify-content:space-between;gap:.8rem;align-items:center}`,
    `.task-item span{color:#60706a;font-size:.88rem}`,
    `.task-item strong{color:#1B4D3E}`,
    `.task-item strong.bad{color:#8f3b2f}`,
    `.task-list{margin:0;padding:0 1.8rem 1rem;color:#5f6f69}`,
    `.task-list li{margin:.28rem 0}`,
    `.list-container{padding:.35rem 1rem .9rem}`,
    `.list-header,.list-row{display:grid;grid-template-columns:1fr 150px 230px;align-items:center;gap:.8rem}`,
    `.list-header{font-size:.78rem;font-weight:700;color:#6b7a82;text-transform:uppercase;letter-spacing:.05em;padding:.4rem .2rem .55rem;border-bottom:1px solid #edf2f1}`,
    `.list-row{padding:.75rem .2rem;border-bottom:1px solid #eef3f1}`,
    `.list-row:last-child{border-bottom:none}`,
    `.list-row strong{color:#1B4D3E}`,
    `.list-row small{display:block;color:#7a888e;font-size:.78rem}`,
    `.list-row.inherited{background:#fbfcfc}`,
    `.amount{font-variant-numeric:tabular-nums;color:#1f8f58}`,
    `.row-actions{display:flex;justify-content:flex-end;gap:.5rem}`,
    `.btn-action{border-radius:9px;border:1px solid transparent;padding:.44rem .7rem;font-size:.82rem;font-weight:700;cursor:pointer}`,
    `.btn-action.confirm{background:#e8f2ef;border-color:#cfe2dc;color:#1B4D3E}`,
    `.btn-action.cancel{background:#fdf0f0;border-color:#f0d0d0;color:#8f2f2f}`,
    `.btn-action:disabled{opacity:.55;cursor:not-allowed}`,
    `.empty{padding:1rem 1.1rem;color:#728389}`,
    `@media (max-width: 900px){.hero{flex-direction:column}.summary-grid{grid-template-columns:1fr}.task-grid{grid-template-columns:1fr}.list-header{display:none}.list-row{grid-template-columns:1fr}.row-actions{justify-content:flex-start}}`
  ]
})
export class PosCajaPendientesComponent {
  pendingTransfers: PendingTransferSale[] = [];
  inheritedTransfers: PendingTransferSale[] = [];
  missingRequiredTasks: string[] = [];
  blockedByCigarettesCount = false;
  hasCigaretteCount = false;

  message = '';
  error = '';
  private pending = 0;

  get isBusy(): boolean {
    return this.pending > 0;
  }

  constructor(
    private readonly api: PosCajaService,
    private readonly operatorSession: OperatorSessionService,
    private readonly dialog: DialogService,
    public readonly health: HealthService
  ) {
    void this.reloadAll();
  }

  async reloadAll(): Promise<void> {
    if (!this.health.isOnline) return;
    this.clearMessages();

    try {
      const session = await this.withBusy(() => this.api.getCurrentCashSession());
      const [currentTransfers, deviceTransfers] = await Promise.all([
        this.withBusy(() => this.api.getPendingTransfers(undefined, 'current-session')),
        this.withBusy(() => this.api.getPendingTransfers(undefined, 'device'))
      ]);
      this.pendingTransfers = currentTransfers;
      const currentIds = new Set(currentTransfers.map(t => t.saleId));
      this.inheritedTransfers = deviceTransfers.filter(t => !currentIds.has(t.saleId));

      const counts = await this.withBusy(() => this.api.getCigaretteCountsReport());
      this.hasCigaretteCount = counts.some(c => c.cashSessionId === session.id);
      this.blockedByCigarettesCount = !this.hasCigaretteCount;

      const tasks: string[] = [];
      if (this.pendingTransfers.length > 0) tasks.push('PendingTransfers');
      if (this.blockedByCigarettesCount) tasks.push('CigaretteCount');
      this.missingRequiredTasks = tasks;
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudieron cargar las transferencias pendientes';
    }
  }

  async confirmTransfer(p: PendingTransferSale): Promise<void> {
    if (!this.health.isOnline) return;
    const payment = p.payments.find(x => x.paymentMethod === 'Transfer' && x.status === 'Pending') ?? p.payments[0];
    if (!payment) return;

    this.clearMessages();
    try {
      await this.withBusy(() => this.operatorSession.ensureSession());
      await this.withBusy(() => this.api.confirmTransfer(p.saleId, payment.id));
      this.message = `Transferencia de venta #${p.saleId} confirmada`;
      await this.reloadAll();
    } catch (err: any) {
      this.error = err?.error?.message ?? err?.message ?? 'No se pudo confirmar la transferencia';
    }
  }

  async cancelTransfer(p: PendingTransferSale): Promise<void> {
    if (!this.health.isOnline) return;
    const reason = await this.dialog.prompt({
      title: 'Cancelar transferencia',
      message: `Venta #${p.saleId}. Ingresa el motivo de cancelacion para dejar registro.`,
      inputLabel: 'Motivo',
      inputPlaceholder: 'Ej: comprobante invalido o transferencia rechazada',
      yesLabel: 'Cancelar transferencia',
      noLabel: 'Volver',
      inputRequired: true
    });
    if (!reason) return;

    this.clearMessages();
    try {
      await this.withBusy(() => this.operatorSession.ensureSession());
      await this.withBusy(() => this.api.cancelPendingTransfer(p.saleId, reason));
      this.message = `Transferencia de venta #${p.saleId} cancelada`;
      await this.reloadAll();
    } catch (err: any) {
      const status = Number(err?.status ?? 0);
      const message = `${err?.error?.message ?? ''}`.toLowerCase();
      if (status === 403 || message.includes('autorizacion') || message.includes('supervisor')) {
        await this.cancelTransferWithAuthorization(p, reason);
        return;
      }
      this.error = err?.error?.message ?? err?.message ?? 'No se pudo cancelar la transferencia';
    }
  }

  private async cancelTransferWithAuthorization(p: PendingTransferSale, reason: string): Promise<void> {
    const approverUsername = await this.dialog.prompt({
      title: 'Autorizacion requerida',
      message: 'Ingresa usuario supervisor o administrador para autorizar esta cancelacion.',
      inputLabel: 'Usuario autorizador',
      inputPlaceholder: 'usuario',
      yesLabel: 'Continuar',
      noLabel: 'Cancelar',
      inputRequired: true
    });
    if (!approverUsername) return;

    if (this.normalizeText(approverUsername) === this.normalizeText(this.operatorSession.getOperatorName())) {
      this.error = 'Para cancelar una transferencia se requiere autorizacion de un supervisor o administrador distinto al operador activo.';
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

    this.clearMessages();
    try {
      await this.withBusy(() => this.operatorSession.ensureSession());
      await this.withBusy(() => this.api.cancelPendingTransferWithAuthorization(p.saleId, {
        reason,
        approverUsername,
        approverPassword,
        approverPin
      }));
      this.message = `Transferencia de venta #${p.saleId} cancelada con autorizacion`;
      await this.reloadAll();
    } catch (err: any) {
      this.error = err?.error?.message ?? err?.message ?? 'No se pudo validar la autorizacion para cancelar';
    }
  }

  private clearMessages(): void {
    this.message = '';
    this.error = '';
  }

  private normalizeText(value: string | null | undefined): string {
    return `${value ?? ''}`.toLowerCase().trim().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
  }

  taskLabel(task: string): string {
    if (task === 'CigaretteCount') return 'Conteo de cigarrillos';
    if (task === 'PendingTransfers') return 'Transferencias pendientes';
    return task;
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
