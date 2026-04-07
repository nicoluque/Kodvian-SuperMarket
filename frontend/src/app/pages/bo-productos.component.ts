import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { BoModuleNavComponent } from '../shared/components/bo-module-nav.component';

interface PendingProduct {
  id: number;
  name: string;
  barcode?: string;
  quickCode?: string;
  saleType?: string;
  stockControl?: boolean;
  defaultPrice?: number;
  defaultPricePerKg?: number;
  createdAt?: string;
}

@Component({
  standalone: true,
  selector: 'app-bo-productos',
  imports: [CommonModule, FormsModule, BoModuleNavComponent],
  template: `
    <main class="bo-productos">
      <app-bo-module-nav />

      <header class="hero">
        <h1>Productos pendientes</h1>
        <p>Alta rapida desde Totem/POS para completar por administracion.</p>
      </header>

      <section class="card">
        <div class="actions">
          <button class="btn" (click)="load()" [disabled]="loading">Recargar</button>
        </div>

        <p class="msg ok" *ngIf="message">{{ message }}</p>
        <p class="msg err" *ngIf="error">{{ error }}</p>

        <table *ngIf="products.length > 0">
          <thead>
            <tr>
              <th>ID</th>
              <th>Nombre</th>
              <th>Codigo</th>
              <th>Precio</th>
              <th>Creado</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let p of products">
              <td>{{ p.id }}</td>
              <td>{{ p.name }}</td>
              <td>{{ p.barcode || p.quickCode || '-' }}</td>
              <td>{{ p.defaultPrice || 0 | number:'1.2-2' }}</td>
              <td>{{ p.createdAt ? (p.createdAt | date:'short') : '-' }}</td>
              <td><button class="btn" [disabled]="loading" (click)="startEdit(p)">Completar</button></td>
            </tr>
          </tbody>
        </table>

        <p *ngIf="!loading && products.length === 0">No hay productos pendientes.</p>
      </section>

      <div class="modal-overlay" *ngIf="editing">
        <div class="modal">
          <h3>Completar producto #{{ editing.id }}</h3>
          <label>Nombre</label>
          <input [(ngModel)]="form.name" />
          <label>Codigo de barras</label>
          <input [(ngModel)]="form.barcode" />
          <label>Codigo rapido</label>
          <input [(ngModel)]="form.quickCode" />
          <label>Precio</label>
          <input type="number" [(ngModel)]="form.defaultPrice" />
          <div class="modal-actions">
            <button class="btn" [disabled]="loading" (click)="editing = null">Cancelar</button>
            <button class="btn primary" [disabled]="loading" (click)="save(false)">Guardar</button>
            <button class="btn primary" [disabled]="loading" (click)="save(true)">Guardar y activar</button>
          </div>
        </div>
      </div>
    </main>
  `,
  styles: [`
    .bo-productos { padding: 16px; }
    .hero h1 { margin: 8px 0 4px; color: #1B4D3E; }
    .hero p { margin: 0 0 12px; color: #4f6f65; }
    .card { background: #fff; border: 1px solid #dbe7e1; border-radius: 14px; padding: 12px; }
    .actions { display: flex; justify-content: flex-end; margin-bottom: 8px; }
    .btn { border: 1px solid #c9ddd3; border-radius: 8px; background: #f2f8f5; color: #1B4D3E; padding: 8px 12px; cursor: pointer; }
    .btn.primary { background: #1B4D3E; color: #fff; border-color: #1B4D3E; }
    .msg { margin: 8px 0; }
    .msg.ok { color: #17663f; }
    .msg.err { color: #8f1d22; }
    table { width: 100%; border-collapse: collapse; }
    th, td { text-align: left; padding: 8px; border-bottom: 1px solid #eef4f1; }
    th { color: #4f6f65; font-weight: 600; }
    .modal-overlay { position: fixed; inset: 0; background: rgba(12, 20, 17, 0.45); display: grid; place-items: center; z-index: 1000; }
    .modal { width: min(540px, 92vw); background: #fff; border-radius: 12px; padding: 16px; display: grid; gap: 8px; }
    .modal input { border: 1px solid #d3e1da; border-radius: 8px; padding: 8px; }
    .modal-actions { display: flex; gap: 8px; justify-content: flex-end; margin-top: 8px; }
  `]
})
export class BoProductosComponent {
  loading = false;
  message = '';
  error = '';
  products: PendingProduct[] = [];
  editing: PendingProduct | null = null;
  form = {
    name: '',
    barcode: '',
    quickCode: '',
    defaultPrice: 0
  };

  constructor(private readonly http: HttpClient) {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading = true;
    this.error = '';
    this.message = '';
    try {
      this.products = await firstValueFrom(this.http.get<PendingProduct[]>('/api/v1/products/pending'));
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudieron cargar productos pendientes';
    } finally {
      this.loading = false;
    }
  }

  startEdit(p: PendingProduct): void {
    this.editing = p;
    this.form = {
      name: p.name || '',
      barcode: p.barcode || '',
      quickCode: p.quickCode || '',
      defaultPrice: Number(p.defaultPrice || 0)
    };
  }

  async save(activate: boolean): Promise<void> {
    if (!this.editing) return;
    this.loading = true;
    this.error = '';
    this.message = '';
    try {
      await firstValueFrom(this.http.put(`/api/v1/products/${this.editing.id}`, {
        name: this.form.name.trim(),
        barcode: this.form.barcode.trim() || null,
        quickCode: this.form.quickCode.trim() || null,
        defaultPrice: Number(this.form.defaultPrice || 0),
        catalogStatus: activate ? 'Active' : 'Pending'
      }));

      this.message = activate
        ? `Producto #${this.editing.id} activado` : `Producto #${this.editing.id} actualizado`;
      this.editing = null;
      await this.load();
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo guardar producto';
    } finally {
      this.loading = false;
    }
  }
}
