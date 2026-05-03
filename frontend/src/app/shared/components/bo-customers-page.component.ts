import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

type CustomerStatusFilter = '' | 'Active' | 'Inactive' | 'Pending' | 'Critical';
type CustomerFormStatus = 'Active' | 'Inactive' | 'Pending';

@Component({
  standalone: true,
  selector: 'app-bo-customers-page',
  imports: [CommonModule, FormsModule],
  template: `
    <section class="bo-card">
      <div class="section-head">
        <h3>Clientes</h3>
        <p>Altas, edición y gestión de estado de clientes.</p>
      </div>

      <div class="toolbar">
        <div class="field-block toolbar-field">
          <label class="field-label" for="customer-filter">Buscar cliente</label>
          <input id="customer-filter" class="field" [(ngModel)]="customerFilter" (ngModelChange)="customerPage = 1" placeholder="Nombre, DNI o teléfono" />
        </div>
        <div class="field-block toolbar-field">
          <label class="field-label" for="customer-status-filter">Estado</label>
          <select id="customer-status-filter" class="field" [(ngModel)]="customerStatusFilter" (ngModelChange)="customerPage = 1">
            <option value="">Todos los estados</option>
            <option value="Active">Activado</option>
            <option value="Inactive">Desactivado</option>
            <option value="Pending">Pendiente</option>
            <option value="Critical">Crítico/Alerta</option>
          </select>
        </div>
        <label class="checkbox"><input type="checkbox" [(ngModel)]="customerCriticalOnly" (ngModelChange)="customerPage = 1" /> Solo críticos</label>
        <button class="btn btn-primary toolbar-action" (click)="startCreateCustomer()">Nuevo cliente</button>
        <button class="btn btn-secondary toolbar-action" (click)="loadCustomers()" [disabled]="loadingCustomers">{{ loadingCustomers ? 'Actualizando...' : 'Actualizar clientes' }}</button>
      </div>

      <div class="form-card" *ngIf="isCustomerFormOpen">
        <div class="field-block">
          <label class="field-label" for="customer-full-name">Nombre completo</label>
          <input id="customer-full-name" class="field" [(ngModel)]="customerForm.fullName" placeholder="Nombre y apellido" />
        </div>
        <div class="field-block">
          <label class="field-label" for="customer-dni">DNI</label>
          <input id="customer-dni" class="field" [(ngModel)]="customerForm.dni" placeholder="Documento" />
        </div>
        <div class="field-block">
          <label class="field-label" for="customer-phone">Teléfono</label>
          <input id="customer-phone" class="field" [(ngModel)]="customerForm.phone" placeholder="Número principal" />
        </div>
        <div class="field-block">
          <label class="field-label" for="customer-phone-backup">Teléfono alternativo</label>
          <input id="customer-phone-backup" class="field" [(ngModel)]="customerForm.phoneBackup" placeholder="Número alternativo" />
        </div>
        <div class="field-block">
          <label class="field-label" for="customer-address">Dirección</label>
          <input id="customer-address" class="field" [(ngModel)]="customerForm.address" placeholder="Calle y altura" />
        </div>
        <div class="field-block">
          <label class="field-label" for="customer-birth-date">Fecha de nacimiento</label>
          <input id="customer-birth-date" class="field" [(ngModel)]="customerForm.birthDate" type="date" />
        </div>
        <div class="field-block">
          <label class="field-label" for="customer-status">Estado</label>
          <select id="customer-status" class="field" [(ngModel)]="customerForm.status">
            <option value="Active">Activado</option>
            <option value="Pending">Pendiente</option>
            <option value="Inactive">Desactivado</option>
          </select>
        </div>
        <div class="field-block">
          <label class="field-label" for="customer-credit-limit">Límite de crédito</label>
          <input id="customer-credit-limit" class="field" [(ngModel)]="customerForm.creditLimit" type="number" min="0" placeholder="Monto máximo" />
        </div>
        <label class="checkbox"><input type="checkbox" [(ngModel)]="customerForm.isFixedCustomer" /> Cliente fijo</label>
        <label class="checkbox"><input type="checkbox" [(ngModel)]="customerForm.allowsCredit" /> Habilitar cuenta corriente</label>
        <div class="actions">
          <button class="btn btn-primary" (click)="saveCustomer()" [disabled]="savingCustomer">{{ savingCustomer ? 'Guardando...' : (customerForm.id ? 'Guardar cambios' : 'Crear cliente') }}</button>
          <button class="btn btn-secondary" (click)="cancelCustomerForm()" [disabled]="savingCustomer">Cancelar</button>
        </div>
      </div>

      <p *ngIf="customerFormError" class="error">{{ customerFormError }}</p>
      <p *ngIf="customerError" class="error">{{ customerError }}</p>

      <div class="table-wrap" *ngIf="!customerError">
      <table class="table">
        <thead>
          <tr>
            <th>Cliente</th>
            <th>DNI</th>
            <th>Telefono</th>
            <th>Estado</th>
            <th>Cuenta corriente</th>
            <th>Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let c of pagedCustomers()">
            <td>{{ c.fullName }}</td>
            <td>{{ c.dni || '-' }}</td>
            <td>{{ c.phone || '-' }}</td>
            <td>
              <span class="status-pill" [style.background]="customerStatusColor(c.effectiveStatus || c.status)">{{ customerStatusLabel(c.effectiveStatus || c.status) }}</span>
            </td>
            <td>{{ c.allowsCredit ? 'Sí' : 'No' }} · Uso {{ c.creditUsedPct || 0 }}%</td>
            <td class="row-actions">
              <button class="btn btn-secondary" (click)="startEditCustomer(c)" [disabled]="savingCustomer">Editar</button>
              <button class="btn btn-secondary" (click)="toggleCustomerStatus(c)" [disabled]="savingCustomer">{{ c.status === 'Inactive' ? 'Reactivar' : 'Desactivar' }}</button>
            </td>
          </tr>
        </tbody>
      </table>
      </div>

      <div *ngIf="!loadingCustomers && !customerError && filteredCustomersRows().length > 0" class="pager">
        <span>Mostrando {{ pageRangeLabel(filteredCustomersRows().length, customerPage) }}</span>
        <div class="pager-actions">
          <button class="btn btn-secondary" (click)="prevPage()" [disabled]="customerPage <= 1">Anterior</button>
          <span>Página {{ customerPage }}/{{ totalPages(filteredCustomersRows().length) }}</span>
          <button class="btn btn-secondary" (click)="nextPage(filteredCustomersRows().length)" [disabled]="customerPage >= totalPages(filteredCustomersRows().length)">Siguiente</button>
        </div>
      </div>
      <p *ngIf="!loadingCustomers && filteredCustomersRows().length === 0" class="empty">No hay clientes para el filtro aplicado.</p>
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
    `.form-card{border:1px solid #dcebe5;border-radius:10px;padding:10px;display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px;background:#fbfefd}`,
    `.actions{display:flex;gap:6px;align-items:center;grid-column:1 / -1}`,
    `.error{color:#b3261e;margin:0}`,
    `.table-wrap{overflow:auto;border:1px solid #e5efeb;border-radius:10px}`,
    `.table{width:100%;border-collapse:collapse;font-size:13px;background:#fff}`,
    `.table thead th{text-align:left;padding:10px 9px;border-bottom:1px solid #0d8a6a;background:#0fa47f;color:#fff;font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:.04em}`,
    `.table td{padding:9px;border-bottom:1px solid #edf4f1}`,
    `.status-pill{padding:2px 8px;border-radius:999px;font-size:11px;font-weight:700}`,
    `.row-actions{display:flex;gap:6px;flex-wrap:wrap}`,
    `.pager{display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;color:#555;font-size:12px}`,
    `.pager-actions{display:flex;gap:6px;align-items:center}`,
    `.empty{margin:0;color:#555}`,
    `@media (max-width: 1200px){.form-card{grid-template-columns:repeat(2,minmax(0,1fr))}}`,
    `@media (max-width: 760px){.form-card{grid-template-columns:1fr}.field{min-width:0}.row-actions{flex-direction:column;align-items:flex-start}}`
  ]
})
export class BoCustomersPageComponent {
  private readonly http = inject(HttpClient);
  private readonly pageSize = 12;

  customers: any[] = [];
  customerFilter = '';
  customerStatusFilter: CustomerStatusFilter = '';
  customerCriticalOnly = false;
  customerPage = 1;
  loadingCustomers = false;
  customerError = '';
  isCustomerFormOpen = false;
  savingCustomer = false;
  customerFormError = '';
  customerForm: {
    id: number | null;
    fullName: string;
    dni: string;
    address: string;
    phone: string;
    phoneBackup: string;
    birthDate: string;
    isFixedCustomer: boolean;
    allowsCredit: boolean;
    creditLimit: string;
    status: CustomerFormStatus;
  } = {
    id: null,
    fullName: '',
    dni: '',
    address: '',
    phone: '',
    phoneBackup: '',
    birthDate: '',
    isFixedCustomer: false,
    allowsCredit: false,
    creditLimit: '0',
    status: 'Pending'
  };

  constructor() {
    void this.loadCustomers();
  }

  async loadCustomers(): Promise<void> {
    this.loadingCustomers = true;
    this.customerError = '';
    try {
      const rows: any = await firstValueFrom(this.http.get('/api/v1/customers'));
      this.customers = Array.isArray(rows) ? rows : [];
      this.customerPage = 1;
    } catch (err: any) {
      this.customerError = this.formatError(err, 'No se pudo cargar clientes');
    } finally {
      this.loadingCustomers = false;
    }
  }

  startCreateCustomer(): void {
    this.isCustomerFormOpen = true;
    this.customerFormError = '';
    this.customerForm = {
      id: null,
      fullName: '',
      dni: '',
      address: '',
      phone: '',
      phoneBackup: '',
      birthDate: '',
      isFixedCustomer: false,
      allowsCredit: false,
      creditLimit: '0',
      status: 'Pending'
    };
  }

  startEditCustomer(customer: any): void {
    this.isCustomerFormOpen = true;
    this.customerFormError = '';
    this.customerForm = {
      id: Number(customer?.id ?? 0),
      fullName: `${customer?.fullName ?? ''}`,
      dni: `${customer?.dni ?? ''}`,
      address: `${customer?.address ?? ''}`,
      phone: `${customer?.phone ?? ''}`,
      phoneBackup: `${customer?.phoneBackup ?? ''}`,
      birthDate: customer?.birthDate ? `${customer.birthDate}`.slice(0, 10) : '',
      isFixedCustomer: !!customer?.isFixedCustomer,
      allowsCredit: !!customer?.allowsCredit,
      creditLimit: `${customer?.creditLimit ?? 0}`,
      status: this.customerStatusValue(customer?.status)
    };
  }

  cancelCustomerForm(): void {
    this.isCustomerFormOpen = false;
    this.customerFormError = '';
  }

  async saveCustomer(): Promise<void> {
    const fullName = this.customerForm.fullName.trim();
    if (!fullName) {
      this.customerFormError = 'El nombre completo es obligatorio.';
      return;
    }

    const creditLimit = Number(this.customerForm.creditLimit);
    if (!Number.isFinite(creditLimit) || creditLimit < 0) {
      this.customerFormError = 'El límite de crédito debe ser un número mayor o igual a 0.';
      return;
    }

    this.savingCustomer = true;
    this.customerFormError = '';
    try {
      const payload: any = {
        fullName,
        dni: this.customerForm.dni.trim() || null,
        address: this.customerForm.address.trim() || null,
        phone: this.customerForm.phone.trim() || null,
        phoneBackup: this.customerForm.phoneBackup.trim() || null,
        birthDate: this.customerForm.birthDate || null,
        isFixedCustomer: this.customerForm.isFixedCustomer,
        allowsCredit: this.customerForm.allowsCredit,
        creditLimit,
        status: this.customerForm.status
      };

      if (this.customerForm.id) {
        await firstValueFrom(this.http.put(`/api/v1/customers/${this.customerForm.id}`, payload));
      } else {
        await firstValueFrom(this.http.post('/api/v1/customers', payload));
      }

      this.isCustomerFormOpen = false;
      await this.loadCustomers();
    } catch (err: any) {
      this.customerFormError = this.formatError(err, 'No se pudo guardar el cliente.');
    } finally {
      this.savingCustomer = false;
    }
  }

  async toggleCustomerStatus(customer: any): Promise<void> {
    const id = Number(customer?.id ?? 0);
    if (!id) return;

    const isInactive = `${customer?.status ?? ''}` === 'Inactive';
    const nextStatus = isInactive ? 'Active' : 'Inactive';

    this.savingCustomer = true;
    this.customerFormError = '';
    try {
      await firstValueFrom(this.http.patch(`/api/v1/customers/${id}/status`, { status: nextStatus }));
      await this.loadCustomers();
    } catch (err: any) {
      this.customerFormError = this.formatError(err, 'No se pudo actualizar el estado del cliente.');
    } finally {
      this.savingCustomer = false;
    }
  }

  filteredCustomersRows(): any[] {
    const q = this.customerFilter.trim().toLowerCase();
    let rows = Array.isArray(this.customers) ? [...this.customers] : [];

    if (this.customerStatusFilter) {
      rows = rows.filter(c => `${c.effectiveStatus ?? c.status ?? ''}` === this.customerStatusFilter);
    }

    if (this.customerCriticalOnly) {
      rows = rows.filter(c => !!c.isCritical);
    }

    if (!q) return rows;
    return rows.filter(c => `${c.fullName ?? ''} ${c.dni ?? ''} ${c.phone ?? ''}`.toLowerCase().includes(q));
  }

  pagedCustomers(): any[] {
    const start = (Math.max(1, this.customerPage) - 1) * this.pageSize;
    return this.filteredCustomersRows().slice(start, start + this.pageSize);
  }

  customerStatusLabel(status: string): string {
    if (status === 'Active') return 'Activado';
    if (status === 'Inactive') return 'Desactivado';
    if (status === 'Pending') return 'Pendiente';
    if (status === 'Critical') return 'Crítico';
    return status || 'Sin estado';
  }

  customerStatusColor(status: string): string {
    if (status === 'Active') return '#e8f6ec';
    if (status === 'Inactive') return '#f2f3f5';
    if (status === 'Pending') return '#fff5df';
    if (status === 'Critical') return '#fde9ea';
    return '#eef2f1';
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
    this.customerPage = Math.max(1, this.customerPage - 1);
  }

  nextPage(totalRows: number): void {
    this.customerPage = Math.min(this.totalPages(totalRows), this.customerPage + 1);
  }

  private customerStatusValue(status: string): CustomerFormStatus {
    if (status === 'Inactive') return 'Inactive';
    if (status === 'Pending') return 'Pending';
    return 'Active';
  }

  private formatError(err: any, fallback: string): string {
    if (err?.status === 0) return 'No hay conexión con el servidor. Verificá red local y API.';
    if (err?.status === 401) return '';
    if (err?.status === 403) return 'No tenés permisos para esta acción.';

    const message = typeof err?.error?.message === 'string' ? err.error.message.trim() : '';
    const details = typeof err?.error?.details === 'string' ? err.error.details.trim() : '';

    if (message && message !== 'Database update failed') return message;
    if (details) return this.formatDbDetails(details);
    if (message) return message;
    return fallback;
  }

  private formatDbDetails(details: string): string {
    const lower = details.toLowerCase();

    if (lower.includes('duplicate key value violates unique constraint')) {
      return 'No se pudo guardar porque ya existe un registro con esos datos.';
    }

    if (lower.includes('violates foreign key constraint')) {
      return 'No se pudo guardar porque uno de los datos relacionados ya no existe o no está disponible.';
    }

    if (lower.includes('value too long for type character varying')) {
      return 'No se pudo guardar porque uno de los textos supera el largo permitido.';
    }

    return `No se pudo guardar por una validación de base de datos. ${details}`;
  }
}
