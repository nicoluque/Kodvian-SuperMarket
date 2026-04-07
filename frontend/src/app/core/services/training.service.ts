import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface TrainingRoleSummary {
  role: string;
  checklistCount: number;
  totalItems: number;
  latestRunId?: number;
  latestRunStatus?: string;
  latestRunAt?: string;
  latestCompletedAt?: string;
}

@Injectable({ providedIn: 'root' })
export class TrainingService {
  private readonly base = '/api/v1/training';
  private readonly demoBase = '/api/v1/admin/demo';

  constructor(private readonly http: HttpClient) {}

  getRoleSummaries(): Promise<TrainingRoleSummary[]> {
    return firstValueFrom(this.http.get<TrainingRoleSummary[]>(`${this.base}/checklists`));
  }

  getRoleDetail(role: string): Promise<any> {
    return firstValueFrom(this.http.get<any>(`${this.base}/checklists/${encodeURIComponent(role)}`));
  }

  startRun(payload: { role?: string; checklistId?: number }): Promise<any> {
    return firstValueFrom(this.http.post<any>(`${this.base}/runs/start`, payload));
  }

  completeItem(runId: number, itemId: number): Promise<any> {
    return firstValueFrom(this.http.post<any>(`${this.base}/runs/${runId}/items/${itemId}/complete`, {}));
  }

  history(role?: string): Promise<any[]> {
    const query = role ? `?role=${encodeURIComponent(role)}` : '';
    return firstValueFrom(this.http.get<any[]>(`${this.base}/runs/history${query}`));
  }

  resetTraining(): Promise<any> {
    return firstValueFrom(this.http.post<any>(`${this.demoBase}/reset-training`, {}));
  }
}
