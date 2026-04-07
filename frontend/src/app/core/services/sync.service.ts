import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom, Subscription } from 'rxjs';
import { HealthService } from './health.service';
import { OfflineQueueService } from './offline-queue.service';

@Injectable({ providedIn: 'root' })
export class SyncService {
  private running = false;
  private onlineSub?: Subscription;

  constructor(
    private readonly http: HttpClient,
    private readonly health: HealthService,
    private readonly queue: OfflineQueueService
  ) {}

  start(): void {
    if (this.onlineSub) return;
    this.onlineSub = this.health.isOnline$.subscribe((online) => {
      if (online && this.canSyncNow()) {
        void this.syncNow();
      }
    });
  }

  stop(): void {
    this.onlineSub?.unsubscribe();
    this.onlineSub = undefined;
  }

  async syncNow(): Promise<void> {
    if (this.running || !this.health.isOnline || !this.canSyncNow()) return;
    this.running = true;

    try {
      const tickets = await this.queue.list();
      const pending = tickets.filter(t => t.status === 'queued' || t.status === 'failed');

      for (const t of pending) {
        try {
          await firstValueFrom(this.http.post('/api/v1/offline/manual-sales', {
            externalTicketId: t.externalTicketId,
            originalCreatedAt: t.originalCreatedAt,
            customerAlias: t.operatorAlias,
            items: t.items.map(i => ({ code: i.code, quantity: i.quantity, unitPrice: i.unitPrice }))
          }));
          await this.queue.markSynced(t.externalTicketId);
        } catch (err: any) {
          const message = String(err?.error?.message ?? err?.message ?? 'Sync failed');
          if (message.toLowerCase().includes('exist') || message.toLowerCase().includes('already')) {
            await this.queue.markSynced(t.externalTicketId);
          } else {
            await this.queue.markFailed(t.externalTicketId, message);
          }
        }
      }
    } finally {
      this.running = false;
    }
  }

  private canSyncNow(): boolean {
    if (typeof window !== 'undefined') {
      const path = window.location.pathname;
      if (!path.startsWith('/pos/')) return false;
    }

    const deviceToken = localStorage.getItem('pos_device_token');
    const operatorSession = localStorage.getItem('pos_operator_session');
    return !!deviceToken && !!operatorSession;
  }
}
