import { HttpInterceptorFn } from '@angular/common/http';

export const deviceTokenInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.includes('/api/')) return next(req);

  const path = typeof window !== 'undefined' ? window.location.pathname : '';
  if (!path.startsWith('/pos/') && !path.startsWith('/bo/')) return next(req);

  const token = localStorage.getItem('pos_device_token');
  if (!token) return next(req);

  return next(
    req.clone({
      setHeaders: {
        'X-Device-Token': token
      }
    })
  );
};
