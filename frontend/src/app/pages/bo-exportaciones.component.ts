import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BoExportsService } from '../core/services/bo-exports.service';
import { BoModuleNavComponent } from '../shared/components/bo-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-bo-exportaciones',
  imports: [CommonModule, FormsModule, BoModuleNavComponent],
  template: `
    <main class="wrap">
      <div class="bg-orb orb-a"></div>
      <div class="bg-orb orb-b"></div>
      <app-bo-module-nav />

      <header class="hero">
        <h1>Exportaciones gerenciales</h1>
        <p>Centraliza reportes de ventas, stock, clientes, proveedores, RRHH y caja con salida en Excel o PDF.</p>
      </header>

      <section class="filters card">
        <label>Fecha diaria <input type="date" [(ngModel)]="date" /></label>
        <label>Desde <input type="date" [(ngModel)]="from" /></label>
        <label>Hasta <input type="date" [(ngModel)]="to" /></label>
        <label>Formato
          <select [(ngModel)]="format">
            <option value="xlsx">Excel (.xlsx)</option>
            <option value="pdf">PDF (.pdf)</option>
          </select>
        </label>
        <label>Cliente ID <input type="number" [(ngModel)]="customerId" /></label>
        <label>Caja ID <input type="number" [(ngModel)]="cashSessionId" /></label>
      </section>

      <section class="grid">
        <article class="card">
          <h3>Ventas</h3>
          <button class="btn" (click)="run('/sales/daily', { date, format })">Ventas diarias</button>
          <button class="btn" (click)="run('/sales/range', { from, to, format })">Ventas por rango</button>
          <button class="btn" (click)="run('/transfers/pending', { format })">Transferencias pendientes</button>
        </article>

        <article class="card">
          <h3>Clientes</h3>
          <button class="btn" (click)="run('/customers/account-summary', { format })">Resumen cuenta corriente</button>
          <button class="btn" [disabled]="!customerId" (click)="run('/customers/' + customerId + '/account-statement', { from, to, format })">Estado de cuenta cliente</button>
        </article>

        <article class="card">
          <h3>Envases</h3>
          <button class="btn" (click)="run('/containers/debtors', { format })">Deudores de envases</button>
          <button class="btn" (click)="run('/containers/movements', { from, to, format })">Movimientos de envases</button>
        </article>

        <article class="card">
          <h3>Stock</h3>
          <button class="btn" (click)="run('/stock/summary', { format })">Resumen de stock</button>
          <button class="btn" (click)="run('/stock/critical', { format })">Stock crítico</button>
          <button class="btn" (click)="run('/stock/movements', { from, to, format })">Movimientos de stock</button>
        </article>

        <article class="card">
          <h3>Proveedores</h3>
          <button class="btn" (click)="run('/suppliers/claims', { format })">Reclamos</button>
          <button class="btn" (click)="run('/suppliers/credits', { format })">Créditos</button>
        </article>

        <article class="card">
          <h3>RRHH</h3>
          <button class="btn" (click)="run('/rrhh/hours', { from, to, format })">Horas</button>
          <button class="btn" (click)="run('/rrhh/punches', { from, to, format })">Fichadas</button>
        </article>

        <article class="card">
          <h3>Caja</h3>
          <button class="btn" [disabled]="!cashSessionId" (click)="run('/cash-sessions/' + cashSessionId + '/summary', { format: 'pdf' })">Resumen cierre caja (PDF)</button>
          <button class="btn" (click)="run('/cash-movements', { from, to, format })">Movimientos de caja</button>
        </article>
      </section>

      <p *ngIf="message" class="ok">{{ message }}</p>
      <p *ngIf="error" class="error">{{ error }}</p>
    </main>
  `,
  styles: [
    `:host{display:block}`,
    `.wrap{position:relative;overflow-x:clip;min-height:100vh;padding:24px;display:flex;flex-direction:column;gap:14px;font-family:'Montserrat','Segoe UI',sans-serif;background:linear-gradient(160deg,#fdf7ef 0%,#fff8ee 46%,#f2faf7 100%)}`,
    `.bg-orb{position:absolute;border-radius:999px;filter:blur(4px);pointer-events:none;opacity:.5}`,
    `.orb-a{width:320px;height:320px;right:0;top:80px;transform:translateX(22%);background:radial-gradient(circle,#ffcf9d 0%,rgba(255,207,157,0) 68%)}`,
    `.orb-b{width:300px;height:300px;left:0;top:220px;transform:translateX(-35%);background:radial-gradient(circle,#9fe6c6 0%,rgba(159,230,198,0) 70%)}`,
    `.hero{position:relative;z-index:1;background:linear-gradient(120deg,#0e5a42,#247a58 54%,#3a9b75);color:#fff;border-radius:22px;padding:22px 24px;box-shadow:0 18px 40px rgba(22,66,46,.22)}`,
    `.hero h1{margin:0 0 8px;font-size:30px}`,
    `.hero p{margin:0;color:rgba(255,255,255,.9)}`,
    `.card{position:relative;z-index:1;border:1px solid rgba(16,94,67,.16);border-radius:18px;padding:14px;background:rgba(255,255,255,.94);box-shadow:0 14px 32px rgba(33,73,57,.12);display:flex;flex-direction:column;gap:8px}`,
    `.filters{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:10px}`,
    `.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:12px}`,
    `label{display:flex;flex-direction:column;gap:4px;color:#1f6048;font-weight:600}`,
    `input,select{border:1px solid #d5e6dd;border-radius:10px;padding:9px 10px;background:#fff;outline:none;min-height:42px}`,
    `input:focus,select:focus{border-color:#2f8e67;box-shadow:0 0 0 3px rgba(47,142,103,.12)}`,
    `.btn{border:1px solid #2c7f5d;background:#e8f5ef;color:#1f6f50;border-radius:10px;padding:9px 10px;margin:4px 0;width:100%;text-align:left;font-weight:700;cursor:pointer;min-height:42px;transition:transform .14s ease,box-shadow .2s ease,background .2s ease}`,
    `.btn:hover{background:#dff2e9}`,
    `.btn:hover:not([disabled]){transform:translateY(-1px);box-shadow:0 8px 16px rgba(31,111,80,.18)}`,
    `.btn:focus-visible{outline:none;box-shadow:0 0 0 3px rgba(47,142,103,.2)}`,
    `.btn[disabled]{opacity:.6;cursor:not-allowed}`,
    `.ok{color:#0a7a32;font-weight:700}`,
    `.error{color:#b3261e;font-weight:700}`,
    `@media (max-width: 900px){.wrap{padding:16px}.hero h1{font-size:24px}}`
  ]
})
export class BoExportacionesComponent {
  date = this.today();
  from = this.daysAgo(6);
  to = this.today();
  format: 'xlsx' | 'pdf' = 'xlsx';
  customerId: number | null = null;
  cashSessionId: number | null = null;

  message = '';
  error = '';

  constructor(private readonly exportsApi: BoExportsService) {}

  async run(path: string, params: Record<string, unknown>): Promise<void> {
    this.message = '';
    this.error = '';
    try {
      await this.exportsApi.download(path, params as Record<string, string | number | undefined | null>);
      this.message = 'Exportación generada';
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo generar exportación';
    }
  }

  private today(): string {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
  }

  private daysAgo(days: number): string {
    const d = new Date();
    d.setDate(d.getDate() - days);
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }
}
