import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { OperatingModeService } from '../services/operating-mode.service';

export const posGuard: CanActivateFn = async () => {
  const router = inject(Router);
  const http = inject(HttpClient);
  const operatingMode = inject(OperatingModeService);
  const token = localStorage.getItem('pos_device_token');
  const guardCheckedToken = sessionStorage.getItem('pos_device_token_guard_checked');

  if (!token) {
    return router.createUrlTree(['/pos/setup'], { queryParams: { reason: 'missing', prefill: 'demo-device-caja' } });
  }

  const validatedToken = localStorage.getItem('pos_device_token_validated');
  if (validatedToken === token && guardCheckedToken === token) {
    return true;
  }

  try {
    const validation = await firstValueFrom(http.get('/api/v1/auth/device/validate', {
      headers: new HttpHeaders({ 'X-Device-Token': token })
    }));
    operatingMode.setFromDeviceValidation(validation);
    localStorage.setItem('pos_device_token_validated', token);
    sessionStorage.setItem('pos_device_token_guard_checked', token);
    return true;
  } catch {
    localStorage.removeItem('pos_device_token');
    localStorage.removeItem('pos_device_token_validated');
    localStorage.removeItem('operating_device_type');
    localStorage.removeItem('pos_operator_session');
    sessionStorage.removeItem('pos_device_token_guard_checked');
    return router.createUrlTree(['/pos/setup'], { queryParams: { reason: 'invalid', prefill: 'demo-device-caja' } });
  }
};
