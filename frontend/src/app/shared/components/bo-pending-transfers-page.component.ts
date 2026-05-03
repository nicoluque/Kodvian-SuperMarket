import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

type PendingTransferRow = {
  saleId: number;
  total: number;
  createdAt: string;
  deviceId?: number;
};

@Component({
  standalone: true,
  selector: 'app-bo-pending-transfers-page',
  imports: [CommonModule, FormsModule],
  template: `
    <section class="bo-card">
      <div class="section-head">
        <h3>Pendientes de transferencia</h3>
        <p>Seguimiento de ventas con transferencia aún no confirmada.</p>
      </div>

      <div class="summary">
        <article>
          <span>Total pendientes</span>
          <strong>{{ filteredRows().length }}</strong>
        </article>
        <article>
          <span>Monto acumulado</span>
          <strong>{{ totalAmount(filteredRows()) | number:'1.2-2' }}</strong>
        </article>
      </div>

      <div class="toolbar">
        <div class="field-block toolbar-field">
          <label class="field-label" for="pending-transfer-filter">Buscar venta</label>
          <input id="pending-transfer-filter" class="field" [(ngModel)]="filter" (ngModelChange)="page = 1" placeholder="ID de venta o dispositivo" />
        </div>
        <button class="btn btn-secondary toolbar-action" (click)="load()" [disabled]="loading">{{ loading ? 'Actualizando...' : 'Actualizar pendientes' }}</button>
      </div>

      <p *ngIf="error" class="error">{{ error }}</p>

      <div class="table-wrap" *ngIf="!error">
        <table class="table">
          <thead>
            <tr>
              <th>Venta</th>
              <th>Fecha</th>
              <th>Total</th>
              <th>Dispositivo</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let r of pagedRows()">
              <td>#{{ r.saleId }}</td>
              <td>{{ r.createdAt | date:'short' }}</td>
              <td>{{ r.total | number:'1.2-2' }}</td>
              <td>{{ r.deviceId ?? '-' }}</td>
              <td class="row-actions">
                <a class="btn btn-secondary" [href]="'/print/sale/' + r.saleId" target="_blank">Ver</a>
                <a class="btn btn-secondary" [href]="'/print/sale/' + r.saleId + '?autoprint=1'" target="_blank">Imprimir</a>
                <a class="btn btn-secondary" [href]="'/print/sale/' + r.saleId + '?autoprint=1&reprint=1'" target="_blank">Reimprimir</a>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div *ngIf="!loading && !error && filteredRows().length > 0" class="pager">
        <span>Mostrando {{ pageRangeLabel(filteredRows().length, page) }}</span>
        <div class="pager-actions">
          <button class="btn btn-secondary" (click)="prevPage()" [disabled]="page <= 1">Anterior</button>
          <span>Página {{ page }}/{{ totalPages(filteredRows().length) }}</span>
          <button class="btn btn-secondary" (click)="nextPage(filteredRows().length)" [disabled]="page >= totalPages(filteredRows().length)">Siguiente</button>
        </div>
      </div>
      <p *ngIf="!loading && filteredRows().length === 0" class="empty">No hay pendientes de transferencia para el filtro aplicado.</p>
    </section>
  `,
  styles: [
    `.bo-card{background:#fff;border:1px solid #d8e8df;border-radius:14px;padding:14px;display:flex;flex-direction:column;gap:12px;box-shadow:0 4px 18px rgba(5,46,42,.08)}`,
    `.section-head h3{margin:0;color:#0f3a40;font-size:1.1rem}`,
    `.section-head p{margin:2px 0 0;color:#5f7a73;font-size:.85rem}`,
    `.summary{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:8px}`,
    `.summary article{border:1px solid #dcebe5;border-radius:10px;padding:10px;background:#fbfefd;display:flex;flex-direction:column;gap:4px}`,
    `.summary span{font-size:12px;color:#5f7a73;text-transform:uppercase;letter-spacing:.04em}`,
    `.summary strong{font-size:22px;color:#0f3a40}`,
    `.toolbar{display:flex;gap:8px;flex-wrap:wrap;align-items:center}`,
    `.toolbar-field{min-width:260px}`,
    `.toolbar-action{align-self:flex-end}`,
    `.field-block{display:flex;flex-direction:column;gap:4px}`,
    `.field-label{font-size:12px;font-weight:700;color:#355b4f}`,
    `.field{border:1px solid #c6ddd4;background:#f7fcfa;color:#0f3a40;border-radius:8px;min-height:36px;padding:7px 10px;min-width:200px}`,
    `.field:focus{outline:none;border-color:#0fa47f;box-shadow:0 0 0 3px rgba(15,164,127,.15)}`,
    `.btn{border-radius:8px;min-height:36px;padding:0 12px;border:1px solid transparent;cursor:pointer;font-weight:600;text-decoration:none;display:inline-flex;align-items:center}`,
    `.btn:disabled{opacity:.6;cursor:not-allowed}`,
    `.btn-secondary{background:#eff8f4;border-color:#c6ddd4;color:#0f3a40}`,
    `.btn-secondary:hover:not(:disabled){background:#e2f2ea}`,
    `.error{color:#b3261e;margin:0}`,
    `.table-wrap{overflow:auto;border:1px solid #e5efeb;border-radius:10px}`,
    `.table{width:100%;border-collapse:collapse;font-size:13px;background:#fff}`,
    `.table thead th{text-align:left;padding:10px 9px;border-bottom:1px solid #0d8a6a;background:#0fa47f;color:#fff;font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:.04em}`,
    `.table td{padding:9px;border-bottom:1px solid #edf4f1}`,
    `.row-actions{display:flex;gap:6px;flex-wrap:wrap}`,
    `.pager{display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;color:#555;font-size:12px}`,
    `.pager-actions{display:flex;gap:6px;align-items:center}`,
    `.empty{margin:0;color:#555}`,
    `@media (max-width: 760px){.summary{grid-template-columns:1fr}.field{min-width:0}.row-actions{flex-direction:column;align-items:flex-start}}`
  ]
})
export class BoPendingTransfersPageComponent {
  private readonly http = inject(HttpClient);
  private readonly pageSize = 12;

  rows: PendingTransferRow[] = [];
  filter = '';
  page = 1;
  loading = false;
  error = '';

  constructor() {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading = true;
    this.error = '';
    try {
      const operations: any = await firstValueFrom(this.http.get('/api/v1/dashboard/operations-summary'));
      const pending = Array.isArray(operations?.pendingTransfers) ? operations.pendingTransfers : [];
      this.rows = pending.map((x: any) => ({
        saleId: Number(x?.id ?? x?.saleId ?? 0),
        total: Number(x?.total ?? 0),
        createdAt: `${x?.createdAt ?? ''}`,
        deviceId: Number(x?.deviceId ?? 0) || undefined
      })).filter((x: PendingTransferRow) => x.saleId > 0);
      this.page = 1;
    } catch (err: any) {
      this.error = this.formatError(err, 'No se pudieron cargar las transferencias pendientes.');
    } finally {
      this.loading = false;
    }
  }

  filteredRows(): PendingTransferRow[] {
    const q = this.filter.trim().toLowerCase();
    if (!q) return this.rows;
    return this.rows.filter(r => `${r.saleId} ${r.deviceId ?? ''}`.toLowerCase().includes(q));
  }

  pagedRows(): PendingTransferRow[] {
    const start = (Math.max(1, this.page) - 1) * this.pageSize;
    return this.filteredRows().slice(start, start + this.pageSize);
  }

  totalAmount(rows: PendingTransferRow[]): number {
    return rows.reduce((sum, row) => sum + Number(row.total || 0), 0);
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

  prevPage(): void {
    this.page = Math.max(1, this.page - 1);
  }

  nextPage(totalRows: number): void {
    this.page = Math.min(this.totalPages(totalRows), this.page + 1);
  }

  private formatError(err: any, fallback: string): string {
    if (err?.status === 0) return 'No hay conexión con el servidor. Verificá red local y API.';
    if (err?.status === 403) return 'No tenés permisos para esta acción.';
    const message = typeof err?.error?.message === 'string' ? err.error.message.trim() : '';
    return message || fallback;
  }
}
