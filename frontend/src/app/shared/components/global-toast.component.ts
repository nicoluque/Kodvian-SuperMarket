import { CommonModule } from '@angular/common';
import { Component, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';
import { NotificationMessage, NotificationsService } from '../../core/services/notifications.service';

interface ToastItem extends NotificationMessage {
  id: number;
}

@Component({
  standalone: true,
  selector: 'app-global-toast',
  imports: [CommonModule],
  template: `
    <div class="toast-wrap">
      <article *ngFor="let t of toasts" class="toast" [class.error]="t.level === 'error'" [class.warning]="t.level === 'warning'">
        <p>{{ t.text }}</p>
        <small *ngIf="t.traceId">traceId: {{ t.traceId }}</small>
      </article>
    </div>
  `,
  styles: [
    `.toast-wrap{position:fixed;right:16px;bottom:16px;z-index:200;display:flex;flex-direction:column;gap:8px;max-width:420px}`,
    `.toast{background:#e8f7ee;border:1px solid #8ac9a4;border-radius:8px;padding:10px 12px;font-family:Arial,sans-serif;box-shadow:0 4px 14px rgba(0,0,0,.12)}`,
    `.toast.warning{background:#fff6df;border-color:#e0b45a}`,
    `.toast.error{background:#ffe7e7;border-color:#d37a7a}`,
    `.toast p{margin:0}`,
    `.toast small{display:block;margin-top:4px;color:#5f5f5f}`
  ]
})
export class GlobalToastComponent implements OnDestroy {
  toasts: ToastItem[] = [];
  private seq = 0;
  private readonly sub: Subscription;

  constructor(notifications: NotificationsService) {
    this.sub = notifications.messages$.subscribe((msg) => {
      const id = ++this.seq;
      this.toasts = [...this.toasts, { ...msg, id }];
      setTimeout(() => {
        this.toasts = this.toasts.filter(t => t.id !== id);
      }, 4500);
    });
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }
}
