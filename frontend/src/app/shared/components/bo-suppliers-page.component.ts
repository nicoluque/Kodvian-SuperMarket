import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

type SupplierMode = 'Credit' | 'Refund' | 'Exchange';

@Component({
  standalone: true,
  selector: 'app-bo-suppliers-page',
  imports: [CommonModule, FormsModule],
  template: `
    <section class="bo-card">
      <div class="section-head">
        <h3>Proveedores</h3>
        <p>Altas, edición y política de resolución de reclamos.</p>
      </div>

      <div class="toolbar">
        <div class="field-block toolbar-field">
          <label class="field-label" for="supplier-filter">Buscar proveedor</label>
          <input id="supplier-filter" class="field" [(ngModel)]="supplierFilter" placeholder="Nombre, CUIT, email o teléfono" />
        </div>
        <button class="btn btn-primary toolbar-action" (click)="startCreate()">Nuevo proveedor</button>
        <button class="btn btn-secondary toolbar-action" (click)="loadSuppliers()" [disabled]="loading">{{ loading ? 'Actualizando...' : 'Actualizar proveedores' }}</button>
      </div>

      <div *ngIf="showForm" class="form-card">
        <div class="field-block">
          <label class="field-label" for="supplier-name">Nombre</label>
          <input id="supplier-name" class="field" [(ngModel)]="form.name" placeholder="Razón social o nombre" />
        </div>
        <div class="field-block">
          <label class="field-label" for="supplier-cuit">CUIT</label>
          <input id="supplier-cuit" class="field" [(ngModel)]="form.cuit" placeholder="CUIT" />
        </div>
        <div class="field-block">
          <label class="field-label" for="supplier-phone">Teléfono</label>
          <input id="supplier-phone" class="field" [(ngModel)]="form.phone" placeholder="Número principal" />
        </div>
        <div class="field-block">
          <label class="field-label" for="supplier-email">Email</label>
          <input id="supplier-email" class="field" [(ngModel)]="form.email" placeholder="Correo electrónico" />
        </div>
        <div class="field-block">
          <label class="field-label" for="supplier-address">Dirección</label>
          <input id="supplier-address" class="field" [(ngModel)]="form.address" placeholder="Calle y altura" />
        </div>
        <div class="field-block">
          <label class="field-label" for="supplier-mode">Devoluciones de mercadería</label>
          <select id="supplier-mode" class="field" [(ngModel)]="form.claimSettlementModeDefault">
            <option value="Credit">Crédito</option>
            <option value="Refund">Reembolso</option>
            <option value="Exchange">Reposición</option>
          </select>
        </div>
        <label class="checkbox">
          <input type="checkbox" [(ngModel)]="form.allowClaimSettlementOverride" /> Permitir cambio de condición por reclamo
        </label>
        <label class="checkbox" *ngIf="form.id">
          <input type="checkbox" [(ngModel)]="form.isActive" /> Activo
        </label>
        <div class="actions">
          <button class="btn btn-primary" (click)="save()" [disabled]="saving">{{ saving ? 'Guardando...' : (form.id ? 'Guardar cambios' : 'Crear proveedor') }}</button>
          <button class="btn btn-secondary" (click)="cancelForm()" [disabled]="saving">Cancelar</button>
        </div>
      </div>

      <p *ngIf="formError" class="error">{{ formError }}</p>
      <p *ngIf="error" class="error">{{ error }}</p>

      <div class="table-wrap" *ngIf="!error">
      <table class="table">
        <thead>
          <tr>
            <th>Proveedor</th>
            <th>CUIT</th>
            <th>Teléfono</th>
            <th>Devoluciones de mercadería</th>
            <th>Estado</th>
            <th>Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let s of filteredSuppliers()">
            <td>{{ s.name }}</td>
            <td>{{ s.cuit || '-' }}</td>
            <td>{{ s.phone || '-' }}</td>
            <td>{{ modeLabel(s.claimSettlementModeDefault) }}</td>
            <td>{{ s.isActive ? 'Activo' : 'Inactivo' }}</td>
            <td class="row-actions">
              <button class="btn btn-secondary" (click)="startEdit(s)" [disabled]="saving">Editar</button>
              <button class="btn btn-secondary" (click)="toggleActive(s)" [disabled]="saving">{{ s.isActive ? 'Desactivar' : 'Reactivar' }}</button>
            </td>
          </tr>
        </tbody>
      </table>
      </div>

      <p *ngIf="!loading && filteredSuppliers().length === 0" class="empty">No hay proveedores para el filtro aplicado.</p>
    </section>
  `,
  styles: [
    `.bo-card{background:#fff;border:1px solid #d8e8df;border-radius:14px;padding:14px;display:flex;flex-direction:column;gap:12px;box-shadow:0 4px 18px rgba(5,46,42,.08)}`,
    `.section-head h3{margin:0;color:#0f3a40;font-size:1.1rem}`,
    `.section-head p{margin:2px 0 0;color:#5f7a73;font-size:.85rem}`,
    `.toolbar{display:flex;gap:8px;flex-wrap:wrap;align-items:center}`,
    `.toolbar-field{min-width:220px}`,
    `.toolbar-action{align-self:flex-end}`,
    `.field-block{display:flex;flex-direction:column;gap:4px}`,
    `.field-label{font-size:12px;font-weight:700;color:#355b4f}`,
    `.field{border:1px solid #c6ddd4;background:#f7fcfa;color:#0f3a40;border-radius:8px;min-height:36px;padding:7px 10px;min-width:200px}`,
    `.field:focus{outline:none;border-color:#0fa47f;box-shadow:0 0 0 3px rgba(15,164,127,.15)}`,
    `.checkbox{display:flex;gap:6px;align-items:center;font-size:12px;color:#355b4f}`,
    `.btn{border-radius:8px;min-height:36px;padding:0 12px;border:1px solid transparent;cursor:pointer;font-weight:600}`,
    `.btn:disabled{opacity:.6;cursor:not-allowed}`,
    `.btn-primary{background:#0fa47f;border-color:#0fa47f;color:#fff}`,
    `.btn-primary:hover:not(:disabled){background:#0c8f6f}`,
    `.btn-secondary{background:#eff8f4;border-color:#c6ddd4;color:#0f3a40}`,
    `.btn-secondary:hover:not(:disabled){background:#e2f2ea}`,
    `.form-card{border:1px solid #dcebe5;border-radius:10px;padding:10px;display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:8px;background:#fbfefd}`,
    `.actions{display:flex;gap:6px;align-items:center;grid-column:1 / -1}`,
    `.error{color:#b3261e;margin:0}`,
    `.table-wrap{overflow:auto;border:1px solid #e5efeb;border-radius:10px}`,
    `.table{width:100%;border-collapse:collapse;font-size:13px;background:#fff}`,
    `.table thead th{text-align:left;padding:10px 9px;border-bottom:1px solid #0d8a6a;background:#0fa47f;color:#fff;font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:.04em}`,
    `.table td{padding:9px;border-bottom:1px solid #edf4f1}`,
    `.row-actions{display:flex;gap:6px;flex-wrap:wrap}`,
    `.empty{margin:0;color:#555}`,
    `@media (max-width: 1000px){.form-card{grid-template-columns:repeat(2,minmax(0,1fr))}}`,
    `@media (max-width: 760px){.form-card{grid-template-columns:1fr}.field{min-width:0}.row-actions{flex-direction:column;align-items:flex-start}}`
  ]
})
export class BoSuppliersPageComponent {
  private readonly http = inject(HttpClient);

  suppliers: any[] = [];
  supplierFilter = '';
  loading = false;
  saving = false;
  error = '';
  showForm = false;
  formError = '';

  form: {
    id: number | null;
    name: string;
    cuit: string;
    phone: string;
    email: string;
    address: string;
    isActive: boolean;
    claimSettlementModeDefault: SupplierMode;
    allowClaimSettlementOverride: boolean;
  } = this.emptyForm();

  constructor() {
    void this.loadSuppliers();
  }

  emptyForm() {
    return {
      id: null,
      name: '',
      cuit: '',
      phone: '',
      email: '',
      address: '',
      isActive: true,
      claimSettlementModeDefault: 'Credit' as SupplierMode,
      allowClaimSettlementOverride: true
    };
  }

  async loadSuppliers(): Promise<void> {
    this.loading = true;
    this.error = '';
    try {
      const rows: any = await firstValueFrom(this.http.get('/api/v1/suppliers'));
      this.suppliers = Array.isArray(rows) ? rows : [];
    } catch (err: any) {
      this.error = this.formatError(err, 'No se pudo cargar proveedores');
    } finally {
      this.loading = false;
    }
  }

  startCreate(): void {
    this.showForm = true;
    this.formError = '';
    this.form = this.emptyForm();
  }

  startEdit(supplier: any): void {
    this.showForm = true;
    this.formError = '';
    this.form = {
      id: Number(supplier?.id ?? 0),
      name: `${supplier?.name ?? ''}`,
      cuit: `${supplier?.cuit ?? ''}`,
      phone: `${supplier?.phone ?? ''}`,
      email: `${supplier?.email ?? ''}`,
      address: `${supplier?.address ?? ''}`,
      isActive: !!supplier?.isActive,
      claimSettlementModeDefault: this.normalizeMode(supplier?.claimSettlementModeDefault),
      allowClaimSettlementOverride: !!supplier?.allowClaimSettlementOverride
    };
  }

  cancelForm(): void {
    this.showForm = false;
    this.formError = '';
  }

  async save(): Promise<void> {
    const name = this.form.name.trim();
    if (!name) {
      this.formError = 'El nombre es obligatorio.';
      return;
    }

    this.saving = true;
    this.formError = '';
    try {
      const payload: any = {
        name,
        cuit: this.form.cuit.trim() || null,
        phone: this.form.phone.trim() || null,
        email: this.form.email.trim() || null,
        address: this.form.address.trim() || null,
        isActive: this.form.isActive,
        claimSettlementModeDefault: this.form.claimSettlementModeDefault,
        allowClaimSettlementOverride: this.form.allowClaimSettlementOverride
      };

      if (this.form.id) {
        await firstValueFrom(this.http.put(`/api/v1/suppliers/${this.form.id}`, payload));
      } else {
        await firstValueFrom(this.http.post('/api/v1/suppliers', payload));
      }

      this.showForm = false;
      await this.loadSuppliers();
    } catch (err: any) {
      this.formError = this.formatError(err, 'No se pudo guardar el proveedor.');
    } finally {
      this.saving = false;
    }
  }

  async toggleActive(supplier: any): Promise<void> {
    const id = Number(supplier?.id ?? 0);
    if (!id) return;

    this.saving = true;
    try {
      const payload = {
        name: supplier?.name ?? '',
        cuit: supplier?.cuit ?? null,
        phone: supplier?.phone ?? null,
        email: supplier?.email ?? null,
        address: supplier?.address ?? null,
        isActive: !supplier?.isActive,
        claimSettlementModeDefault: this.normalizeMode(supplier?.claimSettlementModeDefault),
        allowClaimSettlementOverride: !!supplier?.allowClaimSettlementOverride
      };
      await firstValueFrom(this.http.put(`/api/v1/suppliers/${id}`, payload));
      await this.loadSuppliers();
    } catch (err: any) {
      this.error = this.formatError(err, 'No se pudo actualizar estado del proveedor.');
    } finally {
      this.saving = false;
    }
  }

  filteredSuppliers(): any[] {
    const q = this.supplierFilter.trim().toLowerCase();
    if (!q) return this.suppliers;
    return this.suppliers.filter(s => `${s?.name ?? ''} ${s?.cuit ?? ''} ${s?.email ?? ''} ${s?.phone ?? ''}`.toLowerCase().includes(q));
  }

  modeLabel(mode: string): string {
    if (mode === 'Refund') return 'Reembolso';
    if (mode === 'Exchange') return 'Reposición';
    return 'Crédito';
  }

  private normalizeMode(mode: unknown): SupplierMode {
    if (mode === 'Refund') return 'Refund';
    if (mode === 'Exchange') return 'Exchange';
    return 'Credit';
  }

  private formatError(err: any, fallback: string): string {
    if (err?.status === 0) return 'No hay conexión con el servidor. Verificá red local y API.';
    if (err?.status === 401) return '';
    if (err?.status === 403) return 'No tenés permisos para esta acción.';
    const message = typeof err?.error?.message === 'string' ? err.error.message.trim() : '';
    return message || fallback;
  }
}
