import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { OperatingModeService } from '../services/operating-mode.service';

function guardByRouteData(routeData: Record<string, unknown> | undefined) {
  const router = inject(Router);
  const operatingMode = inject(OperatingModeService);

  const requiredDeviceType = (routeData?.['requiredDeviceType'] as string | undefined) ?? '';
  if (!requiredDeviceType) return true;

  const currentDeviceType = operatingMode.getDeviceType();
  if (currentDeviceType === requiredDeviceType) return true;

  const target = requiredDeviceType === 'CashRegister' ? 'caja' : 'tablet';
  const prefill = requiredDeviceType === 'CashRegister' ? 'demo-device-caja' : 'demo-device-tablet';

  return router.createUrlTree(['/pos/setup'], {
    queryParams: {
      reason: 'device_mismatch',
      target,
      prefill
    }
  });
}

export const deviceTypeGuard: CanActivateFn = (route) => {
  return guardByRouteData(route.data as Record<string, unknown> | undefined);
};

export const deviceTypeChildGuard: CanActivateChildFn = (childRoute) => {
  return guardByRouteData(childRoute.parent?.data as Record<string, unknown> | undefined);
};
