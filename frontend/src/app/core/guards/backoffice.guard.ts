import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthJwtService } from '../services/auth-jwt.service';

export const backofficeGuard: CanActivateFn = (route) => {
  const auth = inject(AuthJwtService);
  const router = inject(Router);

  if (!auth.isTokenValid()) {
    return router.createUrlTree(['/inicio/login']);
  }

  const requiredRoles = (route.data?.['roles'] as string[] | undefined) ?? [];
  if (requiredRoles.length > 0 && !auth.hasAnyRole(requiredRoles)) {
    return router.createUrlTree(['/inicio/login'], { queryParams: { reason: 'forbidden-role' } });
  }

  return true;
};
