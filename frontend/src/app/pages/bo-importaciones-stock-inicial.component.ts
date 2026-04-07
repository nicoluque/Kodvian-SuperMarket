import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BoImportService } from '../core/services/bo-import.service';
import { BoModuleNavComponent } from '../shared/components/bo-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-bo-importaciones-stock-inicial',
  imports: [CommonModule, FormsModule, BoModuleNavComponent],
  template: `
    <main class="wrap">
      <div class="bg-orb orb-a"></div>
      <div class="bg-orb orb-b"></div>
      <app-bo-module-nav />

      <header class="hero">
        <h1>Importación de stock inicial</h1>
        <p>Carga el archivo B22, valida diferencias y confirma con control de errores antes de impactar stock.</p>
      </header>

      <section class="card">
        <div class="row actions">
          <button class="btn btn-secondary" [disabled]="loadingTemplate" (click)="downloadTemplate()">{{ loadingTemplate ? 'Descargando...' : 'Descargar plantilla' }}</button>
          <label class="file">Subir archivo <input type="file" accept=".xlsx" (change)="onFile($event)" /></label>
          <button class="btn btn-secondary" [disabled]="!file || loading" (click)="previewFile()">Vista previa</button>
          <button class="btn btn-primary" [disabled]="!preview || loading" (click)="commit(false)">Confirmar</button>
        </div>
        <p *ngIf="file" class="meta">Archivo: <strong>{{ file.name }}</strong></p>
      </section>

      <section class="card" *ngIf="preview">
        <h3>Vista previa sesión #{{ preview.sessionId }}</h3>
        <div class="kpis">
          <div class="kpi"><span>Total</span><strong>{{ preview.totalRows }}</strong></div>
          <div class="kpi"><span>Errores</span><strong>{{ preview.errorRows }}</strong></div>
        </div>
        <p *ngIf="preview.warningMessage" class="warn">{{ preview.warningMessage }}</p>

        <div class="row filters">
          <label><input type="checkbox" [(ngModel)]="onlyErrors" /> Solo errores</label>
          <label><input type="checkbox" [(ngModel)]="onlyDiffs" /> Solo diferencias</label>
        </div>

        <div class="table-scroll">
          <table>
            <thead>
              <tr>
                <th>Fila</th><th>Producto</th><th>Actual</th><th>Target</th><th>Delta</th><th>Error</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let l of filteredLines()">
                <td>{{ l.rowNumber }}</td>
                <td>{{ l.productName || l.barcode || l.quickCode }}</td>
                <td>V {{ l.currentVendibleQty }} / R {{ l.currentReclamoQty }} / M {{ l.currentMermaQty }}</td>
                <td>V {{ l.targetVendibleQty }} / R {{ l.targetReclamoQty }} / M {{ l.targetMermaQty }}</td>
                <td>V {{ l.deltaVendibleQty }} / R {{ l.deltaReclamoQty }} / M {{ l.deltaMermaQty }}</td>
                <td>{{ l.error || '-' }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <p *ngIf="filteredLines().length === 0" class="meta">No hay líneas para mostrar con los filtros actuales.</p>

        <button class="btn btn-warning" *ngIf="preview.requiresExplicitConfirmation" [disabled]="loading" (click)="commit(true)">Confirmar con advertencia fuerte</button>
      </section>

      <p *ngIf="result" class="ok">Commit: {{ result.status }} (líneas {{ result.totalLines }})</p>
      <p *ngIf="error" class="error">{{ error }}</p>
    </main>
  `,
  styles: [
    `.wrap{position:relative;overflow-x:clip;min-height:100vh;padding:24px;display:flex;flex-direction:column;gap:14px;font-family:'Montserrat','Segoe UI',sans-serif;background:linear-gradient(160deg,#fdf7ef 0%,#fff8ee 46%,#f2faf7 100%)}`,
    `.bg-orb{position:absolute;border-radius:999px;filter:blur(4px);pointer-events:none;opacity:.5}`,
    `.orb-a{width:320px;height:320px;right:0;top:80px;transform:translateX(22%);background:radial-gradient(circle,#ffcf9d 0%,rgba(255,207,157,0) 68%)}`,
    `.orb-b{width:300px;height:300px;left:0;top:260px;transform:translateX(-35%);background:radial-gradient(circle,#9fe6c6 0%,rgba(159,230,198,0) 70%)}`,
    `.hero{position:relative;z-index:1;background:linear-gradient(120deg,#0e5a42,#247a58 54%,#3a9b75);color:#fff;border-radius:22px;padding:22px 24px;box-shadow:0 18px 40px rgba(22,66,46,.22)}`,
    `.hero h1{margin:0 0 8px;font-size:30px}`,
    `.hero p{margin:0;color:rgba(255,255,255,.9)}`,
    `.card{position:relative;z-index:1;border:1px solid rgba(16,94,67,.16);border-radius:18px;padding:14px;background:rgba(255,255,255,.94);box-shadow:0 14px 32px rgba(33,73,57,.12);display:flex;flex-direction:column;gap:12px}`,
    `.row{display:flex;gap:8px;flex-wrap:wrap;align-items:center}`,
    `.actions{gap:10px}`,
    `.file{display:inline-block;padding:10px 12px;border:1px dashed #7ebda0;border-radius:12px;background:#f4fbf8;font-weight:600;color:#23644b}`,
    `.file input{margin-left:8px}`,
    `.btn{border:1px solid transparent;border-radius:10px;padding:10px 12px;font-weight:700;cursor:pointer;text-decoration:none;display:inline-flex;align-items:center;justify-content:center}`,
    `.btn-primary{border-color:#2c7f5d;background:linear-gradient(135deg,#2f8e67,#1f6f50);color:#fff}`,
    `.btn-secondary{border-color:#2f8e67;background:#e8f5ef;color:#1f6f50}`,
    `.btn-warning{border-color:#c58a22;background:#fff4df;color:#8a5a00}`,
    `.btn[disabled]{opacity:.6;cursor:not-allowed}`,
    `.kpis{display:grid;grid-template-columns:repeat(auto-fit,minmax(140px,1fr));gap:10px}`,
    `.kpi{border:1px solid #dcebe3;background:#f8fcfa;border-radius:12px;padding:10px;display:flex;flex-direction:column;gap:4px}`,
    `.kpi span{font-size:12px;color:#4e7463}`,
    `.kpi strong{font-size:22px;color:#0f4f3a}`,
    `.filters{padding:8px 10px;border:1px solid #e3efe9;background:#fbfefd;border-radius:12px}`,
    `.table-scroll{overflow:auto;max-width:100%}`,
    `table{width:100%;border-collapse:collapse;background:#fff;border-radius:12px;overflow:hidden;min-width:860px}`,
    `th,td{border-bottom:1px solid #edf3ef;padding:8px;text-align:left}`,
    `th{color:#1e5f47;background:#f5fbf8}`,
    `.meta{margin:0;color:#4b6f60}`,
    `.warn{margin:0;color:#8a5a00;font-weight:600}`,
    `.ok{margin:0;color:#0a7a32;font-weight:700}`,
    `.error{margin:0;color:#b3261e;font-weight:700}`,
    `@media (max-width: 900px){.wrap{padding:16px}.hero h1{font-size:24px}}`
  ]
})
export class BoImportacionesStockInicialComponent {
  file: File | null = null;
  preview: any = null;
  result: any = null;
  error = '';
  loading = false;
  loadingTemplate = false;
  onlyErrors = false;
  onlyDiffs = false;

  constructor(private readonly imports: BoImportService) {}

  async downloadTemplate(): Promise<void> {
    this.loadingTemplate = true;
    this.error = '';
    try {
      await this.imports.downloadStockOpeningTemplate();
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

  onFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    const selected = input.files?.[0] ?? null;
    if (selected && !selected.name.toLowerCase().endsWith('.xlsx')) {
      this.file = null;
      this.error = 'Formato inválido. Para stock inicial debes subir un archivo .xlsx descargado desde la plantilla.';
      this.preview = null;
      this.result = null;
      input.value = '';
      return;
    }

    this.file = selected;
    this.preview = null;
    this.result = null;
    this.error = '';
  }

  async previewFile(): Promise<void> {
    if (!this.file) return;
    this.loading = true;
    this.error = '';
    try {
      this.preview = await this.imports.stockOpeningPreview(this.file);
      this.result = null;
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo generar preview';
    } finally {
      this.loading = false;
    }
  }

  filteredLines(): any[] {
    if (!this.preview?.lines) return [];
    return this.preview.lines.filter((l: any) => {
      if (this.onlyErrors && !l.error) return false;
      const hasDiff = Number(l.deltaVendibleQty) !== 0 || Number(l.deltaReclamoQty) !== 0 || Number(l.deltaMermaQty) !== 0;
      if (this.onlyDiffs && !hasDiff) return false;
      return true;
    });
  }

  async commit(explicit: boolean): Promise<void> {
    if (!this.preview?.sessionId) return;
    this.loading = true;
    this.error = '';
    try {
      this.result = await this.imports.stockOpeningCommit(this.preview.sessionId, explicit);
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo confirmar stock inicial';
    } finally {
      this.loading = false;
    }
  }
}
