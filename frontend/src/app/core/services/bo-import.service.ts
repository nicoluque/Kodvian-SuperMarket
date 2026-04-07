import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class BoImportService {
  private readonly base = '/api/v1/import';

  constructor(private readonly http: HttpClient) {}

  async downloadTemplate(type: 'products' | 'customers' | 'suppliers' | 'prices'): Promise<void> {
    const response = await firstValueFrom(this.http.get(`${this.base}/templates/${type}`, {
      observe: 'response',
      responseType: 'blob'
    }));
    this.downloadBlob(response, `${type}-template.xlsx`);
  }

  async downloadStockOpeningTemplate(): Promise<void> {
    const response = await firstValueFrom(this.http.get(`${this.base}/templates/stock-opening`, {
      observe: 'response',
      responseType: 'blob'
    }));
    this.downloadBlob(response, 'stock-opening-template.xlsx');
  }

  async downloadCatalogStockTemplate(): Promise<void> {
    const response = await firstValueFrom(this.http.get(`${this.base}/templates/catalog-stock`, {
      observe: 'response',
      responseType: 'blob'
    }));
    this.downloadBlob(response, 'catalog-stock-template.xlsx');
  }

  async downloadStockAdjustmentsTemplate(): Promise<void> {
    const response = await firstValueFrom(this.http.get(`${this.base}/templates/stock-adjustments`, {
      observe: 'response',
      responseType: 'blob'
    }));
    this.downloadBlob(response, 'stock-adjustments-template.xlsx');
  }

  preview(type: 'products' | 'customers' | 'suppliers' | 'prices', file: File, upsert: boolean): Promise<any> {
    const form = new FormData();
    form.append('file', file);
    form.append('upsert', String(upsert));
    return firstValueFrom(this.http.post(`${this.base}/${type}/preview`, form));
  }

  commit(type: 'products' | 'customers' | 'suppliers' | 'prices', file: File, upsert: boolean): Promise<any> {
    const form = new FormData();
    form.append('file', file);
    form.append('upsert', String(upsert));
    return firstValueFrom(this.http.post(`${this.base}/${type}/commit`, form));
  }

  stockOpeningPreview(file: File): Promise<any> {
    const form = new FormData();
    form.append('file', file);
    return firstValueFrom(this.http.post(`${this.base}/stock-opening/preview`, form));
  }

  stockOpeningCommit(sessionId: number, explicitConfirmation: boolean): Promise<any> {
    return firstValueFrom(this.http.post(`${this.base}/stock-opening/commit`, { sessionId, explicitConfirmation }));
  }

  catalogStockPreview(file: File, upsert: boolean): Promise<any> {
    const form = new FormData();
    form.append('file', file);
    form.append('upsert', String(upsert));
    return firstValueFrom(this.http.post(`${this.base}/catalog-stock/preview`, form));
  }

  catalogStockCommit(file: File, upsert: boolean): Promise<any> {
    const form = new FormData();
    form.append('file', file);
    form.append('upsert', String(upsert));
    return firstValueFrom(this.http.post(`${this.base}/catalog-stock/commit`, form));
  }

  stockAdjustmentsPreview(file: File): Promise<any> {
    const form = new FormData();
    form.append('file', file);
    return firstValueFrom(this.http.post(`${this.base}/stock-adjustments/preview`, form));
  }

  stockAdjustmentsCommit(file: File): Promise<any> {
    const form = new FormData();
    form.append('file', file);
    return firstValueFrom(this.http.post(`${this.base}/stock-adjustments/commit`, form));
  }

  listStockCountSessions(): Promise<any[]> {
    return firstValueFrom(this.http.get<any[]>('/api/v1/stock-count-sessions'));
  }

  getStockCountSession(id: number): Promise<any> {
    return firstValueFrom(this.http.get(`/api/v1/stock-count-sessions/${id}`));
  }

  private downloadBlob(response: HttpResponse<Blob>, fallbackName: string): void {
    const blob = response.body;
    if (!blob) return;

    const disposition = response.headers.get('content-disposition') ?? '';
    const filenameMatch = disposition.match(/filename="?([^";]+)"?/i);
    const filename = filenameMatch?.[1] || fallbackName;

    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    a.remove();
    window.URL.revokeObjectURL(url);
  }
}
