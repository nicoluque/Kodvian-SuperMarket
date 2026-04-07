import { NgIf } from '@angular/common';
import { Component, OnDestroy, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { DialogRequest, DialogService } from '../../core/services/dialog.service';

@Component({
  standalone: true,
  selector: 'app-global-dialog',
  imports: [NgIf, FormsModule],
  template: `
    <div class="dialog-overlay" *ngIf="dialog as dialog" (click)="cancel()">
      <section class="dialog-card" role="dialog" aria-modal="true" (click)="$event.stopPropagation()">
        <h3>{{ dialog.title }}</h3>
        <p>{{ dialog.message }}</p>

        <div class="dialog-input" *ngIf="dialog.type === 'prompt'">
          <label>{{ dialog.inputLabel || 'Motivo' }}</label>
          <input
            [type]="dialog.inputType || 'text'"
            [(ngModel)]="promptValue"
            [placeholder]="dialog.inputPlaceholder || ''"
            [attr.minlength]="dialog.inputMinLength || null"
            [attr.maxlength]="dialog.inputMaxLength || 220"
            [attr.inputmode]="dialog.inputMode || null"
            [attr.pattern]="dialog.inputPattern || null"
            [attr.autocomplete]="dialog.inputType === 'password' ? 'current-password' : 'off'"
            [class.pin-mask]="dialog.inputType === 'password' && dialog.inputDigitsOnly"
            (input)="onPromptInput(dialog)"
            (paste)="onPromptPaste($event, dialog)"
            (keydown)="onPromptKeydown($event, dialog)"
            (keydown.enter)="submitPrompt(dialog)"
          />
          <small *ngIf="promptError">{{ promptError }}</small>
        </div>

        <div class="dialog-actions">
          <button class="btn-no" (click)="cancel()">{{ dialog.noLabel || 'Cancelar' }}</button>
          <button class="btn-yes" *ngIf="dialog.type !== 'prompt'" (click)="confirm()">{{ dialog.yesLabel || 'Confirmar' }}</button>
          <button class="btn-yes" *ngIf="dialog.type === 'prompt'" (click)="submitPrompt(dialog)">{{ dialog.yesLabel || 'Confirmar' }}</button>
        </div>
      </section>
    </div>
  `,
  styles: [
    `.dialog-overlay{position:fixed;inset:0;z-index:240;background:rgba(16,24,21,.48);display:flex;align-items:center;justify-content:center;padding:1rem}`,
    `.dialog-card{width:min(460px,96vw);background:#fff;border:1px solid #d9ebe4;border-radius:14px;padding:1rem;box-shadow:0 14px 34px rgba(0,0,0,.22)}`,
    `.dialog-card h3{margin:0 0 .35rem 0;color:#1b4d3e;font-size:1.08rem}`,
    `.dialog-card p{margin:0;color:#355b4f;font-size:.92rem;line-height:1.45}`,
    `.dialog-input{margin-top:.8rem;display:flex;flex-direction:column;gap:.35rem}`,
    `.dialog-input label{font-size:.82rem;font-weight:700;color:#355b4f}`,
    `.dialog-input input{min-height:44px;border:1px solid #cfd8d3;border-radius:10px;padding:.55rem .65rem;font-size:.9rem;outline:none}`,
    `.dialog-input input.pin-mask{letter-spacing:.28em;font-weight:700}`,
    `.dialog-input input:focus{border-color:#1b8f5e;box-shadow:0 0 0 3px rgba(27,143,94,.12)}`,
    `.dialog-input small{color:#9a2f2f;font-size:.78rem}`,
    `.dialog-actions{margin-top:.9rem;display:flex;gap:.75rem}`,
    `.dialog-actions button{flex:1;min-height:44px;border-radius:10px;font-size:.86rem;font-weight:700;letter-spacing:.01em;cursor:pointer}`,
    `.btn-no{background:#fff;border:1px solid #cfd8d3;color:#355b4f}`,
    `.btn-no:hover{background:#f3f7f5;border-color:#b9c9c2}`,
    `.btn-yes{background:linear-gradient(135deg,#20a36b 0%,#1b8f5e 100%);border:1px solid #1b8f5e;color:#fff;box-shadow:0 4px 12px rgba(27,143,94,.2)}`,
    `.btn-yes:hover{transform:translateY(-1px);box-shadow:0 6px 16px rgba(27,143,94,.28)}`,
    `@media (max-width:640px){.dialog-actions{flex-direction:column}}`
  ]
})
export class GlobalDialogComponent implements OnDestroy {
  private readonly dialogService = inject(DialogService);
  private readonly sub: Subscription;
  dialog: DialogRequest | null = null;
  promptValue = '';
  promptError = '';

  constructor() {
    this.sub = this.dialogService.dialog$.subscribe(dialog => {
      this.dialog = dialog;
      this.promptValue = dialog?.type === 'prompt' ? (dialog.inputInitialValue ?? '') : '';
      this.promptError = '';
    });
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }

  cancel(): void {
    this.dialogService.answer(null);
  }

  confirm(): void {
    this.dialogService.answer(true);
  }

  submitPrompt(dialog: DialogRequest): void {
    const required = dialog.inputRequired !== false;
    const value = this.promptValue.trim();
    if (required && !value) {
      this.promptError = 'Completa este campo para continuar.';
      return;
    }

    if (dialog.inputMinLength && value.length < dialog.inputMinLength) {
      this.promptError = dialog.inputErrorMessage || `Debe tener al menos ${dialog.inputMinLength} caracteres.`;
      return;
    }

    if (dialog.inputMaxLength && value.length > dialog.inputMaxLength) {
      this.promptError = dialog.inputErrorMessage || `Debe tener como maximo ${dialog.inputMaxLength} caracteres.`;
      return;
    }

    if (dialog.inputPattern) {
      try {
        const regex = new RegExp(dialog.inputPattern);
        if (value && !regex.test(value)) {
          this.promptError = dialog.inputErrorMessage || 'El formato ingresado no es valido.';
          return;
        }
      } catch {
      }
    }

    this.dialogService.answer(value);
  }

  onPromptInput(dialog: DialogRequest): void {
    if (!dialog.inputDigitsOnly) return;
    const max = dialog.inputMaxLength || 220;
    this.promptValue = this.promptValue.replace(/\D+/g, '').slice(0, max);
  }

  onPromptPaste(event: ClipboardEvent, dialog: DialogRequest): void {
    if (!dialog.inputDigitsOnly) return;

    event.preventDefault();
    const text = event.clipboardData?.getData('text') ?? '';
    const digits = text.replace(/\D+/g, '');
    const max = dialog.inputMaxLength || 220;
    this.promptValue = digits.slice(0, max);
  }

  onPromptKeydown(event: KeyboardEvent, dialog: DialogRequest): void {
    if (!dialog.inputDigitsOnly) return;

    if (event.ctrlKey || event.metaKey || event.altKey) return;

    const allowedKeys = new Set([
      'Backspace', 'Delete', 'Tab', 'Escape', 'Enter',
      'ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown',
      'Home', 'End'
    ]);
    if (allowedKeys.has(event.key)) return;

    if (!/^\d$/.test(event.key)) {
      event.preventDefault();
    }
  }
}
