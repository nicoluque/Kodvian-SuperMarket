import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthJwtService } from '../core/services/auth-jwt.service';
import { BoAuthService } from '../core/services/bo-auth.service';
import { OperatingModeService } from '../core/services/operating-mode.service';
import { OperatorSessionService } from '../core/services/operator-session.service';

@Component({
  standalone: true,
  selector: 'app-inicio-operativo',
  imports: [CommonModule],
  template: `
    <main class="inicio-container">
      <div class="hero-bg">
        <div class="hero-shape shape-1"></div>
        <div class="hero-shape shape-2"></div>
        <div class="hero-shape shape-3"></div>
      </div>

      <header class="hero">
        <div class="hero-content">
          <div class="session-row">
            <span class="session-chip">{{ username }} · {{ roleLabel }}</span>
            <button class="session-logout" (click)="logout()">Cerrar sesion</button>
          </div>
          <div class="hero-badge">
            <span class="badge-dot"></span>
            <span>Centro Operativo</span>
          </div>
          <h1>Todo listo para vender</h1>
          <p class="hero-subtitle">Gestioná tu negocio con potencia y simplicidad</p>
          <div class="mode-chip">
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect>
              <line x1="8" y1="21" x2="16" y2="21"></line>
              <line x1="12" y1="17" x2="12" y2="21"></line>
            </svg>
            <span>{{ modeLabel }}</span>
          </div>
          <div class="device-chip">
            <span>Dispositivo: {{ deviceTypeLabel }}</span>
          </div>
        </div>
      </header>

      <section class="main-actions">
        <h2 class="section-title">Accesos rapidos</h2>
        
        <div class="action-grid">
          <article class="action-card primary" *ngIf="canUsePos" (click)="goCaja()" tabindex="0" (keydown.enter)="goCaja()">
            <div class="card-icon">
              <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="2" y="5" width="20" height="14" rx="2"></rect>
                <line x1="2" y1="10" x2="22" y2="10"></line>
              </svg>
            </div>
            <div class="card-content">
              <h3>Caja</h3>
              <p>Venta en mostrador, apertura, cierre y cobro diario</p>
            </div>
            <div class="card-arrow">
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="5" y1="12" x2="19" y2="12"></line>
                <polyline points="12 5 19 12 12 19"></polyline>
              </svg>
            </div>
            <div class="card-glow"></div>
          </article>

          <article class="action-card" *ngIf="modules.tablet && canUsePos" (click)="goTablet()" tabindex="0" (keydown.enter)="goTablet()">
            <div class="card-icon tablet">
              <svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="4" y="2" width="16" height="20" rx="2" ry="2"></rect>
                <line x1="12" y1="18" x2="12.01" y2="18"></line>
              </svg>
            </div>
            <div class="card-content">
              <h3>Totem</h3>
              <p>Armado rapido de pedidos y envio a caja</p>
            </div>
            <div class="card-arrow">
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="5" y1="12" x2="19" y2="12"></line>
                <polyline points="12 5 19 12 12 19"></polyline>
              </svg>
            </div>
          </article>

          <article class="action-card" *ngIf="canUseBackoffice" (click)="go('/bo/dashboard-gerencial')" tabindex="0" (keydown.enter)="go('/bo/dashboard-gerencial')">
            <div class="card-icon backoffice">
              <svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
                <polyline points="9 22 9 12 15 12 15 22"></polyline>
              </svg>
            </div>
            <div class="card-content">
              <h3>Administración</h3>
              <p>Gestion comercial, compras, stock y reportes</p>
            </div>
            <div class="card-arrow">
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="5" y1="12" x2="19" y2="12"></line>
                <polyline points="12 5 19 12 12 19"></polyline>
              </svg>
            </div>
          </article>
        </div>
      </section>

      <section class="secondary-actions" *ngIf="canUseBackoffice">
        <p class="tech-link" (click)="go('/pos/setup?target=auto')" tabindex="0" (keydown.enter)="go('/pos/setup?target=auto')">
          Soporte tecnico: configurar dispositivo POS
        </p>
      </section>

      <section class="capabilities" *ngIf="hasActiveModules">
        <h3 class="capabilities-title">Modulos activos</h3>
        <div class="capabilities-pills">
          <span class="pill" *ngIf="modules.tablet">Totem</span>
          <span class="pill" *ngIf="modules.envases">Envases</span>
          <span class="pill" *ngIf="modules.cuentaCorriente">Cuenta Corriente</span>
          <span class="pill" *ngIf="modules.comprasSugeridas">Compras Sugeridas</span>
          <span class="pill" *ngIf="modules.reportes">Reportes</span>
        </div>
      </section>
    </main>
  `,
  styles: [`
    .inicio-container {
      min-height: 100vh;
      padding: 0 0 2rem 0;
      position: relative;
      overflow: hidden;
    }

    .hero-bg {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      height: 380px;
      background: linear-gradient(135deg, #1B4D3E 0%, #1a1a1a 100%);
      overflow: hidden;
      z-index: 0;
    }

    .hero-shape {
      position: absolute;
      border-radius: 50%;
      filter: blur(80px);
      opacity: 0.4;
    }

    .shape-1 {
      width: 400px;
      height: 400px;
      background: #BFEBF1;
      top: -150px;
      right: -100px;
      opacity: 0.25;
    }

    .shape-2 {
      width: 300px;
      height: 300px;
      background: #a8d8e0;
      bottom: -100px;
      left: -50px;
      opacity: 0.3;
    }

    .shape-3 {
      width: 200px;
      height: 200px;
      background: #BFEBF1;
      top: 50%;
      left: 30%;
      opacity: 0.15;
    }

    .hero {
      position: relative;
      z-index: 1;
      padding: 3rem 1.5rem 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .hero-content {
      animation: fadeInUp 0.6s ease-out;
    }

    .session-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
      gap: 0.75rem;
      flex-wrap: wrap;
    }

    .session-chip {
      background: rgba(255, 255, 255, 0.16);
      border: 1px solid rgba(255, 255, 255, 0.22);
      color: #e8f7f2;
      border-radius: 999px;
      padding: 0.35rem 0.7rem;
      font-size: 0.82rem;
      font-weight: 600;
    }

    .session-logout {
      background: rgba(255, 255, 255, 0.14);
      color: #f8fbfa;
      border: 1px solid rgba(255, 255, 255, 0.28);
      border-radius: 8px;
      padding: 0.35rem 0.75rem;
      cursor: pointer;
    }

    @keyframes fadeInUp {
      from {
        opacity: 0;
        transform: translateY(20px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .hero-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      background: rgba(191, 235, 241, 0.15);
      border: 1px solid rgba(191, 235, 241, 0.3);
      padding: 0.4rem 0.75rem;
      border-radius: 20px;
      color: #BFEBF1;
      font-size: 0.8rem;
      font-weight: 500;
      margin-bottom: 1rem;
    }

    .badge-dot {
      width: 8px;
      height: 8px;
      background: #28a745;
      border-radius: 50%;
      animation: pulse 2s infinite;
    }

    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.5; }
    }

    .hero h1 {
      font-size: 2.5rem;
      font-weight: 700;
      color: #FFFFFF;
      margin: 0 0 0.5rem 0;
      letter-spacing: -0.02em;
    }

    .hero-subtitle {
      font-size: 1.1rem;
      color: #9EABB1;
      margin: 0 0 1.5rem 0;
    }

    .mode-chip {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      background: #BFEBF1;
      color: #1B4D3E;
      padding: 0.5rem 1rem;
      border-radius: 8px;
      font-weight: 600;
      font-size: 0.9rem;
    }

    .mode-chip svg {
      opacity: 0.7;
    }

    .main-actions {
      position: relative;
      z-index: 1;
      max-width: 1200px;
      margin: 0 auto;
      padding: 0 1.5rem;
    }

    .section-title {
      font-size: 0.85rem;
      text-transform: uppercase;
      letter-spacing: 0.1em;
      color: #9EABB1;
      margin: 0 0 1rem 0;
      font-weight: 600;
    }

    .action-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 1rem;
    }

    .action-card {
      background: #FFFFFF;
      border-radius: 16px;
      padding: 1.5rem;
      display: flex;
      align-items: center;
      gap: 1rem;
      cursor: pointer;
      position: relative;
      overflow: hidden;
      transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
      border: 1px solid rgba(0, 0, 0, 0.06);
      animation: cardFadeIn 0.5s ease-out backwards;
    }

    .action-card:nth-child(1) { animation-delay: 0.1s; }
    .action-card:nth-child(2) { animation-delay: 0.2s; }
    .action-card:nth-child(3) { animation-delay: 0.3s; }

    @keyframes cardFadeIn {
      from {
        opacity: 0;
        transform: translateY(15px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .action-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 12px 24px rgba(0, 0, 0, 0.12);
      border-color: rgba(191, 235, 241, 0.5);
    }

    .action-card:focus-visible {
      outline: 2px solid #BFEBF1;
      outline-offset: 2px;
    }

    .action-card.primary {
      background: linear-gradient(135deg, #1B4D3E 0%, #234F45 100%);
      border: none;
    }

    .action-card.primary:hover {
      box-shadow: 0 16px 32px rgba(0, 0, 0, 0.25);
    }

    .card-icon {
      width: 56px;
      height: 56px;
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: #BFEBF1;
      color: #1B4D3E;
      flex-shrink: 0;
    }

    .card-icon.tablet {
      background: #e9ecef;
      color: #495057;
    }

    .card-icon.backoffice {
      background: #a8d8e0;
      color: #1B4D3E;
    }

    .device-chip {
      display: inline-flex;
      align-items: center;
      margin-left: 0.6rem;
      margin-top: 0.35rem;
      padding: 0.42rem 0.75rem;
      border-radius: 16px;
      background: rgba(255, 255, 255, 0.14);
      border: 1px solid rgba(255, 255, 255, 0.24);
      color: #e3f4ee;
      font-size: 0.82rem;
      font-weight: 600;
    }

    .action-card.primary .card-icon {
      background: #BFEBF1;
      color: #1B4D3E;
    }

    .card-content {
      flex: 1;
      min-width: 0;
    }

    .card-content h3 {
      margin: 0 0 0.25rem 0;
      font-size: 1.25rem;
      font-weight: 600;
      color: #1B4D3E;
    }

    .action-card.primary .card-content h3 {
      color: #FFFFFF;
    }

    .card-content p {
      margin: 0;
      font-size: 0.875rem;
      color: #6c757d;
      line-height: 1.4;
    }

    .action-card.primary .card-content p {
      color: #9EABB1;
    }

    .card-arrow {
      color: #9EABB1;
      transition: transform 0.2s ease;
    }

    .action-card:hover .card-arrow {
      transform: translateX(4px);
      color: #BFEBF1;
    }

    .action-card.primary:hover .card-arrow {
      color: #BFEBF1;
    }

    .card-glow {
      position: absolute;
      top: 0;
      right: 0;
      width: 150px;
      height: 100%;
      background: linear-gradient(90deg, transparent, rgba(191, 235, 241, 0.1));
      pointer-events: none;
    }

    .secondary-actions {
      max-width: 1200px;
      margin: 0.75rem auto 0;
      padding: 0 1.5rem;
      animation: fadeInUp 0.5s ease-out 0.4s backwards;
    }

    .tech-link {
      margin: 0;
      color: #7d8f89;
      font-size: 0.86rem;
      cursor: pointer;
      text-decoration: underline;
      text-underline-offset: 2px;
    }

    .capabilities {
      max-width: 1200px;
      margin: 2rem auto 0;
      padding: 0 1.5rem;
      animation: fadeInUp 0.5s ease-out 0.5s backwards;
    }

    .capabilities-title {
      font-size: 0.8rem;
      text-transform: uppercase;
      letter-spacing: 0.08em;
      color: #9EABB1;
      margin: 0 0 0.75rem 0;
      font-weight: 600;
    }

    .capabilities-pills {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
    }

    .pill {
      background: #e9ecef;
      color: #495057;
      padding: 0.35rem 0.75rem;
      border-radius: 20px;
      font-size: 0.8rem;
      font-weight: 500;
    }

    @media (max-width: 640px) {
      .hero {
        padding: 2rem 1rem 1.5rem;
      }

      .hero h1 {
        font-size: 1.75rem;
      }

      .hero-subtitle {
        font-size: 1rem;
      }

      .action-grid {
        grid-template-columns: 1fr;
      }

      .action-card {
        padding: 1.25rem;
      }

      .card-icon {
        width: 48px;
        height: 48px;
      }

      .card-arrow {
        display: none;
      }
    }
  `]
})
export class InicioOperativoComponent {
  modeLabel = '';
  deviceTypeLabel = 'No configurado';
  roleLabel = 'Operador';
  username = 'Usuario';
  canUsePos = true;
  canUseBackoffice = false;
  modules = {
    tablet: true,
    envases: true,
    cuentaCorriente: true,
    comprasSugeridas: true,
    reportes: true
  };

  constructor(
    private readonly router: Router,
    private readonly operatingMode: OperatingModeService,
    private readonly authJwt: AuthJwtService,
    private readonly boAuth: BoAuthService,
    private readonly operatorSession: OperatorSessionService
  ) {
    const cfg = this.operatingMode.getConfig();
    this.modeLabel = this.operatingMode.getModeLabel(cfg.mode);
    this.deviceTypeLabel = this.resolveDeviceTypeLabel(this.operatingMode.getDeviceType());
    this.modules = cfg.modules;

    const role = this.authJwt.getRole() ?? this.operatorSession.getOperatorRole() ?? 'Operator';
    const username = this.authJwt.getUsername() ?? this.operatorSession.getOperatorName();
    this.username = username;
    this.roleLabel = role === 'Admin' ? 'Administrador' : role === 'Supervisor' || role === 'Manager' ? 'Supervisor' : 'Operador';
    this.canUsePos = role === 'Operator' || role === 'Supervisor' || role === 'Admin' || role === 'Manager';
    this.canUseBackoffice = role === 'Supervisor' || role === 'Admin' || role === 'Manager';
  }

  get hasActiveModules(): boolean {
    return this.modules.tablet || this.modules.envases || 
           this.modules.cuentaCorriente || this.modules.comprasSugeridas || 
           this.modules.reportes;
  }

  go(path: string): void {
    void this.router.navigateByUrl(path);
  }

  private resolveDeviceTypeLabel(deviceType: string): string {
    if (deviceType === 'CashRegister') return 'Caja fisica';
    if (deviceType === 'Tablet') return 'Totem';
    return 'No configurado';
  }

  logout(): void {
    this.operatorSession.clearSession();
    this.boAuth.logout();
    localStorage.removeItem('bo_active_store_id');
    localStorage.removeItem('bo_active_tenant_id');
    void this.router.navigateByUrl('/inicio/login');
  }

  goCaja(): void {
    const token = localStorage.getItem('pos_device_token');
    const deviceType = this.operatingMode.getDeviceType();
    if (token && deviceType === 'CashRegister') {
      void this.router.navigateByUrl('/pos/caja/apertura');
      return;
    }
    const reason = token ? 'device_mismatch' : 'missing';
    void this.router.navigateByUrl(`/pos/setup?reason=${reason}&target=caja&prefill=demo-device-caja`);
  }

  goTablet(): void {
    const token = localStorage.getItem('pos_device_token');
    const deviceType = this.operatingMode.getDeviceType();
    if (token && (deviceType === 'Tablet' || this.operatingMode.getConfig().mode === 'TotemQrOnly')) {
      void this.router.navigateByUrl('/pos/tablet/nueva');
      return;
    }
    const reason = token ? 'device_mismatch' : 'missing';
    void this.router.navigateByUrl(`/pos/setup?reason=${reason}&target=tablet&prefill=demo-device-tablet`);
  }
}
