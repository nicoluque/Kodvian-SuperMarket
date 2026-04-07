import { HttpInterceptorFn } from '@angular/common/http';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.includes('/api/')) return next(req);

  const path = typeof window !== 'undefined' ? window.location.pathname : '';
  if (!path.startsWith('/bo/')) return next(req);

  const jwt = localStorage.getItem('bo_jwt');
  if (!jwt) return next(req);

  return next(
    req.clone({
      setHeaders: {
        'X-Operator-Session': jwt
      }
    })
  );
};
