import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationsService } from '../services/notifications.service';

interface ApiErrorPayload {
  code?: string;
  message?: string;
  details?: unknown;
  traceId?: string;
}

export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifications = inject(NotificationsService);

  return next(req).pipe(
    catchError((error: unknown) => {
      const isHealthCheck = req.url.includes('/health');
      if (!(error instanceof HttpErrorResponse) || isHealthCheck) return throwError(() => error);

      const currentPath = typeof window !== 'undefined' ? window.location.pathname : '';
      const isSetupAuthNoise = currentPath.startsWith('/pos/setup') && error.status === 401;
      const isOfflineSyncAuthNoise = req.url.includes('/api/v1/offline/manual-sales') && error.status === 401;
      const isAutoRecoveryRedirect = typeof window !== 'undefined'
        && sessionStorage.getItem('pos_recovery_redirecting') === '1'
        && currentPath.startsWith('/pos/')
        && error.status === 401;
      const isBoUnauthorized = currentPath.startsWith('/bo/') && error.status === 401;

      if (isSetupAuthNoise || isOfflineSyncAuthNoise || isAutoRecoveryRedirect) {
        return throwError(() => error);
      }

      if (isBoUnauthorized) {
        handleBoUnauthorized(currentPath);
        return throwError(() => error);
      }

      const payload = (error.error ?? {}) as ApiErrorPayload;
      const code = payload.code ?? 'UNKNOWN_ERROR';
      const rawMessage = payload.message || error.message || 'Error inesperado';

      const friendly = mapFriendlyMessage(error.status, code, rawMessage, req.url, payload.details);
      notifications.push(error.status >= 500 ? 'error' : 'warning', friendly, payload.traceId);
      return throwError(() => error);
    })
  );
};

function handleBoUnauthorized(currentPath: string): void {
  if (typeof window === 'undefined') return;
  if (sessionStorage.getItem('bo_recovery_redirecting') === '1') return;

  sessionStorage.setItem('bo_recovery_redirecting', '1');
  localStorage.removeItem('bo_jwt');
  localStorage.removeItem('bo_role');
  localStorage.removeItem('bo_username');
  localStorage.removeItem('bo_expires_at');
  localStorage.removeItem('bo_active_store_id');
  localStorage.removeItem('bo_active_tenant_id');

  if (!currentPath.startsWith('/inicio/login')) {
    window.location.assign('/inicio/login?reason=session-expired&source=bo');
  }
}

function mapFriendlyMessage(status: number, code: string, fallback: string, url: string, details?: unknown): string {
  const translatedFallback = translateKnownEnglish(fallback);

  switch (code) {
    case 'VALIDATION_ERROR': {
      const detailed = parseValidationDetails(details);
      if (detailed) return detailed;
      if (translatedFallback && translatedFallback !== 'Error de validación' && translatedFallback !== 'Database update failed') return translatedFallback;
      return 'Revisa los datos ingresados y vuelve a intentar.';
    }
    case 'CASH_SESSION_NOT_OPEN':
      return 'No hay caja abierta para completar esta acción.';
    case 'SHIFT_CLOSE_BLOCKED':
      return 'No se puede cerrar caja porque hay tareas pendientes.';
    case 'PENDING_TRANSFER_LIMIT_REACHED':
      return 'El cliente ya tiene una transferencia pendiente.';
    case 'RETURN_WINDOW_EXPIRED':
      return 'La devolución está fuera de la ventana permitida.';
    case 'DUPLICATE_SUBMISSION':
      return 'Ya se está procesando la acción. Esperá un momento.';
    default:
      if (status === 0) return 'Sin conexión con el servidor.';
      if (status === 401) {
        if (url.includes('/api/v1/auth/operator-session')) return 'Credenciales de operador inválidas. Revisá usuario, contraseña y PIN.';
        if (url.includes('/api/v1/auth/operator-session/refresh')) return 'La sesión de operador venció. Iniciá sesión nuevamente.';
        if (url.includes('/api/v1/cash-sessions/')) return 'Iniciá sesión de operador antes de abrir o cerrar caja.';
        if (url.includes('/api/v1/auth/device/validate')) return 'Token de dispositivo inválido. Reconfigurá el dispositivo POS.';
        if (url.includes('/api/v1/dashboard')) return 'Tu usuario no tiene acceso al dashboard o la sesión expiró.';
        return 'Sesión vencida o no autorizada.';
      }
      if (status === 403) return 'No tenés permisos para esta acción.';
      if (status >= 500) return 'Ocurrió un error del servidor. Intentá nuevamente.';
      return translatedFallback;
  }
}

function translateKnownEnglish(text: string): string {
  const lower = `${text ?? ''}`.toLowerCase();

  if (lower.includes('no open cash session found')) return 'No hay una caja abierta.';
  if (lower.includes('please open a cash session first')) return 'No hay una caja abierta. Abrí una caja para continuar.';
  if (lower.includes('invalid credentials')) return 'Credenciales inválidas.';
  if (lower.includes('cart not found')) return 'Carrito no encontrado.';
  if (lower.includes('cart is not open')) return 'El carrito no está abierto.';
  if (lower.includes('cart is empty')) return 'El carrito está vacío.';
  if (lower.includes('sale not found')) return 'Venta no encontrada.';
  if (lower.includes('payment not found')) return 'Pago no encontrado.';
  if (lower.includes('reason is required')) return 'Debes ingresar un motivo.';
  if (lower.includes('validation error')) return 'Error de validación';

  return text;
}

function parseValidationDetails(details: unknown): string {
  if (typeof details !== 'string' || !details.trim()) return '';

  const text = details.trim();
  const lower = text.toLowerCase();

  if (lower.includes('ix_productstocks_productid_bucket') && lower.includes('duplicate key')) {
    return 'Ya existe un registro de stock para ese producto. Refrescá la pantalla y reintentá.';
  }

  if (lower.includes('duplicate key value violates unique constraint')) {
    return 'No se pudo guardar porque los datos ya existen.';
  }

  if (lower.includes('violates foreign key constraint')) {
    return 'No se pudo guardar porque uno de los datos relacionados no es válido.';
  }

  if (lower.includes('value too long for type character varying')) {
    return 'No se pudo guardar porque un texto supera el largo permitido.';
  }

  return text;
}
