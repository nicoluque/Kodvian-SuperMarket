import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface DialogRequest {
  type?: 'confirm' | 'prompt';
  title: string;
  message: string;
  yesLabel?: string;
  noLabel?: string;
  inputLabel?: string;
  inputPlaceholder?: string;
  inputInitialValue?: string;
  inputRequired?: boolean;
  inputType?: 'text' | 'password';
  inputMode?: 'text' | 'numeric' | 'decimal' | 'email' | 'tel' | 'search' | 'url';
  inputMinLength?: number;
  inputMaxLength?: number;
  inputPattern?: string;
  inputDigitsOnly?: boolean;
  inputErrorMessage?: string;
}

interface PendingDialog {
  request: DialogRequest;
  resolve: (answer: boolean | string | null) => void;
}

@Injectable({ providedIn: 'root' })
export class DialogService {
  private readonly dialogSubject = new BehaviorSubject<DialogRequest | null>(null);
  readonly dialog$ = this.dialogSubject.asObservable();

  private readonly queue: PendingDialog[] = [];
  private current: PendingDialog | null = null;

  confirm(request: DialogRequest): Promise<boolean> {
    return new Promise<boolean>(resolve => {
      this.queue.push({
        request: { ...request, type: 'confirm' },
        resolve: (answer: boolean | string | null) => resolve(answer === true)
      });
      this.openNextIfIdle();
    });
  }

  prompt(request: DialogRequest): Promise<string | null> {
    return new Promise<string | null>(resolve => {
      this.queue.push({
        request: { ...request, type: 'prompt' },
        resolve: (answer: boolean | string | null) => {
          if (typeof answer !== 'string') {
            resolve(null);
            return;
          }
          const value = answer.trim();
          resolve(value.length > 0 ? value : null);
        }
      });
      this.openNextIfIdle();
    });
  }

  answer(answer: boolean | string | null): void {
    if (!this.current) return;

    const active = this.current;
    this.current = null;
    this.dialogSubject.next(null);
    active.resolve(answer);
    this.openNextIfIdle();
  }

  private openNextIfIdle(): void {
    if (this.current || this.queue.length === 0) return;

    this.current = this.queue.shift() ?? null;
    this.dialogSubject.next(this.current?.request ?? null);
  }
}
