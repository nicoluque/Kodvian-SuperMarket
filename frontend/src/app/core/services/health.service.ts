import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { firstValueFrom } from 'rxjs';
import { ApiConfigService } from './api.config';

@Injectable({ providedIn: 'root' })
export class HealthService {
  private readonly onlineSubject = new BehaviorSubject<boolean>(true);
  private failures = 0;
  private timer?: ReturnType<typeof setInterval>;

  readonly isOnline$ = this.onlineSubject.asObservable();

  constructor(private readonly http: HttpClient, private readonly api: ApiConfigService) {}

  get isOnline(): boolean {
    return this.onlineSubject.value;
  }

  startPolling(intervalMs = 4000): void {
    if (this.timer) return;

    void this.check();
    this.timer = setInterval(() => {
      void this.check();
    }, intervalMs);
  }

  stopPolling(): void {
    if (!this.timer) return;
    clearInterval(this.timer);
    this.timer = undefined;
  }

  private async check(): Promise<void> {
    try {
      await firstValueFrom(this.http.get(`${this.api.apiBase}/health`));
      this.failures = 0;
      if (!this.onlineSubject.value) {
        this.onlineSubject.next(true);
      }
    } catch {
      this.failures += 1;
      if (this.failures >= 2 && this.onlineSubject.value) {
        this.onlineSubject.next(false);
      }
    }
  }

  pingPos(): Promise<unknown> {
    return firstValueFrom(this.http.get(`${this.api.posBase}/health`));
  }

  pingBackoffice(): Promise<unknown> {
    return firstValueFrom(this.http.get(`${this.api.boBase}/health`));
  }
}
