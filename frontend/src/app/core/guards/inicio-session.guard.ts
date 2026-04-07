import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthJwtService } from '../services/auth-jwt.service';

export const inicioSessionGuard: CanActivateFn = () => {
  const auth = inject(AuthJwtService);
  const router = inject(Router);

  if (!auth.isTokenValid()) {
    return router.createUrlTree(['/inicio/login']);
  }

  return true;
};
