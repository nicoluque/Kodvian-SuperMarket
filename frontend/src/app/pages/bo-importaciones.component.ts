import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BoImportService } from '../core/services/bo-import.service';
import { BoModuleNavComponent } from '../shared/components/bo-module-nav.component';

type ImportTab = 'products' | 'customers' | 'suppliers' | 'prices';

@Component({
  standalone: true,
  selector: 'app-bo-importaciones',
  imports: [CommonModule, FormsModule, RouterLink, BoModuleNavComponent],
  template: `
    <main class="wrap">
      <div class="bg-orb orb-a"></div>
      <div class="bg-orb orb-b"></div>
      <app-bo-module-nav />

      <section class="content">
        <header class="hero">
          <div class="hero-copy">
            <h1>Importaciones masivas iniciales</h1>
            <p>Gestioná altas por lote de productos, clientes, proveedores y precios con validación previa.</p>
          </div>
          <div class="hero-actions">
            <span class="hero-pill">Tipo activo: {{ currentLabel }}</span>
            <a class="btn btn-secondary" routerLink="/bo/importaciones/stock-inicial">Ir a Stock inicial (B22)</a>
          </div>
        </header>

        <section class="card flow-card">
          <div class="info-banner">
            <strong>Importante:</strong> esta pantalla crea o actualiza catalogo, pero no impacta stock.
            <a routerLink="/bo/importaciones/unificada">Usar importacion unificada</a>
            <a routerLink="/bo/importaciones/stock-inicial">Ir a stock inicial</a>
          </div>

          <div class="flow-head">
            <h2>Flujo principal</h2>
            <p>Seleccioná el tipo de importación y seguí estos pasos: descargar, subir, validar y confirmar.</p>
          </div>

          <div class="tabs" role="tablist" aria-label="Tipo de importación">
            <button *ngFor="let t of tabs" [class.active]="tab === t.key" (click)="selectTab(t.key)">{{ t.label }}</button>
          </div>

          <div class="toolbar-grid">
            <button class="btn btn-secondary" [disabled]="loadingTemplate" (click)="downloadTemplate()">{{ loadingTemplate ? 'Descargando...' : '1) Descargar plantilla' }}</button>
            <label class="file-drop">
              <span class="file-title">2) Subir archivo</span>
              <span class="file-sub">{{ file ? file.name : 'Seleccionar archivo .xlsx' }}</span>
              <input class="file-input" type="file" accept=".xlsx" (change)="onFile($event)" />
            </label>
            <button class="btn btn-secondary" [disabled]="!file || loading" (click)="doPreview()">3) Vista previa</button>
            <button class="btn btn-primary" [disabled]="!file || !preview || loading" (click)="doCommit()">4) Confirmar</button>
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
            <p class="meta" *ngIf="file">Archivo seleccionado: <strong>{{ file.name }}</strong></p>
            <p class="hint" *ngIf="!file">Solo se permite formato <strong>.xlsx</strong> descargado desde plantilla.</p>
          </div>
        </section>

        <section class="alert error" *ngIf="error" aria-live="assertive">{{ error }}</section>

        <section class="card empty-state" *ngIf="!preview && !commit && !loading">
          <h3>Sin importaciones cargadas</h3>
          <ol>
            <li>Descargá la plantilla de {{ currentLabel }}.</li>
            <li>Completá los datos en el archivo.</li>
            <li>Subí el archivo y ejecutá vista previa.</li>
            <li>Si está correcto, confirmá la importación.</li>
          </ol>
        </section>

        <section class="card" *ngIf="loading" aria-live="polite">
          <div class="loading-box">
            <span class="spinner" aria-hidden="true"></span>
            <p class="meta">Procesando archivo, aguardá un momento...</p>
          </div>
        </section>

        <section class="card" *ngIf="preview">
          <div class="section-title">
            <h3>Resultado de vista previa</h3>
            <span class="subtitle">{{ currentLabel }}</span>
          </div>
          <div class="kpis">
            <div class="kpi"><span>Total filas</span><strong>{{ preview.totalRows }}</strong></div>
            <div class="kpi"><span>Válidas</span><strong>{{ preview.validRows }}</strong></div>
            <div class="kpi"><span>Inválidas</span><strong>{{ preview.invalidRows }}</strong></div>
          </div>

          <div class="table-scroll">
            <div class="preview-tools">
              <label class="mini-check"><input type="checkbox" [(ngModel)]="showOnlyErrors" /> Solo con error</label>
              <span class="meta">Mostrando {{ filteredPreviewRows.length }} fila(s)</span>
            </div>
            <table>
              <thead>
                <tr>
                  <th>Fila</th>
                  <th>Estado</th>
                  <th>Identificador</th>
                  <th>Nombre</th>
                  <th>Tipo</th>
                  <th>Precio</th>
                  <th>Detalle</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let r of filteredPreviewRows" [class.invalid-row]="!r.valid">
                  <td>{{ r.rowNumber }}</td>
                  <td>
                    <span class="status" [class.ok-status]="r.valid" [class.error-status]="!r.valid">{{ r.valid ? 'Válida' : 'Con error' }}</span>
                  </td>
                  <td>{{ rowIdentifier(r) }}</td>
                  <td>{{ rowName(r) }}</td>
                  <td>{{ rowType(r) }}</td>
                  <td>{{ rowPrice(r) }}</td>
                  <td>
                    <span *ngIf="!r.errors?.length" class="ok-text">Sin observaciones</span>
                    <span class="error-line" *ngFor="let e of r.errors">{{ translateError(e) }}</span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        <section class="card" *ngIf="commit">
          <div class="section-title">
            <h3>Resultado de confirmación</h3>
            <button class="btn btn-secondary" (click)="resetFlow()">Nueva importación</button>
          </div>
          <div class="kpis">
            <div class="kpi"><span>Creados</span><strong>{{ commit.created }}</strong></div>
            <div class="kpi"><span>Actualizados</span><strong>{{ commit.updated }}</strong></div>
          </div>
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
    `.hero{background:linear-gradient(120deg,#0e5a42,#247a58 54%,#3a9b75);color:#fff;border-radius:22px;padding:22px 24px;box-shadow:0 18px 40px rgba(22,66,46,.22);display:flex;justify-content:space-between;align-items:flex-start;gap:12px;flex-wrap:wrap}`,
    `.hero-copy{max-width:760px}`,
    `.hero-actions{display:flex;flex-direction:column;gap:8px;align-items:flex-end}`,
    `.hero h1{margin:0 0 8px;font-size:clamp(24px,2.4vw,32px);line-height:1.1}`,
    `.hero p{margin:0;color:rgba(255,255,255,.95)}`,
    `.hero-pill{display:inline-flex;align-items:center;background:rgba(255,255,255,.22);border:1px solid rgba(255,255,255,.4);border-radius:999px;padding:6px 10px;font-size:12px;font-weight:700;color:#ffffff}`,
    `.card{border:1px solid rgba(16,94,67,.16);border-radius:18px;padding:16px;background:rgba(255,255,255,.94);box-shadow:0 14px 32px rgba(33,73,57,.12);backdrop-filter:blur(2px);display:flex;flex-direction:column;gap:12px}`,
    `.flow-card{gap:14px}`,
    `.info-banner{display:flex;gap:10px;flex-wrap:wrap;align-items:center;padding:10px 12px;border:1px solid #d0e5da;background:#edf8f2;border-radius:12px;color:#1d5a43}`,
    `.info-banner a{text-decoration:none;color:#15543d;background:#dff2e9;border:1px solid #b8dbc9;border-radius:999px;padding:5px 10px;font-size:12px;font-weight:700}`,
    `.flow-head h2{margin:0;font-size:20px;color:#184f3c}`,
    `.flow-head p{margin:6px 0 0;color:#365d4d}`,
    `.tabs{display:flex;gap:8px;flex-wrap:wrap}`,
    `.tabs button{border:1px solid #bfd9cc;background:#f2faf6;color:#1f5f45;border-radius:999px;padding:9px 12px;font-weight:700;cursor:pointer;min-height:40px;transition:all .18s ease}`,
    `.tabs button:hover{background:#e8f5ef;border-color:#9dcab5}`,
    `.tabs button.active{background:#dff2e9;border-color:#2f8e67;color:#1f6f50}`,
    `.tabs button:focus-visible{outline:none;box-shadow:0 0 0 3px rgba(47,142,103,.22)}`,
    `.toolbar-grid{display:grid;gap:10px;grid-template-columns:2fr 2fr 1fr 1fr;align-items:stretch}`,
    `.file-drop{position:relative;display:flex;flex-direction:column;justify-content:center;border:1px dashed #7ebda0;border-radius:12px;background:#f4fbf8;color:#23644b;padding:8px 12px;cursor:pointer;min-height:42px;gap:2px}`,
    `.file-title{font-weight:700;font-size:13px}`,
    `.file-sub{font-size:12px;color:#2e5f4c;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}`,
    `.file-input{position:absolute;inset:0;opacity:0;cursor:pointer}`,
    `.btn{border:1px solid transparent;border-radius:10px;padding:10px 12px;font-weight:700;cursor:pointer;text-decoration:none;display:inline-flex;align-items:center;justify-content:center;min-height:42px;transition:transform .14s ease,box-shadow .2s ease,background .2s ease}`,
    `.btn-primary{border-color:#2c7f5d;background:linear-gradient(135deg,#2f8e67,#1f6f50);color:#fff}`,
    `.btn-secondary{border-color:#2b7f5c;background:#e7f4ed;color:#15543d}`,
    `.btn:hover:not([disabled]){transform:translateY(-1px);box-shadow:0 8px 16px rgba(31,111,80,.18)}`,
    `.btn:focus-visible{outline:none;box-shadow:0 0 0 3px rgba(47,142,103,.2)}`,
    `.btn[disabled]{opacity:.6;cursor:not-allowed}`,
    `.upsert-row{display:flex;align-items:center;gap:10px;padding:10px 12px;border:1px solid #d8e8df;border-radius:12px;background:#f7fcf9;color:#1f5b44;max-width:520px}`,
    `.upsert-copy{display:flex;flex-direction:column;gap:2px}`,
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
    `.kpis{display:grid;grid-template-columns:repeat(auto-fit,minmax(140px,1fr));gap:10px}`,
    `.kpi{border:1px solid #dcebe3;background:#f8fcfa;border-radius:12px;padding:10px;display:flex;flex-direction:column;gap:4px}`,
    `.kpi span{font-size:12px;color:#2f5e4b}`,
    `.kpi strong{font-size:22px;color:#0f4f3a;font-variant-numeric:tabular-nums}`,
    `.section-title{display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap}`,
    `.section-title h3{margin:0;color:#184f3c}`,
    `.subtitle{display:inline-flex;background:#edf8f2;color:#16543d;border:1px solid #bfd9cc;border-radius:999px;padding:4px 10px;font-size:12px;font-weight:700}`,
    `.table-scroll{overflow:auto;max-width:100%}`,
    `.preview-tools{display:flex;justify-content:space-between;align-items:center;gap:10px;padding:4px 2px}`,
    `.mini-check{display:inline-flex;gap:6px;align-items:center;color:#235a44;font-size:13px;font-weight:600}`,
    `table{width:100%;border-collapse:collapse;background:#fff;border-radius:12px;overflow:hidden;min-width:560px}`,
    `th,td{border-bottom:1px solid #edf3ef;padding:8px;text-align:left;vertical-align:top}`,
    `th{color:#1e5f47;background:#f5fbf8;position:sticky;top:0}`,
    `tbody tr:hover{background:#f9fcfa}`,
    `.invalid-row{background:#fff9f9}`,
    `.status{display:inline-block;padding:2px 8px;border-radius:999px;font-size:12px;font-weight:700;border:1px solid transparent}`,
    `.ok-status{background:#e4f7ea;color:#0f6a3d;border-color:#8fc8a9}`,
    `.error-status{background:#fde9e9;color:#8f1b1b;border-color:#e6a7a7}`,
    `.error-line{display:block;color:#7d2b2b;line-height:1.35;margin-bottom:2px}`,
    `.ok-text{color:#2f5f4c;font-size:13px}`,
    `.meta{margin:0;color:#2f5f4c}`,
    `.hint{margin:0;color:#366552}`,
    `.alert{border-radius:12px;padding:12px 14px}`,
    `.alert.error{background:#feefef;border:1px solid #f3c6c6;color:#9f1f1f;font-weight:700}`,
    `.empty-state h3{margin:0;color:#184f3c}`,
    `.empty-state ol{margin:0;padding-left:20px;color:#2f5f4c;display:flex;flex-direction:column;gap:6px}`,
    `.loading-box{display:flex;align-items:center;gap:10px}`,
    `.spinner{width:18px;height:18px;border-radius:999px;border:2px solid #c9e2d7;border-top-color:#2f8e67;animation:spin .9s linear infinite}`,
    `@keyframes spin{to{transform:rotate(360deg)}}`,
    `@media (max-width: 1080px){.toolbar-grid{grid-template-columns:repeat(2,minmax(0,1fr))}}`,
    `@media (max-width: 900px){.wrap{padding:16px}.hero-actions{align-items:flex-start;width:100%}.hero-pill{align-self:flex-start}.toolbar-grid{grid-template-columns:1fr}.btn,.file-drop{width:100%}.upsert-row{max-width:none}.preview-tools{flex-direction:column;align-items:flex-start}}`
  ]
})
export class BoImportacionesComponent {
  tabs = [
    { key: 'products' as const, label: 'Productos' },
    { key: 'customers' as const, label: 'Clientes' },
    { key: 'suppliers' as const, label: 'Proveedores' },
    { key: 'prices' as const, label: 'Precios' }
  ];

  tab: ImportTab = 'products';
  upsert = true;
  file: File | null = null;
  preview: any = null;
  commit: any = null;
  loading = false;
  loadingTemplate = false;
  showOnlyErrors = false;
  error = '';

  constructor(private readonly imports: BoImportService) {}

  get currentLabel(): string {
    return this.tabs.find(t => t.key === this.tab)?.label ?? this.tab;
  }

  async downloadTemplate(): Promise<void> {
    this.loadingTemplate = true;
    this.error = '';
    try {
      await this.imports.downloadTemplate(this.tab);
    } catch (err: any) {
      if (err?.status === 401) {
        this.error = 'Sesión expirada o faltan cabeceras operativas. Iniciá sesión operativa y reintentá.';
      } else if (err?.status === 403) {
        this.error = 'Tu rol no tiene permisos para descargar esta plantilla.';
      } else {
        this.error = err?.error?.message ?? 'No se pudo descargar plantilla';
      }
    } finally {
      this.loadingTemplate = false;
    }
  }

  selectTab(tab: ImportTab): void {
    this.tab = tab;
    this.file = null;
    this.preview = null;
    this.commit = null;
    this.error = '';
  }

  onFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    const selected = input.files?.[0] ?? null;
    if (selected && !selected.name.toLowerCase().endsWith('.xlsx')) {
      this.file = null;
      this.preview = null;
      this.commit = null;
      this.error = 'Formato inválido. Debes subir un archivo .xlsx descargado desde la plantilla.';
      input.value = '';
      return;
    }

    this.file = selected;
    this.preview = null;
    this.commit = null;
    this.error = '';
  }

  async doPreview(): Promise<void> {
    if (!this.file) return;
    this.loading = true;
    this.error = '';
    this.commit = null;
    try {
      this.preview = await this.imports.preview(this.tab, this.file, this.upsert);
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo generar preview';
    } finally {
      this.loading = false;
    }
  }

  async doCommit(): Promise<void> {
    if (!this.file) return;
    this.loading = true;
    this.error = '';
    try {
      this.commit = await this.imports.commit(this.tab, this.file, this.upsert);
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo confirmar importación';
    } finally {
      this.loading = false;
    }
  }

  resetFlow(): void {
    this.file = null;
    this.preview = null;
    this.commit = null;
    this.error = '';
  }

  get previewRows(): any[] {
    const rows = Array.isArray(this.preview?.rows) ? this.preview.rows : [];
    return [...rows].sort((a, b) => {
      if (a.valid === b.valid) return (a.rowNumber ?? 0) - (b.rowNumber ?? 0);
      return a.valid ? 1 : -1;
    });
  }

  get filteredPreviewRows(): any[] {
    if (!this.showOnlyErrors) return this.previewRows;
    return this.previewRows.filter(r => !r.valid);
  }

  rowIdentifier(row: any): string {
    const barcode = this.getCell(row, 'barcode');
    const quickCode = this.getCell(row, 'quickCode');
    const dni = this.getCell(row, 'dni');
    const cuit = this.getCell(row, 'cuit');
    return barcode || quickCode || dni || cuit || '-';
  }

  rowName(row: any): string {
    return this.getCell(row, 'name') || this.getCell(row, 'fullName') || '-';
  }

  rowType(row: any): string {
    return this.getCell(row, 'saleType') || '-';
  }

  rowPrice(row: any): string {
    const price = this.getCell(row, 'price');
    const pricePerKg = this.getCell(row, 'pricePerKg');
    if (price && pricePerKg) return `${price} / ${pricePerKg} kg`;
    return price || pricePerKg || '-';
  }

  translateError(error: any): string {
    const field = this.translateField(error?.field ?? 'campo');
    const message = this.translateMessage(error?.message ?? 'error de validacion');
    return `${field}: ${message}`;
  }

  private getCell(row: any, key: string): string {
    const value = row?.data?.[key] ?? row?.data?.[key.toLowerCase()] ?? row?.data?.[key.toUpperCase()] ?? '';
    return typeof value === 'string' ? value.trim() : '';
  }

  private translateField(field: string): string {
    const map: Record<string, string> = {
      saleType: 'Tipo de venta',
      name: 'Nombre',
      fullName: 'Nombre completo',
      price: 'Precio',
      pricePerKg: 'Precio por kg',
      'barcode/quickCode': 'Codigo de barras o codigo rapido',
      barcode: 'Codigo de barras',
      quickCode: 'Codigo rapido'
    };
    return map[field] ?? field;
  }

  private translateMessage(message: string): string {
    const lower = (message || '').toLowerCase().trim();
    if (!lower) return 'error de validacion';

    if (lower.includes('required') || lower.endsWith('requerido') || lower.endsWith('requerida')) return 'requerido';
    if (lower.includes('invalid') || lower.includes('invalido') || lower.includes('invalida')) return 'valor invalido';
    if (lower === 'producto existente') return 'ya existe';
    if (lower === 'producto no encontrado') return 'no encontrado';
    if (lower.includes('preview failed')) return 'fallo la vista previa';
    return message;
  }
}
