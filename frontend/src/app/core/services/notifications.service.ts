import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export interface NotificationMessage {
  level: 'success' | 'info' | 'warning' | 'error';
  text: string;
  traceId?: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private readonly stream = new Subject<NotificationMessage>();
  readonly messages$ = this.stream.asObservable();

  private lastNotification: { text: string; timestamp: number } | null = null;
  private readonly cooldownMs = 3000;

  push(level: NotificationMessage['level'], text: string, traceId?: string): void {
    const now = Date.now();
    if (
      this.lastNotification &&
      this.lastNotification.text === text &&
      now - this.lastNotification.timestamp < this.cooldownMs
    ) {
      return;
    }
    this.lastNotification = { text, timestamp: now };
    this.stream.next({ level, text, traceId });
  }
}
