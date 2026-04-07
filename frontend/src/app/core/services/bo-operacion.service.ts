import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface BoChecklistItem {
  key: string;
  label: string;
  done: boolean;
  updatedAt?: string;
}

@Injectable({ providedIn: 'root' })
export class BoOperacionService {
  private readonly base = '/api/v1/admin/operation';

  constructor(private readonly http: HttpClient) {}

  getSystemStatus(): Promise<any> {
    return firstValueFrom(this.http.get(`${this.base}/system-status`));
  }

  getChecklist(): Promise<BoChecklistItem[]> {
    return firstValueFrom(this.http.get<BoChecklistItem[]>(`${this.base}/checklist`));
  }

  toggleChecklist(key: string, done: boolean): Promise<BoChecklistItem[]> {
    return firstValueFrom(this.http.put<BoChecklistItem[]>(`${this.base}/checklist/${encodeURIComponent(key)}`, { done }));
  }

  getLocalInfo(): Promise<{ localName: string; address: string; phone: string }> {
    return firstValueFrom(this.http.get<{ localName: string; address: string; phone: string }>(`${this.base}/local-info`));
  }

  saveLocalInfo(payload: { localName: string; address: string; phone: string }): Promise<any> {
    return firstValueFrom(this.http.post(`${this.base}/local-info`, payload));
  }

  getUsers(): Promise<any[]> {
    return firstValueFrom(this.http.get<any[]>(`${this.base}/users`));
  }

  createUser(payload: { username: string; password: string; pin?: string; role: string }): Promise<any> {
    return firstValueFrom(this.http.post(`${this.base}/users`, payload));
  }

  getDevices(): Promise<any[]> {
    return firstValueFrom(this.http.get<any[]>(`${this.base}/devices`));
  }

  createDevice(payload: { deviceName: string; deviceType: string; parentCashRegisterDeviceId?: number; usuarioId?: number; ipAddress?: string }): Promise<any> {
    return firstValueFrom(this.http.post(`${this.base}/devices`, payload));
  }

  getPosSettings(): Promise<Record<string, string>> {
    return firstValueFrom(this.http.get<Record<string, string>>(`${this.base}/settings/pos`));
  }

  updatePosSettings(payload: any): Promise<any> {
    return firstValueFrom(this.http.put(`${this.base}/settings/pos`, payload));
  }

  getManualKitUrl(): string {
    return `${this.base}/downloads/manual-kit`;
  }

  getEmergencyCatalogUrl(): string {
    return `${this.base}/downloads/emergency-catalog`;
  }
}
