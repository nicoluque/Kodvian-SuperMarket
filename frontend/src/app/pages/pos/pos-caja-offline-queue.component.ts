import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { OfflineQueueService, OfflineTicket } from '../../core/services/offline-queue.service';
import { SyncService } from '../../core/services/sync.service';
import { PosModuleNavComponent } from '../../shared/components/pos-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-pos-caja-offline-queue',
  imports: [CommonModule, FormsModule, PosModuleNavComponent],
  template: `
    <main class="wrap">
      <app-pos-module-nav />
      <h1>Cola offline</h1>
      <div class="row">
        <button (click)="reload()">Recargar</button>
        <button (click)="syncNow()">Sincronizar ahora</button>
      </div>

      <div class="card" *ngFor="let t of tickets">
        <div><strong>{{ t.externalTicketId }}</strong> · {{ t.status }}</div>
        <div>{{ t.originalCreatedAt }} · {{ t.operatorAlias }} · Total {{ t.totalCash | number:'1.2-2' }}</div>
        <div *ngIf="t.lastError" class="error">{{ t.lastError }}</div>
        <button (click)="retry(t)" [disabled]="t.status === 'synced'">Reintentar</button>
      </div>
    </main>
  `,
  styles: [
    `.wrap{padding:16px;font-family:Arial,sans-serif;display:flex;flex-direction:column;gap:10px}`,
    `.row{display:flex;gap:8px}`,
    `.card{border:1px solid #ddd;border-radius:8px;padding:12px;display:flex;flex-direction:column;gap:6px}`,
    `.error{color:#b3261e}`,
    `button{padding:8px}`
  ]
})
export class PosCajaOfflineQueueComponent {
  tickets: OfflineTicket[] = [];

  constructor(private readonly queue: OfflineQueueService, private readonly sync: SyncService) {
    void this.reload();
  }

  async reload(): Promise<void> {
    this.tickets = await this.queue.list();
  }

  async retry(ticket: OfflineTicket): Promise<void> {
    await this.queue.retry(ticket.externalTicketId);
    await this.sync.syncNow();
    await this.reload();
  }

  async syncNow(): Promise<void> {
    await this.sync.syncNow();
    await this.reload();
  }
}
