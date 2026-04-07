import { Injectable } from '@angular/core';
import { DialogService } from './dialog.service';

interface EnsureIdentityOptions {
  idleSeconds?: number;
  confirmationMessage?: string;
  pinPrompt?: () => Promise<unknown>;
}

@Injectable({ providedIn: 'root' })
export class ActivityService {
  private lastInteractionAt = Date.now();

  constructor(private readonly dialog: DialogService) {
    if (typeof window === 'undefined') return;

    window.addEventListener('click', this.markInteraction, { passive: true });
    window.addEventListener('keydown', this.markInteraction, { passive: true });
    window.addEventListener('touchstart', this.markInteraction, { passive: true });
  }

  markInteraction = (): void => {
    this.lastInteractionAt = Date.now();
  };

  async ensureRecentIdentity(options?: EnsureIdentityOptions): Promise<boolean> {
    const idleThreshold = options?.idleSeconds ?? 60;
    const idleSeconds = (Date.now() - this.lastInteractionAt) / 1000;

    if (idleSeconds <= idleThreshold) return true;

    const confirmationMessage = options?.confirmationMessage ?? 'Pasaron mas de ' + idleThreshold + 's sin interaccion. Sos vos?';
    const confirmed = await this.dialog.confirm({
      title: 'Confirmacion de identidad',
      message: confirmationMessage,
      yesLabel: 'SI',
      noLabel: 'NO'
    });

    if (confirmed) {
      this.markInteraction();
      return true;
    }

    if (!options?.pinPrompt) return false;

    try {
      await options.pinPrompt();
      this.markInteraction();
      return true;
    } catch {
      return false;
    }
  }
}
