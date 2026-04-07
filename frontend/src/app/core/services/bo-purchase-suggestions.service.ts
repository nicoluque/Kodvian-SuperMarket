import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class BoPurchaseSuggestionsService {
  private readonly base = '/api/v1/purchase-suggestions';

  constructor(private readonly http: HttpClient) {}

  generate(payload: { supplierId?: number; criticalOnly?: boolean; daysWindow?: number; targetCoverageDays?: number }): Promise<any> {
    return firstValueFrom(this.http.post(`${this.base}/generate`, payload));
  }

  list(): Promise<any[]> {
    return firstValueFrom(this.http.get<any[]>(this.base));
  }

  getById(id: number): Promise<any> {
    return firstValueFrom(this.http.get(`${this.base}/${id}`));
  }

  updateLine(suggestionId: number, lineId: number, payload: { status?: string; acceptedQty?: number; notes?: string }): Promise<any> {
    return firstValueFrom(this.http.put(`${this.base}/${suggestionId}/lines/${lineId}`, payload));
  }

  convertToPurchase(id: number): Promise<any> {
    return firstValueFrom(this.http.post(`${this.base}/${id}/convert-to-purchase`, {}));
  }

  suppliers(): Promise<any[]> {
    return firstValueFrom(this.http.get<any[]>(`${this.base}/suppliers`));
  }
}
