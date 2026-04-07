import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class BoAdminService {
  constructor(private readonly http: HttpClient) {}

  getTenants(): Promise<any[]> { return firstValueFrom(this.http.get<any[]>('/api/v1/tenants')); }
  createTenant(payload: any): Promise<any> { return firstValueFrom(this.http.post('/api/v1/tenants', payload)); }
  updateTenant(id: number, payload: any): Promise<any> { return firstValueFrom(this.http.put(`/api/v1/tenants/${id}`, payload)); }

  getStores(tenantId?: number): Promise<any[]> {
    const q = tenantId ? `?tenantId=${tenantId}` : '';
    return firstValueFrom(this.http.get<any[]>(`/api/v1/stores${q}`));
  }
  getMyStores(): Promise<any[]> { return firstValueFrom(this.http.get<any[]>('/api/v1/stores/my')); }
  createStore(payload: any): Promise<any> { return firstValueFrom(this.http.post('/api/v1/stores', payload)); }
  updateStore(id: number, payload: any): Promise<any> { return firstValueFrom(this.http.put(`/api/v1/stores/${id}`, payload)); }

  getStoreSettings(id: number): Promise<any> { return firstValueFrom(this.http.get(`/api/v1/stores/${id}/settings`)); }
  updateStoreSettings(id: number, payload: any): Promise<any> { return firstValueFrom(this.http.put(`/api/v1/stores/${id}/settings`, payload)); }
  getStoreShiftConfig(id: number): Promise<any> { return firstValueFrom(this.http.get(`/api/v1/stores/${id}/shift-config`)); }
  updateStoreShiftConfig(id: number, payload: any): Promise<any> { return firstValueFrom(this.http.put(`/api/v1/stores/${id}/shift-config`, payload)); }

  getStoreUsers(id: number): Promise<any[]> { return firstValueFrom(this.http.get<any[]>(`/api/v1/stores/${id}/users`)); }
  addStoreUser(id: number, payload: any): Promise<any> { return firstValueFrom(this.http.post(`/api/v1/stores/${id}/users`, payload)); }
  removeStoreUser(id: number, usuarioId: number): Promise<any> { return firstValueFrom(this.http.delete(`/api/v1/stores/${id}/users?usuarioId=${usuarioId}`)); }

  setActiveStore(storeId: number | null): void {
    if (storeId == null) localStorage.removeItem('bo_active_store_id');
    else localStorage.setItem('bo_active_store_id', String(storeId));
  }

  getActiveStore(): number | null {
    const raw = localStorage.getItem('bo_active_store_id');
    const parsed = raw ? Number(raw) : NaN;
    return Number.isFinite(parsed) ? parsed : null;
  }
}
