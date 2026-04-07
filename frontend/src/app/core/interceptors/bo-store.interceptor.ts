import { HttpInterceptorFn } from '@angular/common/http';

export const boStoreInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.includes('/api/')) return next(req);
  const path = typeof window !== 'undefined' ? window.location.pathname : '';
  if (!path.startsWith('/bo/')) return next(req);

  const storeId = localStorage.getItem('bo_active_store_id');
  if (!storeId) return next(req);

  return next(req.clone({ setHeaders: { 'X-Store-Id': storeId } }));
};
