import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface DemoStatus {
  exists: boolean;
  tenantId?: number;
  storeId?: number;
  users?: number;
  products?: number;
  sales?: number;
  lastSeedAt?: string;
  lastResetAt?: string;
}

@Injectable({ providedIn: 'root' })
export class BoDemoService {
  private readonly base = '/api/v1/admin/demo';

  constructor(private readonly http: HttpClient) {}

  status(): Promise<DemoStatus> {
    return firstValueFrom(this.http.get<DemoStatus>(`${this.base}/status`));
  }

  seed(): Promise<DemoStatus> {
    return firstValueFrom(this.http.post<DemoStatus>(`${this.base}/seed`, {}));
  }

  reset(): Promise<DemoStatus> {
    return firstValueFrom(this.http.post<DemoStatus>(`${this.base}/reset`, {}));
  }

  resetTraining(): Promise<any> {
    return firstValueFrom(this.http.post(`${this.base}/reset-training`, {}));
  }
}
