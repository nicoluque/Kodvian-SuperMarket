import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BoPurchaseSuggestionsService } from '../core/services/bo-purchase-suggestions.service';
import { BoModuleNavComponent } from '../shared/components/bo-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-bo-compras-sugeridas',
  imports: [CommonModule, FormsModule, BoModuleNavComponent],
  template: `
    <main class="wrap">
      <div class="bg-orb orb-a"></div>
      <div class="bg-orb orb-b"></div>
      <app-bo-module-nav />

      <header class="hero">
        <h1>Compras sugeridas</h1>
        <p>Genera propuestas de reposicion por demanda, cobertura y criticidad para convertirlas en borradores de compra.</p>
      </header>

      <section class="card filters">
        <label>Proveedor
          <select [(ngModel)]="supplierId">
            <option [ngValue]="null">Todos</option>
            <option *ngFor="let s of suppliers" [ngValue]="s.id">{{ s.name }}</option>
          </select>
        </label>

        <label>Dias ventana
          <input type="number" min="1" max="180" [(ngModel)]="daysWindow" />
        </label>

        <label>Dias cobertura objetivo
          <input type="number" min="1" max="90" [(ngModel)]="targetCoverageDays" />
        </label>

        <label class="check">
          <input type="checkbox" [(ngModel)]="criticalOnly" />
          Solo críticos
        </label>

        <button class="btn btn-primary" [disabled]="busy" (click)="generate()">Generar sugerencias</button>
      </section>

      <section class="card" *ngIf="suggestion">
        <div class="row">
          <h3>Sugerencia #{{ suggestion.id }}</h3>
          <div class="actions">
            <button class="btn btn-primary" [disabled]="busy" (click)="convert()">Convertir a compras draft</button>
            <button class="btn btn-secondary" [disabled]="busy" (click)="reloadSuggestion()">Recargar detalle</button>
          </div>
        </div>

        <div class="table-scroll" *ngIf="filteredLines.length > 0">
        <table>
          <tr>
            <th>Producto</th>
            <th>Stock</th>
            <th>Min</th>
            <th>Promedio diario</th>
            <th>Sugerido</th>
            <th>Proveedor</th>
            <th>Motivo</th>
            <th>Estado</th>
            <th>Acciones</th>
          </tr>
          <tr *ngFor="let l of filteredLines">
            <td>{{ l.productName }}</td>
            <td>{{ l.currentStock }}</td>
            <td>{{ l.minStock }}</td>
            <td>{{ l.avgDailySales }}</td>
            <td>
              <input type="number" min="0" step="0.001" [ngModel]="l.acceptedQty" (ngModelChange)="onQtyChange(l, $event)" style="width:90px" />
            </td>
            <td>{{ l.supplierName || '-' }}</td>
            <td>{{ l.reason }}</td>
            <td>{{ l.status }}</td>
            <td class="actions">
              <button class="btn btn-success" [disabled]="busy || l.status==='Converted'" (click)="setStatus(l, 'Accepted')">Aceptar</button>
              <button class="btn btn-secondary" [disabled]="busy || l.status==='Converted'" (click)="setStatus(l, 'Ignored')">Ignorar</button>
            </td>
          </tr>
        </table>
        </div>
        <p *ngIf="filteredLines.length === 0">No hay líneas para los filtros actuales.</p>
      </section>

      <section class="card">
        <h3>Historial de sugerencias</h3>
        <div class="table-scroll" *ngIf="history.length > 0">
        <table>
          <tr><th>ID</th><th>Fecha</th><th>Estado</th><th>Lineas</th><th>Aceptadas</th><th></th></tr>
          <tr *ngFor="let h of history">
            <td>#{{ h.id }}</td>
            <td>{{ h.generatedAt }}</td>
            <td>{{ h.status }}</td>
            <td>{{ h.totalLines }}</td>
            <td>{{ h.acceptedLines }}</td>
            <td><button class="btn btn-secondary" [disabled]="busy" (click)="openSuggestion(h.id)">Abrir</button></td>
          </tr>
        </table>
        </div>
        <p *ngIf="history.length === 0">Sin sugerencias generadas.</p>
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
    `.orb-b{width:300px;height:300px;left:0;top:240px;transform:translateX(-35%);background:radial-gradient(circle,#9fe6c6 0%,rgba(159,230,198,0) 70%)}`,
    `.hero{position:relative;z-index:1;background:linear-gradient(120deg,#0e5a42,#247a58 54%,#3a9b75);color:#fff;border-radius:22px;padding:22px 24px;box-shadow:0 18px 40px rgba(22,66,46,.22)}`,
    `.hero h1{margin:0 0 8px;font-size:30px}`,
    `.hero p{margin:0;color:rgba(255,255,255,.9)}`,
    `.card{position:relative;z-index:1;border:1px solid rgba(16,94,67,.16);border-radius:18px;padding:14px;background:rgba(255,255,255,.94);box-shadow:0 14px 32px rgba(33,73,57,.12)}`,
    `.filters{display:grid;grid-template-columns:repeat(auto-fit,minmax(170px,1fr));gap:10px;align-items:end}`,
    `label{display:flex;flex-direction:column;gap:4px;color:#1f6048;font-weight:600}`,
    `input,select{border:1px solid #d5e6dd;border-radius:10px;padding:9px 10px;background:#fff;outline:none;min-height:42px}`,
    `input:focus,select:focus{border-color:#2f8e67;box-shadow:0 0 0 3px rgba(47,142,103,.12)}`,
    `.check{flex-direction:row;align-items:center;gap:6px}`,
    `.row{display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap}`,
    `.actions{display:flex;gap:6px;flex-wrap:wrap}`,
    `.table-scroll{overflow:auto;max-width:100%}`,
    `table{width:100%;border-collapse:collapse;min-width:980px;background:#fff}`,
    `th,td{padding:8px;border-bottom:1px solid #edf3ef;text-align:left;vertical-align:middle}`,
    `th{color:#1e5f47;background:#f5fbf8}`,
    `.btn{border:1px solid transparent;border-radius:10px;padding:8px 12px;font-weight:700;cursor:pointer;min-height:42px;transition:transform .14s ease,box-shadow .2s ease,background .2s ease}`,
    `.btn-primary{border-color:#2c7f5d;background:linear-gradient(135deg,#2f8e67,#1f6f50);color:#fff}`,
    `.btn-secondary{border-color:#2f8e67;background:#e8f5ef;color:#1f6f50}`,
    `.btn-success{border-color:#2f8e67;background:#f0faf5;color:#1f6f50}`,
    `.btn:hover:not([disabled]){transform:translateY(-1px);box-shadow:0 8px 16px rgba(31,111,80,.2)}`,
    `.btn:focus-visible{outline:none;box-shadow:0 0 0 3px rgba(47,142,103,.2)}`,
    `.btn[disabled]{opacity:.6;cursor:not-allowed}`,
    `.ok{color:#0a7a32;font-weight:700}`,
    `.error{color:#b3261e;font-weight:700}`,
    `@media (max-width: 900px){.wrap{padding:16px}.hero h1{font-size:24px}}`
  ]
})
export class BoComprasSugeridasComponent {
  suppliers: any[] = [];
  supplierId: number | null = null;
  criticalOnly = false;
  daysWindow = 14;
  targetCoverageDays = 7;

  history: any[] = [];
  suggestion: any = null;

  busy = false;
  message = '';
  error = '';

  get filteredLines(): any[] {
    if (!this.suggestion?.lines) return [];
    return this.suggestion.lines.filter((l: any) => {
      if (this.supplierId && l.supplierId !== this.supplierId) return false;
      if (this.criticalOnly && !l.isCritical) return false;
      return true;
    });
  }

  constructor(private readonly api: BoPurchaseSuggestionsService) {
    void this.loadBoot();
  }

  async loadBoot(): Promise<void> {
    await this.run(async () => {
      this.suppliers = await this.api.suppliers();
      this.history = await this.api.list();
      if (this.history.length > 0) {
        this.suggestion = await this.api.getById(this.history[0].id);
      }
    });
  }

  async generate(): Promise<void> {
    await this.run(async () => {
      this.suggestion = await this.api.generate({
        supplierId: this.supplierId ?? undefined,
        criticalOnly: this.criticalOnly,
        daysWindow: this.daysWindow,
        targetCoverageDays: this.targetCoverageDays
      });
      this.history = await this.api.list();
      this.message = `Sugerencia #${this.suggestion.id} generada`;
    });
  }

  async openSuggestion(id: number): Promise<void> {
    await this.run(async () => {
      this.suggestion = await this.api.getById(id);
    });
  }

  async reloadSuggestion(): Promise<void> {
    if (!this.suggestion?.id) return;
    await this.openSuggestion(this.suggestion.id);
  }

  async setStatus(line: any, status: 'Accepted' | 'Ignored'): Promise<void> {
    if (!this.suggestion?.id) return;
    await this.run(async () => {
      await this.api.updateLine(this.suggestion.id, line.id, { status });
      await this.reloadSuggestion();
    });
  }

  async onQtyChange(line: any, qty: number): Promise<void> {
    if (!this.suggestion?.id) return;
    const numeric = Number(qty);
    if (Number.isNaN(numeric)) return;
    await this.run(async () => {
      await this.api.updateLine(this.suggestion.id, line.id, { acceptedQty: numeric });
      line.acceptedQty = numeric;
    });
  }

  async convert(): Promise<void> {
    if (!this.suggestion?.id) return;
    await this.run(async () => {
      const result = await this.api.convertToPurchase(this.suggestion.id);
      this.message = `Compras draft creadas: ${result.purchasesCreated}`;
      this.history = await this.api.list();
      await this.reloadSuggestion();
    });
  }

  private async run(fn: () => Promise<void>): Promise<void> {
    this.busy = true;
    this.error = '';
    this.message = '';
    try {
      await fn();
    } catch (err: any) {
      this.error = err?.error?.message ?? 'Operación fallida';
    } finally {
      this.busy = false;
    }
  }
}
