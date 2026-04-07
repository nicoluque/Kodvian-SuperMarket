import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { OperatorSessionService } from '../services/operator-session.service';
import { catchError, from, switchMap, throwError } from 'rxjs';

export const operatorSessionInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.includes('/api/')) return next(req);

  const path = typeof window !== 'undefined' ? window.location.pathname : '';
  if (path.startsWith('/bo/')) return next(req);

  if (req.url.includes('/api/v1/auth/operator-session')) {
    return next(req);
  }

  const operatorSession = inject(OperatorSessionService);

  const session = operatorSession.getSessionToken();
  const request = session
    ? req.clone({
        setHeaders: {
          'X-Operator-Session': session
        }
      })
    : req;

  return next(request).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      if (req.headers.has('X-Session-Retry')) {
        return throwError(() => error);
      }

      if (!session) {
        return from(handlePosUnauthorized(path, operatorSession)).pipe(
          switchMap(() => throwError(() => error))
        );
      }

      return from(operatorSession.refreshSession()).pipe(
        switchMap(() => {
          const renewed = operatorSession.getSessionToken();
          if (!renewed) return throwError(() => error);

          return next(
            req.clone({
              setHeaders: {
                'X-Operator-Session': renewed,
                'X-Session-Retry': '1'
              }
            })
          );
        }),
        catchError(() => {
          return from(handlePosUnauthorized(path, operatorSession)).pipe(
            switchMap(() => throwError(() => error))
          );
        })
      );
    })
  );
};

async function handlePosUnauthorized(path: string, operatorSession: OperatorSessionService): Promise<void> {
  operatorSession.clearSession();
  if (typeof window === 'undefined' || !path.startsWith('/pos/')) return;

  sessionStorage.setItem('pos_recovery_redirecting', '1');

  const target = path.startsWith('/pos/tablet/') ? 'tablet' : 'caja';
  const fallbackPrefill = target === 'tablet' ? 'demo-device-tablet' : 'demo-device-caja';
  const token = localStorage.getItem('pos_device_token') ?? '';

  if (!token) {
    redirectToSetup('missing', target, fallbackPrefill);
    return;
  }

  try {
    const response = await fetch('/api/v1/auth/device/validate', {
      method: 'GET',
      headers: { 'X-Device-Token': token }
    });

    if (!response.ok) {
      clearPosDeviceContext();
      redirectToSetup('invalid', target, fallbackPrefill);
      return;
    }

    const validation = await response.json() as { deviceType?: string };
    const deviceType = `${validation?.deviceType ?? ''}`;
    const requiredType = target === 'tablet' ? 'Tablet' : 'CashRegister';

    if (deviceType && deviceType !== requiredType) {
      redirectToSetup('device_mismatch', target, fallbackPrefill);
      return;
    }

    window.location.assign('/inicio/login?reason=session-expired&guide=1');
  } catch {
    clearPosDeviceContext();
    redirectToSetup('invalid', target, fallbackPrefill);
  }
}

function redirectToSetup(reason: string, target: 'tablet' | 'caja', prefill: string): void {
  const query = new URLSearchParams({ reason, target, prefill, guide: '1', source: 'runtime' });
  window.location.assign(`/pos/setup?${query.toString()}`);
}

function clearPosDeviceContext(): void {
  localStorage.removeItem('pos_device_token');
  localStorage.removeItem('pos_device_token_validated');
  localStorage.removeItem('operating_device_type');
}
