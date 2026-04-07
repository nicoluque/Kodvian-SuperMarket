import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface PurchaseItemPayload {
  productId: number;
  quantity: number;
  unitCost: number;
  expiryDate?: string | null;
  damagedForClaimQty: number;
  discardQty: number;
  updateSalePrice: boolean;
  newSalePrice?: number | null;
  newPricePerKg?: number | null;
}

export interface PurchasePayload {
  supplierId?: number | null;
  docType: string;
  docNumber?: string | null;
  purchaseDate: string;
  items: PurchaseItemPayload[];
}

@Injectable({ providedIn: 'root' })
export class BoPurchasesService {
  private readonly base = '/api/v1/purchases';

  constructor(private readonly http: HttpClient) {}

  list(status?: string): Promise<any[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return firstValueFrom(this.http.get<any[]>(this.base, { params }));
  }

  getById(id: number): Promise<any> {
    return firstValueFrom(this.http.get(`${this.base}/${id}`));
  }

  create(payload: PurchasePayload): Promise<any> {
    return firstValueFrom(this.http.post(this.base, payload));
  }

  update(id: number, payload: PurchasePayload): Promise<any> {
    return firstValueFrom(this.http.put(`${this.base}/${id}`, payload));
  }

  confirm(id: number): Promise<any> {
    return firstValueFrom(this.http.post(`${this.base}/${id}/confirm`, {}));
  }

  cancel(id: number, reason: string): Promise<any> {
    return firstValueFrom(this.http.post(`${this.base}/${id}/cancel`, { reason }));
  }

  suppliers(): Promise<any[]> {
    return firstValueFrom(this.http.get<any[]>('/api/v1/suppliers'));
  }

  products(): Promise<any[]> {
    return firstValueFrom(this.http.get<any[]>('/api/v1/products'));
  }

  async findProductByCode(code: string): Promise<any | null> {
    const normalized = (code || '').trim();
    if (!normalized) return null;

    try {
      return await firstValueFrom(this.http.get(`/api/v1/products/barcode/${encodeURIComponent(normalized)}`));
    } catch {
      try {
        return await firstValueFrom(this.http.get(`/api/v1/products/quickcode/${encodeURIComponent(normalized)}`));
      } catch {
        return null;
      }
    }
  }
}
