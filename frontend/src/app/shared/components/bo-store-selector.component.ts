import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BoAdminService } from '../../core/services/bo-admin.service';

@Component({
  standalone: true,
  selector: 'app-bo-store-selector',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="selector" *ngIf="stores.length > 1">
      <label>Local activo:</label>
      <select [ngModel]="activeStoreId" (ngModelChange)="change($event)">
        <option [ngValue]="null">Todos</option>
        <option *ngFor="let s of stores" [ngValue]="s.id">{{ s.name }}</option>
      </select>
    </div>
  `,
  styles:[`.selector{position:sticky;top:0;z-index:40;display:flex;gap:8px;align-items:center;padding:8px 12px;background:#f6f7fb;border-bottom:1px solid #ddd;font-family:Arial,sans-serif}`]
})
export class BoStoreSelectorComponent {
  stores: any[] = [];
  activeStoreId: number | null = null;

  constructor(private readonly admin: BoAdminService) {
    this.activeStoreId = this.admin.getActiveStore();
    void this.load();
  }

  async load(): Promise<void> {
    try {
      this.stores = await this.admin.getMyStores();
      if (this.stores.length === 1 && this.activeStoreId == null) {
        this.activeStoreId = this.stores[0].id;
        this.admin.setActiveStore(this.activeStoreId);
      }
    } catch {
      this.stores = [];
    }
  }

  change(storeId: number | null): void {
    this.activeStoreId = storeId;
    this.admin.setActiveStore(storeId);
    window.location.reload();
  }
}
