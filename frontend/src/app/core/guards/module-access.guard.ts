import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { OperatingModeService, OperatingModuleKey } from '../services/operating-mode.service';

export const moduleAccessGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const operatingMode = inject(OperatingModeService);

  const moduleKey = route.data?.['module'] as OperatingModuleKey | undefined;
  if (!moduleKey) return true;

  if (operatingMode.hasModule(moduleKey)) return true;

  if (state.url.startsWith('/bo/')) {
    return router.createUrlTree(['/bo/dashboard-gerencial']);
  }

  return router.createUrlTree(['/inicio']);
};
