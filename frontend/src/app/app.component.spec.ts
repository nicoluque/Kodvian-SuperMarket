import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { AppComponent } from './app.component';
import { AuthJwtService } from './core/services/auth-jwt.service';
import { BrandingService } from './core/services/branding.service';
import { HealthService } from './core/services/health.service';
import { OperatingModeService } from './core/services/operating-mode.service';
import { OperatorSessionService } from './core/services/operator-session.service';
import { SyncService } from './core/services/sync.service';

class RouterStub {
  url = '/';
  events = new Subject<any>();

  createUrlTree(commands: any[]): string {
    const path = commands
      .map((c) => `${c}`.replace(/^\/+|\/+$/g, ''))
      .filter((c) => c.length > 0)
      .join('/');
    return `/${path}`;
  }

  serializeUrl(url: string): string {
    return url;
  }

  emitNavigation(url: string): void {
    this.url = url;
    this.events.next(new NavigationEnd(1, url, url));
  }
}

describe('AppComponent', () => {
  let router: RouterStub;

  beforeEach(async () => {
    router = new RouterStub();

    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { data: {} } } },
        { provide: HealthService, useValue: { startPolling: jasmine.createSpy('startPolling') } },
        { provide: SyncService, useValue: { start: jasmine.createSpy('start') } },
        { provide: BrandingService, useValue: { load: jasmine.createSpy('load').and.resolveTo(undefined) } },
        {
          provide: AuthJwtService,
          useValue: {
            isTokenValid: jasmine.createSpy('isTokenValid').and.returnValue(true),
            getUsername: jasmine.createSpy('getUsername').and.returnValue('admin'),
            getRole: jasmine.createSpy('getRole').and.returnValue('Admin')
          }
        },
        {
          provide: OperatingModeService,
          useValue: {
            getConfig: jasmine.createSpy('getConfig').and.returnValue({ mode: 'MiniMarketFull' }),
            getModeLabel: jasmine.createSpy('getModeLabel').and.returnValue('MiniMarket Full')
          }
        },
        {
          provide: OperatorSessionService,
          useValue: {
            getSessionToken: jasmine.createSpy('getSessionToken').and.returnValue(null),
            getOperatorName: jasmine.createSpy('getOperatorName').and.returnValue('')
          }
        }
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should show BO selector in backoffice with valid session', () => {
    router.url = '/bo/stock';
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    fixture.detectChanges();

    expect(app.showBoSelector).toBeTrue();
  });

  it('should toggle caja banner on navigation', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance.showCajaBanner).toBeFalse();

    router.emitNavigation('/pos/caja/ventas');
    fixture.detectChanges();

    expect(fixture.componentInstance.showCajaBanner).toBeTrue();
  });

  it('should render global shell components', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-global-loading')).toBeTruthy();
    expect(compiled.querySelector('app-global-status-bar')).toBeTruthy();
    expect(compiled.querySelector('app-global-toast')).toBeTruthy();
  });
});
