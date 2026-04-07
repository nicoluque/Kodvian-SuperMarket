import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class DeviceService {
  private readonly key = 'pos_device_token';

  getDeviceToken(): string | null {
    return localStorage.getItem(this.key);
  }

  setDeviceToken(token: string): void {
    localStorage.setItem(this.key, token);
  }

  clearDeviceToken(): void {
    localStorage.removeItem(this.key);
  }

  hasDeviceToken(): boolean {
    return !!this.getDeviceToken();
  }
}
