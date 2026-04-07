import { Injectable } from '@angular/core';

export type OfflineTicketStatus = 'queued' | 'failed' | 'synced';

export interface OfflineTicketItem {
  code: string;
  name: string;
  quantity: number;
  unitPrice: number;
}

export interface OfflineTicket {
  id?: number;
  externalTicketId: string;
  originalCreatedAt: string;
  operatorAlias: string;
  items: OfflineTicketItem[];
  totalCash: number;
  status: OfflineTicketStatus;
  lastError?: string;
}

@Injectable({ providedIn: 'root' })
export class OfflineQueueService {
  private readonly dbName = 'kodvian-pos-offline';
  private readonly store = 'offlineTickets';

  async createTicket(payload: {
    operatorAlias: string;
    items: OfflineTicketItem[];
    totalCash: number;
  }): Promise<OfflineTicket> {
    const externalTicketId = this.buildExternalTicketId();
    const ticket: OfflineTicket = {
      externalTicketId,
      originalCreatedAt: new Date().toISOString(),
      operatorAlias: payload.operatorAlias,
      items: payload.items,
      totalCash: payload.totalCash,
      status: 'queued'
    };

    await this.put(ticket);
    return ticket;
  }

  async list(status?: OfflineTicketStatus): Promise<OfflineTicket[]> {
    const db = await this.open();
    const tx = db.transaction(this.store, 'readonly');
    const store = tx.objectStore(this.store);

    const all = await this.request<OfflineTicket[]>(store.getAll());
    await this.waitTx(tx);

    const sorted = all.sort((a, b) => b.originalCreatedAt.localeCompare(a.originalCreatedAt));
    return status ? sorted.filter(t => t.status === status) : sorted;
  }

  async markSynced(externalTicketId: string): Promise<void> {
    const ticket = await this.getByExternalId(externalTicketId);
    if (!ticket) return;
    ticket.status = 'synced';
    ticket.lastError = undefined;
    await this.put(ticket);
  }

  async markFailed(externalTicketId: string, lastError: string): Promise<void> {
    const ticket = await this.getByExternalId(externalTicketId);
    if (!ticket) return;
    ticket.status = 'failed';
    ticket.lastError = lastError;
    await this.put(ticket);
  }

  async retry(externalTicketId: string): Promise<void> {
    const ticket = await this.getByExternalId(externalTicketId);
    if (!ticket) return;
    ticket.status = 'queued';
    ticket.lastError = undefined;
    await this.put(ticket);
  }

  async getByExternalId(externalTicketId: string): Promise<OfflineTicket | null> {
    const db = await this.open();
    const tx = db.transaction(this.store, 'readonly');
    const store = tx.objectStore(this.store);
    const idx = store.index('externalTicketId');
    const result = await this.request<OfflineTicket | undefined>(idx.get(externalTicketId));
    await this.waitTx(tx);
    return result ?? null;
  }

  private buildExternalTicketId(): string {
    const rawDevice = localStorage.getItem('pos_device_token') ?? 'device';
    const device = rawDevice.replace(/[^a-zA-Z0-9]/g, '').slice(0, 8) || 'device';
    const now = new Date();
    const yyyymmdd = `${now.getFullYear()}${String(now.getMonth() + 1).padStart(2, '0')}${String(now.getDate()).padStart(2, '0')}`;
    const counterKey = `offline_counter_${yyyymmdd}`;
    const counter = Number(localStorage.getItem(counterKey) ?? '0') + 1;
    localStorage.setItem(counterKey, String(counter));
    return `OFF-${device}-${yyyymmdd}-${String(counter).padStart(4, '0')}`;
  }

  private async put(ticket: OfflineTicket): Promise<void> {
    const db = await this.open();
    const tx = db.transaction(this.store, 'readwrite');
    tx.objectStore(this.store).put(ticket);
    await this.waitTx(tx);
  }

  private open(): Promise<IDBDatabase> {
    return new Promise((resolve, reject) => {
      const req = indexedDB.open(this.dbName, 1);
      req.onupgradeneeded = () => {
        const db = req.result;
        if (!db.objectStoreNames.contains(this.store)) {
          const store = db.createObjectStore(this.store, { keyPath: 'id', autoIncrement: true });
          store.createIndex('externalTicketId', 'externalTicketId', { unique: true });
          store.createIndex('status', 'status', { unique: false });
        }
      };
      req.onsuccess = () => resolve(req.result);
      req.onerror = () => reject(req.error);
    });
  }

  private request<T>(req: IDBRequest<T>): Promise<T> {
    return new Promise((resolve, reject) => {
      req.onsuccess = () => resolve(req.result);
      req.onerror = () => reject(req.error);
    });
  }

  private waitTx(tx: IDBTransaction): Promise<void> {
    return new Promise((resolve, reject) => {
      tx.oncomplete = () => resolve();
      tx.onerror = () => reject(tx.error);
      tx.onabort = () => reject(tx.error);
    });
  }
}
