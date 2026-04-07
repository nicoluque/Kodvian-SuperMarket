import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BoTotemShiftsService } from '../core/services/bo-totem-shifts.service';
import { BoModuleNavComponent } from '../shared/components/bo-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-bo-totem-transitions',
  imports: [CommonModule, FormsModule, BoModuleNavComponent],
  template: `
    <main class="wrap">
      <div class="bg-orb orb-a"></div>
      <div class="bg-orb orb-b"></div>
      <app-bo-module-nav />

      <section class="content">
        <header class="hero">
          <div>
            <h1>Ventas Totem en transición</h1>
            <p>Estas ventas no impactan cierre hasta asignarse al turno entrante.</p>
          </div>
          <span class="hero-pill">Control operativo por turno</span>
        </header>

        <section class="card flow-card">
          <div class="flow-head">
            <h2>Filtro de búsqueda</h2>
            <p>Buscá ventas pendientes por sucursal y reasigná al turno correspondiente.</p>
          </div>

          <div class="toolbar-grid">
            <label class="field">Sucursal (ID)
              <input type="number" [(ngModel)]="storeId" placeholder="Ej: 42" />
            </label>
            <button class="btn btn-secondary" (click)="load()" [disabled]="loading">
              {{ loading ? 'Buscando...' : 'Buscar ventas' }}
            </button>
          </div>
        </section>

        <section class="alert ok" *ngIf="message" aria-live="polite">{{ message }}</section>
        <section class="alert error" *ngIf="error" aria-live="assertive">{{ error }}</section>

        <section class="card" *ngIf="items.length > 0">
          <div class="section-title">
            <h3>Ventas en transición</h3>
            <span class="subtitle">{{ items.length }} pendiente(s)</span>
          </div>

          <div class="table-scroll">
            <table>
              <thead>
                <tr>
                  <th>Venta</th>
                  <th>Sucursal</th>
                  <th>Fecha</th>
                  <th>Total</th>
                  <th>Turno esperado</th>
                  <th>Medios de pago</th>
                  <th>Acción</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of items">
                  <td>#{{ item.id }}</td>
                  <td>{{ item.storeId }}</td>
                  <td>{{ item.createdAt | date:'short' }}</td>
                  <td>{{ item.total | number:'1.2-2' }}</td>
                  <td>{{ item.expectedShiftBucket || '-' }}</td>
                  <td>{{ item.paymentMethods?.join(', ') || '-' }}</td>
                  <td><button class="btn btn-secondary" (click)="openReassign(item)">Asignar</button></td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        <section class="card" *ngIf="!loading && items.length === 0 && !error">
          <h3>Sin ventas pendientes</h3>
          <p class="meta">No hay ventas en transición para los filtros actuales.</p>
        </section>

        <section class="card" *ngIf="loading && items.length === 0">
          <div class="loading-box">
            <span class="spinner" aria-hidden="true"></span>
            <p class="meta">Cargando ventas en transición...</p>
          </div>
        </section>
      </section>

      <div class="overlay" *ngIf="selected">
        <div class="modal">
          <h3>Asignar venta #{{ selected.id }}</h3>
          <label>Turno destino
            <select [(ngModel)]="reassign.shiftBucket">
              <option value="Morning">Mañana</option>
              <option value="Afternoon">Tarde</option>
              <option value="Night">Noche</option>
            </select>
          </label>
          <label>Motivo de reasignación
            <input type="text" [(ngModel)]="reassign.reason" placeholder="Describe el motivo" />
          </label>
          <div class="modal-actions">
            <button class="btn btn-secondary" (click)="selected = null">Cancelar</button>
            <button class="btn btn-primary" (click)="confirmReassign()" [disabled]="loading">Guardar asignación</button>
          </div>
        </div>
      </div>
    </main>
  `,
  styles: [
    `:host{display:block}`,
    `.wrap{position:relative;overflow-x:clip;min-height:100vh;padding:24px;display:flex;flex-direction:column;gap:14px;font-family:'Montserrat','Segoe UI',sans-serif;background:linear-gradient(160deg,#fdf7ef 0%,#fff8ee 46%,#f2faf7 100%)}`,
    `.content{position:relative;z-index:1;display:flex;flex-direction:column;gap:14px;max-width:1120px;width:100%;margin:0 auto}`,
    `.bg-orb{position:absolute;border-radius:999px;filter:blur(4px);pointer-events:none;opacity:.5}`,
    `.orb-a{width:320px;height:320px;right:0;top:80px;transform:translateX(22%);background:radial-gradient(circle,#ffcf9d 0%,rgba(255,207,157,0) 68%)}`,
    `.orb-b{width:300px;height:300px;left:0;top:240px;transform:translateX(-35%);background:radial-gradient(circle,#9fe6c6 0%,rgba(159,230,198,0) 70%)}`,
    `.hero{background:linear-gradient(120deg,#0e5a42,#247a58 54%,#3a9b75);color:#fff;border-radius:22px;padding:22px 24px;box-shadow:0 18px 40px rgba(22,66,46,.22);display:flex;justify-content:space-between;align-items:flex-start;gap:12px;flex-wrap:wrap}`,
    `.hero h1{margin:0 0 8px;font-size:clamp(24px,2.4vw,32px);line-height:1.1}`,
    `.hero p{margin:0;color:rgba(255,255,255,.95)}`,
    `.hero-pill{display:inline-flex;align-items:center;background:rgba(255,255,255,.22);border:1px solid rgba(255,255,255,.4);border-radius:999px;padding:6px 10px;font-size:12px;font-weight:700;color:#fff}`,
    `.card{border:1px solid rgba(16,94,67,.16);border-radius:18px;padding:14px;background:rgba(255,255,255,.94);box-shadow:0 14px 32px rgba(33,73,57,.12);backdrop-filter:blur(2px);display:flex;flex-direction:column;gap:12px}`,
    `.flow-card{gap:14px}`,
    `.flow-head h2{margin:0;font-size:22px;color:#184f3c}`,
    `.flow-head p{margin:6px 0 0;color:#365d4d}`,
    `.toolbar-grid{display:grid;gap:10px;grid-template-columns:2fr 1fr;align-items:end}`,
    `.field{display:flex;flex-direction:column;gap:4px;color:#1f6048;font-weight:700}`,
    `input,select{border:1px solid #d5e6dd;border-radius:10px;padding:9px 10px;background:#fff;outline:none;min-height:42px}`,
    `input:focus,select:focus{border-color:#2f8e67;box-shadow:0 0 0 3px rgba(47,142,103,.12)}`,
    `.btn{border:1px solid transparent;border-radius:10px;padding:10px 12px;font-weight:700;cursor:pointer;text-decoration:none;display:inline-flex;align-items:center;justify-content:center;min-height:42px;transition:transform .14s ease,box-shadow .2s ease,background .2s ease}`,
    `.btn-primary{border-color:#2c7f5d;background:linear-gradient(135deg,#2f8e67,#1f6f50);color:#fff}`,
    `.btn-secondary{border-color:#2b7f5c;background:#e7f4ed;color:#15543d}`,
    `.btn:hover:not([disabled]){transform:translateY(-1px);box-shadow:0 8px 16px rgba(31,111,80,.18)}`,
    `.btn:focus-visible{outline:none;box-shadow:0 0 0 3px rgba(47,142,103,.2)}`,
    `.btn[disabled]{opacity:.6;cursor:not-allowed}`,
    `.section-title{display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap}`,
    `.section-title h3{margin:0;color:#184f3c}`,
    `.subtitle{display:inline-flex;background:#edf8f2;color:#16543d;border:1px solid #bfd9cc;border-radius:999px;padding:4px 10px;font-size:12px;font-weight:700}`,
    `.table-scroll{overflow:auto;border:1px solid #e5f0ea;border-radius:12px}`,
    `table{width:100%;border-collapse:collapse;min-width:960px;background:#fff}`,
    `th,td{padding:8px;border-bottom:1px solid #eef4f1;text-align:left;vertical-align:middle}`,
    `th{color:#1e5f47;background:#f5fbf8;position:sticky;top:0}`,
    `.meta{margin:0;color:#2f5f4c}`,
    `.alert{border-radius:12px;padding:12px 14px;font-weight:700}`,
    `.alert.ok{background:#edf8f2;border:1px solid #bfd9cc;color:#16543d}`,
    `.alert.error{background:#feefef;border:1px solid #f3c6c6;color:#9f1f1f}`,
    `.loading-box{display:flex;align-items:center;gap:10px}`,
    `.spinner{width:18px;height:18px;border-radius:999px;border:2px solid #c9e2d7;border-top-color:#2f8e67;animation:spin .9s linear infinite}`,
    `@keyframes spin{to{transform:rotate(360deg)}}`,
    `.overlay{position:fixed;inset:0;background:rgba(10,20,16,.45);display:grid;place-items:center;padding:16px;z-index:10}`,
    `.modal{background:#fff;border:1px solid #dcebe3;border-radius:16px;padding:16px;display:grid;gap:10px;min-width:320px;max-width:520px;width:100%;box-shadow:0 18px 40px rgba(20,62,46,.2)}`,
    `.modal h3{margin:0;color:#1e5f47}`,
    `.modal label{display:flex;flex-direction:column;gap:4px;color:#1f6048;font-weight:700}`,
    `.modal-actions{display:flex;gap:8px;justify-content:flex-end;flex-wrap:wrap}`,
    `@media (max-width: 900px){.wrap{padding:16px}.toolbar-grid{grid-template-columns:1fr}.btn{width:100%}.modal-actions .btn{width:100%}}`
  ]
})
export class BoTotemTransitionsComponent {
  loading = false;
  message = '';
  error = '';
  storeId: number | null = null;
  items: any[] = [];
  selected: any | null = null;
  reassign = { shiftBucket: 'Afternoon', reason: '' };

  constructor(private readonly api: BoTotemShiftsService) {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading = true;
    this.error = '';
    this.message = '';
    try {
      this.items = await this.api.listTransitions({ storeId: this.storeId ?? undefined });
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo cargar transiciones';
    } finally {
      this.loading = false;
    }
  }

  openReassign(item: any): void {
    this.selected = item;
    this.reassign = { shiftBucket: item.expectedShiftBucket || 'Afternoon', reason: '' };
  }

  async confirmReassign(): Promise<void> {
    if (!this.selected) return;
    if (!this.reassign.reason.trim()) {
      this.error = 'Debes informar motivo';
      return;
    }

    this.loading = true;
    this.error = '';
    this.message = '';
    try {
      await this.api.reassignSale(this.selected.id, this.reassign);
      this.message = `Venta #${this.selected.id} asignada`;
      this.selected = null;
      await this.load();
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo reasignar';
    } finally {
      this.loading = false;
    }
  }
}
