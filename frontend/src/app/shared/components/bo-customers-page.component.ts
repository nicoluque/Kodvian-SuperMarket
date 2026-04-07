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
    <section style="border:1px solid #ddd;border-radius:8px;padding:12px;display:flex;flex-direction:column;gap:10px">
      <div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center">
        <input [(ngModel)]="customerFilter" (ngModelChange)="customerPage = 1" placeholder="Buscar cliente" style="padding:8px;min-width:220px" />
        <select [(ngModel)]="customerStatusFilter" (ngModelChange)="customerPage = 1" style="padding:8px">
          <option value="">Todos los estados</option>
          <option value="Active">Activado</option>
          <option value="Inactive">Desactivado</option>
          <option value="Pending">Pendiente</option>
          <option value="Critical">Crítico/Alerta</option>
        </select>
        <label style="display:flex;gap:6px;align-items:center;font-size:12px;color:#355b4f"><input type="checkbox" [(ngModel)]="customerCriticalOnly" (ngModelChange)="customerPage = 1" /> Solo críticos</label>
        <button (click)="startCreateCustomer()">Nuevo cliente</button>
        <button (click)="loadCustomers()" [disabled]="loadingCustomers">{{ loadingCustomers ? 'Actualizando...' : 'Actualizar clientes' }}</button>
      </div>

      <div style="border:1px solid #e6efeb;border-radius:10px;padding:10px;display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px" *ngIf="isCustomerFormOpen">
        <input [(ngModel)]="customerForm.fullName" placeholder="Nombre completo" style="padding:8px" />
        <input [(ngModel)]="customerForm.dni" placeholder="DNI" style="padding:8px" />
        <input [(ngModel)]="customerForm.phone" placeholder="Teléfono" style="padding:8px" />
        <input [(ngModel)]="customerForm.phoneBackup" placeholder="Teléfono alternativo" style="padding:8px" />
        <input [(ngModel)]="customerForm.address" placeholder="Dirección" style="padding:8px" />
        <input [(ngModel)]="customerForm.birthDate" type="date" style="padding:8px" />
        <select [(ngModel)]="customerForm.status" style="padding:8px">
          <option value="Active">Activado</option>
          <option value="Pending">Pendiente</option>
          <option value="Inactive">Desactivado</option>
        </select>
        <input [(ngModel)]="customerForm.creditLimit" type="number" min="0" placeholder="Límite de crédito" style="padding:8px" />
        <label style="display:flex;gap:6px;align-items:center;font-size:12px;color:#355b4f"><input type="checkbox" [(ngModel)]="customerForm.isFixedCustomer" /> Cliente fijo</label>
        <label style="display:flex;gap:6px;align-items:center;font-size:12px;color:#355b4f"><input type="checkbox" [(ngModel)]="customerForm.allowsCredit" /> Habilitar cuenta corriente</label>
        <div style="display:flex;gap:6px;align-items:center;grid-column:1 / -1">
          <button (click)="saveCustomer()" [disabled]="savingCustomer">{{ savingCustomer ? 'Guardando...' : (customerForm.id ? 'Guardar cambios' : 'Crear cliente') }}</button>
          <button (click)="cancelCustomerForm()" [disabled]="savingCustomer" style="background:#f7f7f7">Cancelar</button>
        </div>
      </div>

      <p *ngIf="customerFormError" style="color:#b3261e;margin:0">{{ customerFormError }}</p>
      <p *ngIf="customerError" style="color:#b3261e;margin:0">{{ customerError }}</p>
      <table *ngIf="!customerError" style="width:100%;border-collapse:collapse;font-size:13px">
        <thead>
          <tr>
            <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Cliente</th>
            <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">DNI</th>
            <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Telefono</th>
            <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Estado</th>
            <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Cuenta corriente</th>
            <th style="text-align:left;border-bottom:1px solid #eee;padding:6px">Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let c of pagedCustomers()">
            <td style="border-bottom:1px solid #f3f3f3;padding:6px">{{ c.fullName }}</td>
            <td style="border-bottom:1px solid #f3f3f3;padding:6px">{{ c.dni || '-' }}</td>
            <td style="border-bottom:1px solid #f3f3f3;padding:6px">{{ c.phone || '-' }}</td>
            <td style="border-bottom:1px solid #f3f3f3;padding:6px">
              <span style="padding:2px 7px;border-radius:999px;font-size:11px;font-weight:700" [style.background]="customerStatusColor(c.effectiveStatus || c.status)">{{ customerStatusLabel(c.effectiveStatus || c.status) }}</span>
            </td>
            <td style="border-bottom:1px solid #f3f3f3;padding:6px">{{ c.allowsCredit ? 'Sí' : 'No' }} · Uso {{ c.creditUsedPct || 0 }}%</td>
            <td style="border-bottom:1px solid #f3f3f3;padding:6px;display:flex;gap:6px;flex-wrap:wrap">
              <button (click)="startEditCustomer(c)" [disabled]="savingCustomer">Editar</button>
              <button (click)="toggleCustomerStatus(c)" [disabled]="savingCustomer">{{ c.status === 'Inactive' ? 'Reactivar' : 'Desactivar' }}</button>
            </td>
          </tr>
        </tbody>
      </table>
      <div *ngIf="!loadingCustomers && !customerError && filteredCustomersRows().length > 0" style="display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap;color:#555;font-size:12px">
        <span>Mostrando {{ pageRangeLabel(filteredCustomersRows().length, customerPage) }}</span>
        <div style="display:flex;gap:6px;align-items:center">
          <button (click)="prevPage()" [disabled]="customerPage <= 1">Anterior</button>
          <span>Página {{ customerPage }}/{{ totalPages(filteredCustomersRows().length) }}</span>
          <button (click)="nextPage(filteredCustomersRows().length)" [disabled]="customerPage >= totalPages(filteredCustomersRows().length)">Siguiente</button>
        </div>
      </div>
      <p *ngIf="!loadingCustomers && filteredCustomersRows().length === 0" style="margin:0;color:#555">No hay clientes para el filtro aplicado.</p>
    </section>
  `
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
    if (err?.status === 401) return 'Sesión vencida o no autorizada. Volvé a iniciar sesión.';
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
