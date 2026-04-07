import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class BoTotemShiftsService {
  private readonly base = '/api/v1/totem-shifts';

  constructor(private readonly http: HttpClient) {}

  listTransitions(params?: { storeId?: number; from?: string; to?: string }): Promise<any[]> {
    let httpParams = new HttpParams();
    if (params?.storeId != null) httpParams = httpParams.set('storeId', String(params.storeId));
    if (params?.from) httpParams = httpParams.set('from', params.from);
    if (params?.to) httpParams = httpParams.set('to', params.to);
    return firstValueFrom(this.http.get<any[]>(`${this.base}/transitions`, { params: httpParams }));
  }

  reassignSale(saleId: number, payload: { shiftBucket: string; reason: string }): Promise<any> {
    return firstValueFrom(this.http.post(`${this.base}/sales/${saleId}/assign`, payload));
  }

  getHistory(saleId: number): Promise<any[]> {
    return firstValueFrom(this.http.get<any[]>(`${this.base}/sales/${saleId}/history`));
  }
}
