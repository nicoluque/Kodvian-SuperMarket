import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { jwtInterceptor } from './core/interceptors/jwt.interceptor';
import { boStoreInterceptor } from './core/interceptors/bo-store.interceptor';
import { deviceTokenInterceptor } from './core/interceptors/device-token.interceptor';
import { operatorSessionInterceptor } from './core/interceptors/operator-session.interceptor';
import { loadingInterceptor } from './core/interceptors/loading.interceptor';
import { httpErrorInterceptor } from './core/interceptors/http-error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([
      loadingInterceptor,
      jwtInterceptor,
      boStoreInterceptor,
      deviceTokenInterceptor,
      httpErrorInterceptor,
      operatorSessionInterceptor
    ]))
  ]
};
