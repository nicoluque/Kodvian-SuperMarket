import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class BoOnboardingService {
  private readonly base = '/api/v1/onboarding';

  constructor(private readonly http: HttpClient) {}

  start(payload: { tenantId?: number; storeId?: number }): Promise<any> {
    return firstValueFrom(this.http.post(`${this.base}/start`, payload));
  }

  current(): Promise<any> {
    return firstValueFrom(this.http.get(`${this.base}/current`));
  }

  setStep(id: number, stepKey: string): Promise<any> {
    return firstValueFrom(this.http.put(`${this.base}/${id}/step`, { stepKey }));
  }

  getSteps(id: number): Promise<any[]> {
    return firstValueFrom(this.http.get<any[]>(`${this.base}/${id}/steps`));
  }

  completeStep(id: number, stepKey: string, dataJson?: string): Promise<any> {
    return firstValueFrom(this.http.post(`${this.base}/${id}/steps/${encodeURIComponent(stepKey)}/complete`, { dataJson }));
  }

  complete(id: number): Promise<any> {
    return firstValueFrom(this.http.post(`${this.base}/${id}/complete`, {}));
  }
}
