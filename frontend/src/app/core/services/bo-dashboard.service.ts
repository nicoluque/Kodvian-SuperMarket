import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class BoDashboardService {
  private readonly base = '/api/v1/dashboard';

  constructor(private readonly http: HttpClient) {}

  summary(from: string, to: string): Promise<any> {
    return firstValueFrom(this.http.get(`${this.base}/summary?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`));
  }

  salesSeries(days: number): Promise<any[]> {
    return firstValueFrom(this.http.get<any[]>(`${this.base}/sales-series?days=${days}`));
  }

  paymentMethods(from: string, to: string): Promise<any> {
    return firstValueFrom(this.http.get(`${this.base}/payment-methods?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`));
  }

  topCreditDebtors(top = 10): Promise<any[]> {
    return firstValueFrom(this.http.get<any[]>(`${this.base}/top-credit-debtors?top=${top}`));
  }

  topContainerDebtors(top = 10): Promise<any[]> {
    return firstValueFrom(this.http.get<any[]>(`${this.base}/top-container-debtors?top=${top}`));
  }

  criticalStock(): Promise<any[]> {
    return firstValueFrom(this.http.get<any[]>(`${this.base}/critical-stock`));
  }

  wasteSummary(from: string, to: string): Promise<any> {
    return firstValueFrom(this.http.get(`${this.base}/waste-summary?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`));
  }

  operationsSummary(): Promise<any> {
    return firstValueFrom(this.http.get(`${this.base}/operations-summary`));
  }

  transformationPendingRecalibrations(): Promise<any[]> {
    return firstValueFrom(this.http.get<any[]>('/api/v1/stock/transformations/yield-recalibrations?status=Pending'));
  }
}
