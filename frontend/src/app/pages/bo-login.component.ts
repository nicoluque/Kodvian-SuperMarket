import { CommonModule } from '@angular/common';
import { Component, isDevMode } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthJwtService } from '../core/services/auth-jwt.service';
import { BoAuthService } from '../core/services/bo-auth.service';
import { BrandingService } from '../core/services/branding.service';

@Component({
  standalone: true,
  selector: 'app-bo-login',
  imports: [CommonModule, FormsModule],
  template: `
    <main class="login-container">
      <div class="hero-bg">
        <div class="hero-shape shape-1"></div>
        <div class="hero-shape shape-2"></div>
      </div>

      <div class="login-card">
        <div class="brand-header">
          <img *ngIf="logoUrl" [src]="logoUrl" alt="logo" class="brand-logo" />
          <div class="brand-info">
            <h1>{{ displayName }}</h1>
            <span>Backoffice</span>
          </div>
        </div>

        <div class="card-body">
          <h2>Ingreso comercial</h2>

          <div class="form-group">
            <label>Usuario</label>
            <input 
              type="text" 
              [(ngModel)]="username" 
              [disabled]="loading"
              placeholder="Ingresa tu usuario"
              autocomplete="username"
            />
          </div>

          <div class="form-group">
            <label>Contrasena</label>
            <input 
              type="password" 
              [(ngModel)]="password" 
              [disabled]="loading"
              placeholder="Ingresa tu contrasena"
              autocomplete="current-password"
            />
          </div>

          <div class="form-group">
            <label>PIN</label>
            <input 
              type="password" 
              [(ngModel)]="pin" 
              [disabled]="loading"
              placeholder="Ingresa tu PIN"
              autocomplete="one-time-code"
            />
          </div>

          <div class="error-message" *ngIf="error">
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"></circle>
              <line x1="15" y1="9" x2="9" y2="15"></line>
              <line x1="9" y1="9" x2="15" y2="15"></line>
            </svg>
            {{ error }}
          </div>

          <button class="btn-primary" (click)="loginWithCredentials()" [disabled]="loading || !isFormValid()">
            <svg *ngIf="!loading" xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4"></path>
              <polyline points="10 17 15 12 10 7"></polyline>
              <line x1="15" y1="12" x2="3" y2="12"></line>
            </svg>
            <svg *ngIf="loading" class="spin" xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="12" y1="2" x2="12" y2="6"></line>
              <line x1="12" y1="18" x2="12" y2="22"></line>
              <line x1="4.93" y1="4.93" x2="7.76" y2="7.76"></line>
              <line x1="16.24" y1="16.24" x2="19.07" y2="19.07"></line>
              <line x1="2" y1="12" x2="6" y2="12"></line>
              <line x1="18" y1="12" x2="22" y2="12"></line>
              <line x1="4.93" y1="19.07" x2="7.76" y2="16.24"></line>
              <line x1="16.24" y1="7.76" x2="19.07" y2="4.93"></line>
            </svg>
            {{ loading ? 'Ingresando...' : 'Ingresar' }}
          </button>

          <button class="btn-secondary" type="button" (click)="goInicio()" [disabled]="loading">
            Volver al inicio operativo
          </button>

          <div class="divider" *ngIf="showDevSection">
            <span>Ingreso tecnico</span>
          </div>

          <div class="jwt-section" *ngIf="showDevSection">
            <p class="jwt-hint">Solo soporte: JWT manual</p>
            <textarea 
              [(ngModel)]="jwt" 
              rows="3"
              placeholder="Pega el token JWT aqui"
            ></textarea>
            <button class="btn-secondary" (click)="loginWithJwt()" [disabled]="!jwt.trim()">
              Ingresar con JWT
            </button>
          </div>
        </div>
      </div>
    </main>
  `,
  styles: [`
    .login-container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2rem;
      position: relative;
      overflow: hidden;
    }

    .hero-bg {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: linear-gradient(135deg, #1B4D3E 0%, #234F45 100%);
      z-index: 0;
    }

    .hero-shape {
      position: absolute;
      border-radius: 50%;
      filter: blur(100px);
      opacity: 0.3;
    }

    .shape-1 {
      width: 500px;
      height: 500px;
      background: #BFEBF1;
      top: -200px;
      right: -100px;
    }

    .shape-2 {
      width: 400px;
      height: 400px;
      background: #1B4D3E;
      bottom: -150px;
      left: -100px;
    }

    .login-card {
      background: #FFFFFF;
      border-radius: 24px;
      width: 100%;
      max-width: 420px;
      box-shadow: 0 25px 80px rgba(0, 0, 0, 0.25);
      position: relative;
      z-index: 1;
      overflow: hidden;
      animation: slideUp 0.5s ease-out;
    }

    @keyframes slideUp {
      from {
        opacity: 0;
        transform: translateY(30px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .brand-header {
      background: linear-gradient(120deg, #1B4D3E, #234F45);
      padding: 1.5rem;
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .brand-logo {
      width: 48px;
      height: 48px;
      object-fit: contain;
      background: #FFFFFF;
      border-radius: 10px;
      padding: 6px;
    }

    .brand-info h1 {
      margin: 0;
      font-size: 1.5rem;
      font-weight: 700;
      color: #FFFFFF;
    }

    .brand-info span {
      font-size: 0.875rem;
      color: #BFEBF1;
    }

    .card-body {
      padding: 2rem;
    }

    .card-body h2 {
      margin: 0 0 1.5rem 0;
      font-size: 1.25rem;
      font-weight: 600;
      color: #1B4D3E;
    }

    .form-group {
      margin-bottom: 1rem;
    }

    .form-group label {
      display: block;
      font-size: 0.875rem;
      font-weight: 500;
      color: #495057;
      margin-bottom: 0.5rem;
    }

    .form-group input,
    .jwt-section textarea {
      width: 100%;
      padding: 0.875rem 1rem;
      border: 2px solid #E9ECEF;
      border-radius: 12px;
      font-size: 1rem;
      transition: border-color 0.2s, box-shadow 0.2s;
      box-sizing: border-box;
    }

    .form-group input:focus,
    .jwt-section textarea:focus {
      outline: none;
      border-color: #1B4D3E;
      box-shadow: 0 0 0 4px rgba(27, 77, 62, 0.1);
    }

    .form-group input::placeholder,
    .jwt-section textarea::placeholder {
      color: #9EABB1;
    }

    .form-group input:disabled {
      background: #F8F9FA;
      cursor: not-allowed;
    }

    .error-message {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.75rem 1rem;
      background: #FEF2F2;
      border: 1px solid #FECACA;
      border-radius: 10px;
      color: #DC2626;
      font-size: 0.875rem;
      margin-bottom: 1rem;
    }

    .btn-primary {
      width: 100%;
      padding: 1rem;
      background: #1B4D3E;
      color: #FFFFFF;
      border: none;
      border-radius: 12px;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      transition: background 0.2s, transform 0.1s;
    }

    .btn-primary:hover:not(:disabled) {
      background: #234F45;
    }

    .btn-primary:active:not(:disabled) {
      transform: scale(0.98);
    }

    .btn-primary:disabled {
      background: #9EABB1;
      cursor: not-allowed;
    }

    .divider {
      display: flex;
      align-items: center;
      margin: 1.5rem 0;
      color: #9EABB1;
      font-size: 0.75rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .divider::before,
    .divider::after {
      content: '';
      flex: 1;
      height: 1px;
      background: #E9ECEF;
    }

    .divider span {
      padding: 0 1rem;
    }

    .jwt-section p {
      margin: 0 0 0.75rem 0;
      font-size: 0.875rem;
      color: #6C757D;
    }

    .jwt-section textarea {
      resize: none;
      font-family: monospace;
      font-size: 0.875rem;
      margin-bottom: 1rem;
    }

    .btn-secondary {
      width: 100%;
      padding: 0.875rem;
      background: #F8F9FA;
      color: #495057;
      border: 2px solid #E9ECEF;
      border-radius: 12px;
      font-size: 0.9375rem;
      font-weight: 500;
      cursor: pointer;
      transition: background 0.2s, border-color 0.2s;
    }

    .btn-secondary:hover:not(:disabled) {
      background: #E9ECEF;
      border-color: #DEE2E6;
    }

    .btn-secondary:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .spin {
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      from { transform: rotate(0deg); }
      to { transform: rotate(360deg); }
    }

    @media (max-width: 480px) {
      .login-container {
        padding: 1rem;
      }

      .login-card {
        border-radius: 20px;
      }

      .brand-header {
        padding: 1.25rem;
      }

      .brand-logo {
        width: 40px;
        height: 40px;
      }

      .brand-info h1 {
        font-size: 1.25rem;
      }

      .card-body {
        padding: 1.5rem;
      }
    }
  `]
})
export class BoLoginComponent {
  jwt = '';
  username = '';
  password = '';
  pin = '';
  loading = false;
  error = '';
  displayName = 'Kodvian';
  logoUrl = '';
  primaryColor = '#1f7f57';
  secondaryColor = '#27313f';
  showDevSection = isDevMode();
  readonly allowedBoRoles = ['Supervisor', 'Admin', 'Manager'];

  constructor(
    private readonly auth: AuthJwtService,
    private readonly boAuth: BoAuthService,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
    private readonly branding: BrandingService
  ) {
    void this.loadBranding();
    const reason = this.route.snapshot.queryParamMap.get('reason');
    if (reason === 'forbidden-role') {
      this.error = 'Tu rol no tiene acceso al Backoffice. Ingresá por POS con tu usuario de operador.';
    }

    const role = this.auth.getRole();
    if (this.auth.isTokenValid() && role && this.allowedBoRoles.includes(role)) {
      void this.router.navigateByUrl('/bo/dashboard-gerencial');
      return;
    }

    if (!this.error && this.auth.isTokenValid() && role && !this.allowedBoRoles.includes(role)) {
      this.error = 'Tenés una sesion activa de operador sin acceso a Backoffice. Volvé al inicio operativo.';
    }
  }

  async loadBranding(): Promise<void> {
    try {
      const b = await this.branding.load();
      this.displayName = b.displayName || this.displayName;
      this.logoUrl = b.logoUrl || '';
      this.primaryColor = b.primaryColor || this.primaryColor;
      this.secondaryColor = b.secondaryColor || this.secondaryColor;
    } catch {
    }
  }

  isFormValid(): boolean {
    return this.username.trim() !== '' && this.password.trim() !== '' && this.pin.trim() !== '';
  }

  loginWithJwt(): void {
    if (!this.jwt.trim()) return;
    this.auth.setToken(this.jwt.trim());
    void this.router.navigateByUrl('/bo/dashboard-gerencial');
  }

  goInicio(): void {
    void this.router.navigateByUrl('/inicio');
  }

  async loginWithCredentials(): Promise<void> {
    this.error = '';
    if (!this.username.trim() || !this.password.trim() || !this.pin.trim()) {
      this.error = 'Completá usuario, contraseña y PIN';
      return;
    }

    this.loading = true;
    try {
      const session = await this.boAuth.boLogin(this.username.trim(), this.password.trim(), this.pin.trim());
      if (!this.allowedBoRoles.includes(session.role)) {
        this.boAuth.logout();
        this.error = 'Este usuario es Operador y no tiene acceso a Backoffice. Ingresá desde POS.';
        return;
      }
      void this.router.navigateByUrl('/bo/dashboard-gerencial');
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo iniciar sesión';
    } finally {
      this.loading = false;
    }
  }
}
