import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BoImportService } from '../core/services/bo-import.service';
import { BoModuleNavComponent } from '../shared/components/bo-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-bo-importacion-unificada',
  imports: [CommonModule, FormsModule, RouterLink, BoModuleNavComponent],
  template: `
    <main class="wrap">
      <div class="bg-orb orb-a"></div>
      <div class="bg-orb orb-b"></div>
      <app-bo-module-nav />

      <section class="content">
        <header class="hero">
          <div class="hero-copy">
            <h1>Importación unificada: catálogo + stock</h1>
            <p>En una sola operación crea o actualiza artículos y aplica cantidades iniciales de stock.</p>
          </div>
          <div class="hero-side">
            <span class="hero-pill">Modo estricto: si hay errores no se aplica nada</span>
            <a class="hero-link" routerLink="/bo/importaciones/ajuste-masivo-stock">Ir a ajuste masivo</a>
          </div>
        </header>

        <section class="card flow-card">
          <div class="flow-head">
            <h2>Flujo principal</h2>
            <p>Descargá la plantilla, cargá el archivo, revisá la vista previa y confirmá.</p>
          </div>

          <div class="toolbar-grid">
            <button class="btn btn-secondary" (click)="downloadTemplate()" [disabled]="loadingTemplate">
              {{ loadingTemplate ? 'Descargando...' : '1) Descargar plantilla unificada' }}
            </button>

            <label class="file-drop">
              <span class="file-title">2) Subir archivo</span>
              <span class="file-sub">{{ file ? file.name : 'Seleccionar archivo .xlsx' }}</span>
              <input class="file-input" type="file" accept=".xlsx" (change)="onFile($event)" />
            </label>

            <button class="btn btn-secondary" (click)="previewFile()" [disabled]="!file || loading">3) Vista previa</button>
            <button class="btn btn-primary" (click)="commitFile()" [disabled]="!file || !preview || loading">4) Confirmar</button>
          </div>

          <label class="upsert-row" title="Si el registro existe, se actualiza; si no existe, se crea.">
            <span class="switch">
              <input type="checkbox" [(ngModel)]="upsert" />
              <span class="switch-slider" aria-hidden="true"></span>
            </span>
            <span class="upsert-copy">
              <strong>Actualizar existentes</strong>
              <small>Si el registro existe, se actualiza; si no existe, se crea.</small>
            </span>
          </label>

          <div class="flow-footer">
            <p class="meta" *ngIf="file; else noFileMeta">Archivo: <strong>{{ file.name }}</strong></p>
            <ng-template #noFileMeta>
              <p class="meta">Subí un archivo <strong>.xlsx</strong> para comenzar la vista previa.</p>
            </ng-template>
          </div>
        </section>

        <section class="alert error" *ngIf="error" aria-live="assertive">{{ error }}</section>

        <section class="card" *ngIf="loading" aria-live="polite">
          <div class="loading-box">
            <span class="spinner" aria-hidden="true"></span>
            <p class="meta">Procesando archivo, aguardá un momento...</p>
          </div>
        </section>

        <section class="card" *ngIf="preview">
          <div class="section-title">
            <h3>Resultado de vista previa</h3>
            <span class="subtitle">Catálogo + stock</span>
          </div>

          <div class="kpis">
            <div class="kpi"><span>Total</span><strong>{{ preview.totalRows }}</strong></div>
            <div class="kpi"><span>Válidas</span><strong>{{ preview.validRows }}</strong></div>
            <div class="kpi"><span>Inválidas</span><strong>{{ preview.invalidRows }}</strong></div>
            <div class="kpi"><span>Modo</span><strong>Estricto</strong></div>
          </div>

          <div class="preview-tools">
            <label class="mini-check"><input type="checkbox" [(ngModel)]="showOnlyErrors" /> Solo con error</label>
            <span class="meta">Mostrando {{ filteredRows.length }} fila(s)</span>
          </div>

          <div class="table-scroll">
            <table>
              <thead>
                <tr>
                  <th>Fila</th>
                  <th>Estado</th>
                  <th>Acción catálogo</th>
                  <th>Nombre</th>
                  <th>Identificador</th>
                  <th>Impacto en stock</th>
                  <th>Detalle</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let r of filteredRows" [class.invalid-row]="!r.valid">
                  <td>{{ r.rowNumber }}</td>
                  <td><span class="status" [class.ok]="r.valid" [class.fail]="!r.valid">{{ r.valid ? 'Válida' : 'Con error' }}</span></td>
                  <td>{{ r.data?.catalogAction || '-' }}</td>
                  <td>{{ r.data?.name || '-' }}</td>
                  <td>{{ r.data?.barcode || r.data?.quickCode || '-' }}</td>
                  <td>{{ r.data?.totalQtyImpact || '0' }}</td>
                  <td>
                    <span class="ok-text" *ngIf="!r.errors?.length">Sin observaciones</span>
                    <span class="error-line" *ngFor="let e of r.errors">{{ fieldLabel(e.field) }}: {{ e.message }}</span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        <section class="card" *ngIf="commit">
          <div class="section-title">
            <h3>Resultado de confirmación</h3>
            <span class="subtitle success">Aplicado</span>
          </div>
          <div class="kpis">
            <div class="kpi"><span>Creados</span><strong>{{ commit.created }}</strong></div>
            <div class="kpi"><span>Actualizados</span><strong>{{ commit.updated }}</strong></div>
            <div class="kpi"><span>Movimientos de stock</span><strong>{{ commit.stockMovementsApplied }}</strong></div>
          </div>
        </section>

        <section class="card" *ngIf="!preview && !commit && !loading && !error">
          <h3>Sin importación ejecutada</h3>
          <ol class="steps">
            <li>Descargá la plantilla unificada.</li>
            <li>Completá datos de catálogo y cantidades.</li>
            <li>Subí el archivo y validá vista previa.</li>
            <li>Confirmá para crear/actualizar e impactar stock en un solo proceso.</li>
          </ol>
        </section>
      </section>
    </main>
  `,
  styles: [
    `:host{display:block}`,
    `.wrap{position:relative;overflow-x:clip;min-height:100vh;padding:24px;display:flex;flex-direction:column;gap:14px;font-family:'Montserrat','Segoe UI',sans-serif;background:linear-gradient(160deg,#fdf7ef 0%,#fff8ee 46%,#f2faf7 100%)}`,
    `.content{position:relative;z-index:1;display:flex;flex-direction:column;gap:14px;max-width:1120px;width:100%;margin:0 auto}`,
    `.bg-orb{position:absolute;border-radius:999px;filter:blur(4px);pointer-events:none;opacity:.5}`,
    `.orb-a{width:320px;height:320px;right:0;top:80px;transform:translateX(22%);background:radial-gradient(circle,#ffcf9d 0%,rgba(255,207,157,0) 68%)}`,
    `.orb-b{width:300px;height:300px;left:0;top:240px;transform:translateX(-35%);background:radial-gradient(circle,#9fe6c6 0%,rgba(159,230,198,0) 70%)}`,
    `.hero{background:linear-gradient(120deg,#0e5a42,#247a58 54%,#3a9b75);color:#fff;border-radius:22px;padding:22px 24px;display:flex;justify-content:space-between;gap:12px;flex-wrap:wrap;box-shadow:0 18px 40px rgba(22,66,46,.22)}`,
    `.hero-copy{max-width:760px}`,
    `.hero h1{margin:0 0 8px;font-size:clamp(24px,2.4vw,36px);line-height:1.1}`,
    `.hero p{margin:0;color:rgba(255,255,255,.95)}`,
    `.hero-side{display:flex;flex-direction:column;gap:8px;align-items:flex-end}`,
    `.hero-pill{display:inline-flex;align-items:center;background:rgba(255,255,255,.22);border:1px solid rgba(255,255,255,.4);border-radius:999px;padding:6px 10px;font-size:12px;font-weight:700}`,
    `.hero-link{text-decoration:none;background:#ecf9f3;color:#16543d;border:1px solid #b9ddcb;border-radius:999px;padding:7px 11px;font-size:12px;font-weight:700}`,
    `.hero-link:hover{background:#dff2e9}`,
    `.flow-card{gap:14px}`,
    `.flow-head h2{margin:0;font-size:24px;color:#184f3c}`,
    `.flow-head p{margin:6px 0 0;color:#365d4d}`,
    `.card{border:1px solid rgba(16,94,67,.16);border-radius:18px;padding:16px;background:rgba(255,255,255,.94);box-shadow:0 14px 32px rgba(33,73,57,.12);display:flex;flex-direction:column;gap:12px}`,
    `.toolbar-grid{display:grid;gap:10px;grid-template-columns:2fr 2fr 1fr 1fr;align-items:stretch}`,
    `.btn{border:1px solid transparent;border-radius:10px;padding:10px 12px;font-weight:700;cursor:pointer;min-height:44px;display:inline-flex;align-items:center;justify-content:center;transition:transform .14s ease,box-shadow .2s ease,background .2s ease}`,
    `.btn-primary{border-color:#2c7f5d;background:linear-gradient(135deg,#2f8e67,#1f6f50);color:#fff}`,
    `.btn-secondary{border-color:#2b7f5c;background:#e7f4ed;color:#15543d}`,
    `.btn:hover:not([disabled]){transform:translateY(-1px);box-shadow:0 8px 16px rgba(31,111,80,.18)}`,
    `.btn:focus-visible{outline:none;box-shadow:0 0 0 3px rgba(47,142,103,.2)}`,
    `.btn[disabled]{opacity:.6;cursor:not-allowed}`,
    `.file-drop{position:relative;display:flex;flex-direction:column;justify-content:center;border:1px dashed #7ebda0;border-radius:12px;background:#f4fbf8;color:#23644b;padding:8px 12px;cursor:pointer;min-height:44px;gap:2px}`,
    `.file-title{font-weight:700;font-size:13px}`,
    `.file-sub{font-size:12px;color:#2e5f4c;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}`,
    `.file-input{position:absolute;inset:0;opacity:0;cursor:pointer}`,
    `.upsert-row{display:flex;align-items:center;gap:10px;padding:10px 12px;border:1px solid #d8e8df;border-radius:12px;background:#f7fcf9;max-width:520px}`,
    `.upsert-copy{display:flex;flex-direction:column;gap:2px;color:#1f5b44}`,
    `.upsert-copy strong{font-size:14px}`,
    `.upsert-copy small{font-size:12px;color:#3e6959;line-height:1.3}`,
    `.switch{position:relative;display:inline-flex;width:42px;height:24px;flex:0 0 42px}`,
    `.switch input{opacity:0;width:0;height:0}`,
    `.switch-slider{position:absolute;inset:0;border-radius:999px;background:#c4d9cd;transition:background .2s ease}`,
    `.switch-slider::before{content:'';position:absolute;width:18px;height:18px;left:3px;top:3px;border-radius:999px;background:#fff;box-shadow:0 1px 2px rgba(0,0,0,.2);transition:transform .2s ease}`,
    `.switch input:checked + .switch-slider{background:#2f8e67}`,
    `.switch input:checked + .switch-slider::before{transform:translateX(18px)}`,
    `.switch input:focus-visible + .switch-slider{box-shadow:0 0 0 3px rgba(47,142,103,.2)}`,
    `.flow-footer{display:flex;gap:8px;flex-wrap:wrap;align-items:center}`,
    `.meta{margin:0;color:#2f5f4c}`,
    `.alert.error{background:#feefef;border:1px solid #f3c6c6;color:#9f1f1f;border-radius:12px;padding:12px 14px;font-weight:700}`,
    `.loading-box{display:flex;align-items:center;gap:10px}`,
    `.spinner{width:18px;height:18px;border-radius:999px;border:2px solid #c9e2d7;border-top-color:#2f8e67;animation:spin .9s linear infinite}`,
    `@keyframes spin{to{transform:rotate(360deg)}}`,
    `.section-title{display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap}`,
    `.section-title h3{margin:0;color:#184f3c}`,
    `.subtitle{display:inline-flex;background:#edf8f2;color:#16543d;border:1px solid #bfd9cc;border-radius:999px;padding:4px 10px;font-size:12px;font-weight:700}`,
    `.subtitle.success{background:#e4f7ea;border-color:#9fd4b8;color:#0f6a3d}`,
    `.kpis{display:grid;grid-template-columns:repeat(auto-fit,minmax(140px,1fr));gap:10px}`,
    `.kpi{border:1px solid #dcebe3;background:#f8fcfa;border-radius:12px;padding:10px;display:flex;flex-direction:column;gap:4px}`,
    `.kpi span{font-size:12px;color:#2f5e4b}`,
    `.kpi strong{font-size:22px;color:#0f4f3a;font-variant-numeric:tabular-nums}`,
    `.preview-tools{display:flex;justify-content:space-between;align-items:center;gap:10px;padding:4px 2px}`,
    `.mini-check{display:inline-flex;gap:6px;align-items:center;color:#235a44;font-size:13px;font-weight:600}`,
    `.table-scroll{overflow:auto;border:1px solid #e5f0ea;border-radius:12px}`,
    `table{width:100%;border-collapse:collapse;min-width:960px;background:#fff;border-radius:12px;overflow:hidden}`,
    `th,td{border-bottom:1px solid #edf3ef;padding:8px;text-align:left;vertical-align:top}`,
    `th{background:#f5fbf8;color:#1e5f47;position:sticky;top:0}`,
    `tbody tr:hover{background:#f9fcfa}`,
    `.status{display:inline-flex;padding:2px 8px;border-radius:999px;font-size:12px;font-weight:700}`,
    `.status.ok{background:#e4f7ea;color:#0f6a3d}`,
    `.status.fail{background:#fde9e9;color:#8f1b1b}`,
    `.invalid-row{background:#fff9f9}`,
    `.ok-text{font-size:13px;color:#2f5f4c}`,
    `.error-line{display:block;color:#7d2b2b}`,
    `.steps{margin:0;padding-left:20px;color:#2f5f4c;display:flex;flex-direction:column;gap:6px}`,
    `@media (max-width: 1080px){.toolbar-grid{grid-template-columns:repeat(2,minmax(0,1fr))}}`,
    `@media (max-width: 900px){.wrap{padding:16px}.hero-side{align-items:flex-start;width:100%}.toolbar-grid{grid-template-columns:1fr}.btn,.file-drop{width:100%}.upsert-row{max-width:none}.preview-tools{flex-direction:column;align-items:flex-start}}`
  ]
})
export class BoImportacionUnificadaComponent {
  file: File | null = null;
  preview: any = null;
  commit: any = null;
  upsert = true;
  loading = false;
  loadingTemplate = false;
  showOnlyErrors = false;
  error = '';

  constructor(private readonly imports: BoImportService) {}

  get orderedRows(): any[] {
    const rows = Array.isArray(this.preview?.rows) ? this.preview.rows : [];
    return [...rows].sort((a, b) => {
      if (a.valid === b.valid) return (a.rowNumber ?? 0) - (b.rowNumber ?? 0);
      return a.valid ? 1 : -1;
    });
  }

  get filteredRows(): any[] {
    if (!this.showOnlyErrors) return this.orderedRows;
    return this.orderedRows.filter(r => !r.valid);
  }

  fieldLabel(field: string): string {
    const map: Record<string, string> = {
      saleType: 'Tipo de venta',
      name: 'Nombre',
      price: 'Precio',
      pricePerKg: 'Precio por kg',
      vendibleQty: 'Cantidad vendible',
      reclamoQty: 'Cantidad reclamo',
      mermaQty: 'Cantidad merma',
      'barcode/quickCode': 'Código de barras o código rápido'
    };
    return map[field] ?? field;
  }

  async downloadTemplate(): Promise<void> {
    this.loadingTemplate = true;
    this.error = '';
    try {
      await this.imports.downloadCatalogStockTemplate();
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo descargar plantilla';
    } finally {
      this.loadingTemplate = false;
    }
  }

  onFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    const selected = input.files?.[0] ?? null;
    if (selected && !selected.name.toLowerCase().endsWith('.xlsx')) {
      this.error = 'Formato inválido. Debes subir un archivo .xlsx';
      this.file = null;
      input.value = '';
      return;
    }
    this.file = selected;
    this.preview = null;
    this.commit = null;
    this.error = '';
    this.showOnlyErrors = false;
  }

  async previewFile(): Promise<void> {
    if (!this.file) return;
    this.loading = true;
    this.error = '';
    this.commit = null;
    try {
      this.preview = await this.imports.catalogStockPreview(this.file, this.upsert);
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo generar vista previa';
    } finally {
      this.loading = false;
    }
  }

  async commitFile(): Promise<void> {
    if (!this.file) return;
    this.loading = true;
    this.error = '';
    try {
      this.commit = await this.imports.catalogStockCommit(this.file, this.upsert);
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo confirmar importación';
      if (err?.error?.preview) this.preview = err.error.preview;
    } finally {
      this.loading = false;
    }
  }
}
