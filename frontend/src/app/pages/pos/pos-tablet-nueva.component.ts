import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { OperatingModeService } from '../../core/services/operating-mode.service';
import { PosCajaService } from '../../core/services/pos-caja.service';
import { PosModuleNavComponent } from '../../shared/components/pos-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-pos-tablet-nueva',
  imports: [CommonModule, PosModuleNavComponent],
  template: `
    <main class="tablet-container">
      <div class="hero-bg">
        <div class="hero-shape shape-1"></div>
        <div class="hero-shape shape-2"></div>
      </div>

      <div class="nav-wrap">
        <app-pos-module-nav />
      </div>

      <header class="hero">
        <div class="hero-content">
          <div class="hero-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="4" y="2" width="16" height="20" rx="2" ry="2"></rect>
              <line x1="12" y1="18" x2="12.01" y2="18"></line>
            </svg>
          </div>
          <h1>{{ modeLabel }}</h1>
          <p class="hero-subtitle">{{ subtitle }}</p>
        </div>
      </header>

      <section class="content-section">
        <div class="alert error" *ngIf="!tabletEnabled">
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="4.93" y1="4.93" x2="19.07" y2="19.07"></line>
          </svg>
          El Totem esta deshabilitado para el modo operativo actual.
        </div>

        <div class="card main-card">
          <div class="card-body centered">
            <div class="icon-large">
              <svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                <rect x="4" y="2" width="16" height="20" rx="2" ry="2"></rect>
                <line x1="12" y1="18" x2="12.01" y2="18"></line>
                <line x1="8" y1="6" x2="16" y2="6"></line>
                <line x1="8" y1="10" x2="16" y2="10"></line>
                <line x1="8" y1="14" x2="12" y2="14"></line>
              </svg>
            </div>
            
            <h2>Iniciar atencion</h2>
            <p class="description">{{ description }}</p>

            <button class="btn-primary btn-large" [disabled]="loading || !tabletEnabled" (click)="createCart()">
              <svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="12" y1="5" x2="12" y2="19"></line>
                <line x1="5" y1="12" x2="19" y2="12"></line>
              </svg>
              {{ loading ? 'Creando...' : 'Nuevo cliente' }}
            </button>

            <button class="btn-fallback" *ngIf="!tabletEnabled" (click)="goCaja()">
              <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="2" y="5" width="20" height="14" rx="2"></rect>
                <line x1="2" y1="10" x2="22" y2="10"></line>
              </svg>
              Ir a Caja
            </button>
          </div>
        </div>

        <div class="alert success" *ngIf="message">
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
            <polyline points="22 4 12 14.01 9 11.01"></polyline>
          </svg>
          {{ message }}
        </div>

        <div class="alert error" *ngIf="error">
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="15" y1="9" x2="9" y2="15"></line>
            <line x1="9" y1="9" x2="15" y2="15"></line>
          </svg>
          {{ error }}
        </div>
      </section>
    </main>
  `,
  styles: [`
    .tablet-container {
      min-height: 100vh;
      padding: 0 0 2rem 0;
      position: relative;
      overflow: hidden;
    }

    .nav-wrap {
      position: relative;
      z-index: 2;
      padding: 14px 18px 0;
      max-width: 1100px;
      margin: 0 auto;
    }

    .hero-bg {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      height: 280px;
      background: linear-gradient(135deg, #1B4D3E 0%, #234F45 100%);
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
      width: 350px;
      height: 350px;
      background: #BFEBF1;
      top: -120px;
      right: -80px;
      opacity: 0.2;
    }

    .shape-2 {
      width: 250px;
      height: 250px;
      background: #a8d8e0;
      bottom: -80px;
      left: -60px;
      opacity: 0.25;
    }

    .hero {
      position: relative;
      z-index: 1;
      padding: 2rem 1.5rem 1.5rem;
      max-width: 500px;
      margin: 0 auto;
      text-align: center;
    }

    .hero-content {
      animation: fadeInUp 0.5s ease-out;
    }

    @keyframes fadeInUp {
      from { opacity: 0; transform: translateY(20px); }
      to { opacity: 1; transform: translateY(0); }
    }

    .hero-icon {
      width: 72px;
      height: 72px;
      margin: 0 auto 1.25rem;
      background: #BFEBF1;
      border-radius: 16px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: #1B4D3E;
    }

    .hero h1 {
      font-size: 1.75rem;
      font-weight: 700;
      color: #FFFFFF;
      margin: 0 0 0.5rem 0;
    }

    .hero-subtitle {
      font-size: 1rem;
      color: rgba(255, 255, 255, 0.7);
      margin: 0;
    }

    .content-section {
      position: relative;
      z-index: 1;
      max-width: 500px;
      margin: 0 auto;
      padding: 0 1.5rem;
      animation: fadeInUp 0.5s ease-out 0.2s backwards;
    }

    .alert {
      display: flex;
      align-items: center;
      gap: 0.6rem;
      padding: 0.75rem 1rem;
      border-radius: 10px;
      font-size: 0.9rem;
      font-weight: 500;
      margin-bottom: 1rem;
    }

    .alert.error {
      background: #f8d7da;
      color: #721c24;
    }

    .alert.success {
      background: #d4edda;
      color: #155724;
    }

    .card {
      background: #FFFFFF;
      border-radius: 16px;
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
      overflow: hidden;
    }

    .card-body {
      padding: 2rem 1.5rem;
    }

    .card-body.centered {
      text-align: center;
      display: flex;
      flex-direction: column;
      align-items: center;
    }

    .icon-large {
      width: 100px;
      height: 100px;
      background: linear-gradient(135deg, #e9ecef 0%, #dee2e6 100%);
      border-radius: 20px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: #6c757d;
      margin-bottom: 1.5rem;
    }

    .card-body h2 {
      font-size: 1.25rem;
      font-weight: 600;
      color: #1B4D3E;
      margin: 0 0 0.5rem 0;
    }

    .description {
      font-size: 0.95rem;
      color: #6c757d;
      margin: 0 0 1.5rem 0;
    }

    .btn-primary {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.6rem;
      padding: 1rem 2rem;
      background: linear-gradient(135deg, #1B4D3E 0%, #234F45 100%);
      color: #FFFFFF;
      border: none;
      border-radius: 12px;
      font-size: 1.1rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s ease;
      box-shadow: 0 4px 12px rgba(27, 77, 62, 0.3);
    }

    .btn-primary:hover:not(:disabled) {
      transform: translateY(-2px);
      box-shadow: 0 6px 20px rgba(27, 77, 62, 0.4);
    }

    .btn-primary:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .btn-large {
      width: 100%;
      max-width: 280px;
    }

    .btn-fallback {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      margin-top: 1rem;
      padding: 0.75rem 1.5rem;
      background: transparent;
      color: #495057;
      border: 1px dashed #9EABB1;
      border-radius: 10px;
      font-size: 0.9rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .btn-fallback:hover {
      background: #f8fafc;
      border-style: solid;
      border-color: #1B4D3E;
      color: #1B4D3E;
    }

    @media (max-width: 640px) {
      .hero {
        padding: 1.5rem 1rem 1rem;
      }

      .hero h1 {
        font-size: 1.5rem;
      }

      .content-section {
        padding: 0 1rem;
      }

      .card-body {
        padding: 1.5rem 1rem;
      }
    }
  `]
})
export class PosTabletNuevaComponent {
  loading = false;
  message = '';
  error = '';
  tabletEnabled = true;
  modeLabel = 'Totem';
  subtitle = 'Armado rapido de pedidos para enviar a caja';
  description = 'Crear un nuevo carrito para un cliente';
  private readonly preferredPosRoute: string;

  constructor(private readonly api: PosCajaService, private readonly router: Router, operatingMode: OperatingModeService) {
    const cfg = operatingMode.getConfig();
    this.preferredPosRoute = operatingMode.getPreferredPosRoute();
    this.tabletEnabled = cfg.modules.tablet;
    if (cfg.mode === 'TotemQrOnly') {
      this.modeLabel = 'Totem';
      this.subtitle = 'Escanea articulos y cobra por QR o cuenta corriente';
      this.description = 'Crear carrito para atencion en totem';
    }
  }

  async createCart(): Promise<void> {
    this.loading = true;
    this.message = '';
    this.error = '';

    try {
      const cart = await this.api.createCart();
      this.message = `Carrito #${cart.id} creado`;
      void this.router.navigateByUrl(`/pos/tablet/carrito/${cart.id}`);
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo crear carrito';
    } finally {
      this.loading = false;
    }
  }

  goCaja(): void {
    void this.router.navigateByUrl(this.preferredPosRoute);
  }
}
