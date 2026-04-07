import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { BoAdminService } from '../core/services/bo-admin.service';
import { BoModuleNavComponent } from '../shared/components/bo-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-bo-admin-local-usuarios',
  imports: [CommonModule, FormsModule, BoModuleNavComponent],
  template: `
    <main class="wrap">
      <app-bo-module-nav />
      <h1>Usuarios del Local #{{ storeId }}</h1>
      <div class="row">
        <input type="number" placeholder="UsuarioId" [(ngModel)]="draft.usuarioId" />
        <select [(ngModel)]="draft.role"><option>Operator</option><option>Supervisor</option><option>Admin</option></select>
        <label><input type="checkbox" [(ngModel)]="draft.isActive" /> Activo</label>
        <button (click)="add()">Asignar</button>
      </div>

      <div class="card" *ngFor="let u of users">
        <span>#{{ u.usuarioId }} {{ u.username }} ({{ u.role }})</span>
        <button (click)="remove(u.usuarioId)">Quitar</button>
      </div>
      <p *ngIf="error" class="error">{{ error }}</p>
    </main>
  `,
  styles:[`.wrap{padding:20px;font-family:Arial,sans-serif;display:flex;flex-direction:column;gap:10px}.row{display:flex;gap:8px;flex-wrap:wrap}.card{border:1px solid #ddd;padding:8px;border-radius:6px;display:flex;justify-content:space-between}.error{color:#b3261e}`]
})
export class BoAdminLocalUsuariosComponent {
  storeId = 0;
  users: any[] = [];
  draft: any = { usuarioId: 0, role: 'Operator', isActive: true };
  error = '';

  constructor(private readonly route: ActivatedRoute, private readonly api: BoAdminService) {
    this.storeId = Number(this.route.snapshot.paramMap.get('id'));
    void this.load();
  }

  async load(): Promise<void> { this.users = await this.api.getStoreUsers(this.storeId); }
  async add(): Promise<void> { try { await this.api.addStoreUser(this.storeId, this.draft); await this.load(); } catch (e:any){ this.error = e?.error?.message ?? 'No se pudo asignar'; } }
  async remove(usuarioId: number): Promise<void> { try { await this.api.removeStoreUser(this.storeId, usuarioId); await this.load(); } catch (e:any){ this.error = e?.error?.message ?? 'No se pudo quitar'; } }
}
