import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { PosCajaService } from '../../core/services/pos-caja.service';
import { OperatorSessionService } from '../../core/services/operator-session.service';
import { PosModuleNavComponent } from '../../shared/components/pos-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-pos-caja-apertura',
  imports: [CommonModule, FormsModule, PosModuleNavComponent],
  template: `
    <main class="apertura-container">
      <div class="hero-bg">
        <div class="hero-shape shape-1"></div>
        <div class="hero-shape shape-2"></div>
      </div>

      <app-pos-module-nav />

      <header class="hero">
        <div class="hero-content">
          <h1>Apertura de Caja</h1>
          <p class="hero-subtitle">Inicia tu jornada comercial</p>
        </div>
      </header>

      <section class="status-section">
        <div class="status-grid">
          <article class="status-card" [class.ready]="deviceReady" [class.not-ready]="!deviceReady">
            <div class="status-icon">
              <svg *ngIf="deviceReady" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect>
                <line x1="8" y1="21" x2="16" y2="21"></line>
                <line x1="12" y1="17" x2="12" y2="21"></line>
              </svg>
              <svg *ngIf="!deviceReady" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect>
                <line x1="8" y1="21" x2="16" y2="21"></line>
                <line x1="12" y1="17" x2="12" y2="21"></line>
              </svg>
            </div>
            <div class="status-info">
              <span class="status-label">Dispositivo</span>
              <span class="status-value">{{ deviceReady ? 'Vinculado' : 'Sin validar' }}</span>
            </div>
            <div class="status-indicator" [class.active]="deviceReady"></div>
          </article>

          <article class="status-card" [class.ready]="operatorReady" [class.not-ready]="!operatorReady">
            <div class="status-icon">
              <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
                <circle cx="12" cy="7" r="4"></circle>
              </svg>
            </div>
            <div class="status-info">
              <span class="status-label">Operador</span>
              <span class="status-value">{{ operatorReady ? operatorName : 'Sin sesión activa' }}</span>
            </div>
            <div class="status-indicator" [class.active]="operatorReady"></div>
          </article>
        </div>
      </section>

      <section class="form-section">
        <div class="form-card">
          <h2 class="form-title">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
              <circle cx="12" cy="7" r="4"></circle>
            </svg>
            Datos del Operador
          </h2>
          
          <div class="form-group">
            <label for="username">Usuario</label>
            <input id="username" type="text" [(ngModel)]="username" placeholder="Ingresa tu usuario" />
          </div>
          
          <div class="form-group">
            <label for="password">Contraseña</label>
            <input id="password" type="password" [(ngModel)]="password" placeholder="Ingresa tu contraseña" />
          </div>
          
          <div class="form-group">
            <label for="pin">PIN</label>
            <input id="pin" type="password" [(ngModel)]="pin" placeholder="PIN de 4 digitos" maxlength="4" />
          </div>

          <div class="form-actions">
            <button class="btn-primary" (click)="loginOperator()" [disabled]="loading || !deviceReady">
              <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4"></path>
                <polyline points="10 17 15 12 10 7"></polyline>
                <line x1="15" y1="12" x2="3" y2="12"></line>
              </svg>
              Iniciar sesión
            </button>
            <button class="btn-secondary" (click)="refreshOperator()">
              <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="23 4 23 10 17 10"></polyline>
                <polyline points="1 20 1 14 7 14"></polyline>
                <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
              </svg>
              Refrescar
            </button>
          </div>

          <p class="hint" *ngIf="!deviceReady">
            <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"></circle>
              <line x1="12" y1="8" x2="12" y2="12"></line>
              <line x1="12" y1="16" x2="12.01" y2="16"></line>
            </svg>
            Si no está vinculado, configura el token en POS Setup
          </p>
        </div>

        <div class="form-card">
          <h2 class="form-title">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="2" y="5" width="20" height="14" rx="2"></rect>
              <line x1="2" y1="10" x2="22" y2="10"></line>
            </svg>
            Configuración de Caja
          </h2>
          
          <div class="form-group">
            <label for="shift">Turno</label>
            <select id="shift" [(ngModel)]="shift">
              <option value="Morning">Mañana</option>
              <option value="Afternoon">Tarde</option>
              <option value="Night">Noche</option>
            </select>
          </div>
          
          <div class="form-group">
            <label for="openingCash">Efectivo inicial ($)</label>
            <input id="openingCash" type="number" [(ngModel)]="openingCash" placeholder="0.00" />
          </div>

          <div class="form-actions main-action">
            <button class="btn-success btn-large" (click)="open()" [disabled]="loading || !canOpen">
              <svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
                <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
              </svg>
              {{ loading ? 'Abriendo...' : 'Abrir Caja' }}
            </button>
          </div>
        </div>

        <div class="secondary-actions">
          <button class="btn-ghost" (click)="goSetup()">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="3"></circle>
              <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"></path>
            </svg>
            Configurar POS
          </button>
        </div>

        <div class="alerts">
          <div class="alert success" *ngIf="okMessage">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
              <polyline points="22 4 12 14.01 9 11.01"></polyline>
            </svg>
            {{ okMessage }}
          </div>
          <div class="alert error" *ngIf="errorMessage">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"></circle>
              <line x1="15" y1="9" x2="9" y2="15"></line>
              <line x1="9" y1="9" x2="15" y2="15"></line>
            </svg>
            {{ errorMessage }}
          </div>
        </div>
      </section>

      <div class="modal-overlay" *ngIf="showSessionConflictModal">
        <div class="modal-card">
          <h3>Caja ya abierta</h3>
          <p>{{ sessionConflictMessage }}</p>
          <div class="modal-actions">
            <button class="btn-ghost" (click)="dismissSessionConflictModal()">Entendido</button>
            <button class="btn-secondary" (click)="goInbox()">Ir a bandeja</button>
            <button class="btn-secondary" (click)="goSetup()">Cambiar dispositivo</button>
          </div>
        </div>
      </div>
    </main>
  `,
  styles: [`
    .apertura-container {
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
      max-width: 600px;
      margin: 0 auto;
    }

    .hero-content {
      animation: fadeInUp 0.5s ease-out;
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

    .hero h1 {
      font-size: 2rem;
      font-weight: 700;
      color: #FFFFFF;
      margin: 0 0 0.4rem 0;
      letter-spacing: -0.02em;
    }

    .hero-subtitle {
      font-size: 1rem;
      color: rgba(255, 255, 255, 0.7);
      margin: 0;
    }

    .status-section {
      position: relative;
      z-index: 1;
      max-width: 600px;
      margin: 0 auto;
      padding: 0 1.5rem;
      animation: fadeInUp 0.5s ease-out 0.1s backwards;
    }

    .status-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 0.75rem;
    }

    .status-card {
      background: #FFFFFF;
      border-radius: 12px;
      padding: 1rem;
      display: flex;
      align-items: center;
      gap: 0.75rem;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
      border: 2px solid transparent;
      transition: all 0.2s ease;
    }

    .status-card.ready {
      border-color: #28a745;
      background: linear-gradient(135deg, #f0fff4 0%, #ffffff 100%);
    }

    .status-card.not-ready {
      border-color: #e9ecef;
    }

    .status-icon {
      width: 44px;
      height: 44px;
      border-radius: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .ready .status-icon {
      background: #d4edda;
      color: #28a745;
    }

    .not-ready .status-icon {
      background: #e9ecef;
      color: #6c757d;
    }

    .status-info {
      flex: 1;
      min-width: 0;
      display: flex;
      flex-direction: column;
      gap: 0.15rem;
    }

    .status-label {
      font-size: 0.75rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: #6c757d;
      font-weight: 500;
    }

    .status-value {
      font-size: 0.9rem;
      font-weight: 600;
      color: #1B4D3E;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .status-indicator {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      background: #e9ecef;
      flex-shrink: 0;
    }

    .status-indicator.active {
      background: #28a745;
      box-shadow: 0 0 8px rgba(40, 167, 69, 0.5);
    }

    .form-section {
      position: relative;
      z-index: 1;
      max-width: 600px;
      margin: 1.5rem auto 0;
      padding: 0 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 1rem;
      animation: fadeInUp 0.5s ease-out 0.2s backwards;
    }

    .form-card {
      background: #FFFFFF;
      border-radius: 16px;
      padding: 1.5rem;
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
    }

    .form-title {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 1rem;
      font-weight: 600;
      color: #1B4D3E;
      margin: 0 0 1.25rem 0;
      padding-bottom: 0.75rem;
      border-bottom: 1px solid #e9ecef;
    }

    .form-title svg {
      color: #BFEBF1;
      background: #1B4D3E;
      padding: 4px;
      border-radius: 6px;
    }

    .form-group {
      margin-bottom: 1rem;
    }

    .form-group:last-child {
      margin-bottom: 0;
    }

    .form-group label {
      display: block;
      font-size: 0.85rem;
      font-weight: 500;
      color: #495057;
      margin-bottom: 0.4rem;
    }

    .form-group input,
    .form-group select {
      width: 100%;
      padding: 0.75rem 1rem;
      font-size: 1rem;
      border: 2px solid #e9ecef;
      border-radius: 10px;
      background: #f8fafc;
      color: #1B4D3E;
      transition: all 0.2s ease;
    }

    .form-group input:focus,
    .form-group select:focus {
      outline: none;
      border-color: #BFEBF1;
      background: #FFFFFF;
      box-shadow: 0 0 0 4px rgba(191, 235, 241, 0.2);
    }

    .form-group input::placeholder {
      color: #9EABB1;
    }

    .form-actions {
      display: flex;
      gap: 0.75rem;
      margin-top: 1.25rem;
    }

    .form-actions.main-action {
      justify-content: center;
    }

    .btn-primary {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding: 0.75rem 1rem;
      background: #1B4D3E;
      color: #FFFFFF;
      border: none;
      border-radius: 10px;
      font-size: 0.95rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .btn-primary:hover:not(:disabled) {
      background: #234F45;
      transform: translateY(-1px);
    }

    .btn-primary:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .btn-secondary {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding: 0.75rem 1rem;
      background: #e9ecef;
      color: #495057;
      border: none;
      border-radius: 10px;
      font-size: 0.95rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .btn-secondary:hover {
      background: #dee2e6;
    }

    .btn-success {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.6rem;
      padding: 1rem 2rem;
      background: linear-gradient(135deg, #28a745 0%, #20c997 100%);
      color: #FFFFFF;
      border: none;
      border-radius: 12px;
      font-size: 1.1rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s ease;
      box-shadow: 0 4px 12px rgba(40, 167, 69, 0.3);
    }

    .btn-success:hover:not(:disabled) {
      transform: translateY(-2px);
      box-shadow: 0 6px 20px rgba(40, 167, 69, 0.4);
    }

    .btn-success:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .btn-large {
      padding: 1rem 3rem;
    }

    .hint {
      display: flex;
      align-items: center;
      gap: 0.4rem;
      font-size: 0.8rem;
      color: #6c757d;
      margin-top: 1rem;
      padding: 0.6rem 0.8rem;
      background: #fff3cd;
      border-radius: 8px;
    }

    .secondary-actions {
      display: flex;
      justify-content: center;
      gap: 1rem;
      margin-top: 0.5rem;
    }

    .btn-ghost {
      display: flex;
      align-items: center;
      gap: 0.4rem;
      padding: 0.6rem 1rem;
      background: transparent;
      color: #6c757d;
      border: 1px solid #e9ecef;
      border-radius: 8px;
      font-size: 0.85rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .btn-ghost:hover {
      background: #f8fafc;
      border-color: #1B4D3E;
      color: #1B4D3E;
    }

    .alerts {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      margin-top: 0.5rem;
    }

    .alert {
      display: flex;
      align-items: center;
      gap: 0.6rem;
      padding: 0.75rem 1rem;
      border-radius: 10px;
      font-size: 0.9rem;
      font-weight: 500;
    }

    .alert.success {
      background: #d4edda;
      color: #155724;
    }

    .alert.error {
      background: #f8d7da;
      color: #721c24;
    }

    .modal-overlay {
      position: fixed;
      inset: 0;
      background: rgba(8, 16, 13, 0.45);
      display: grid;
      place-items: center;
      z-index: 1200;
      padding: 12px;
    }

    .modal-card {
      width: min(520px, 95vw);
      background: #fff;
      border-radius: 12px;
      border: 1px solid #d9e6df;
      padding: 16px;
      display: grid;
      gap: 10px;
    }

    .modal-card h3 {
      margin: 0;
      color: #1B4D3E;
    }

    .modal-card p {
      margin: 0;
      color: #4c6b61;
    }

    .modal-actions {
      display: flex;
      gap: 8px;
      justify-content: flex-end;
      flex-wrap: wrap;
    }

    @media (max-width: 640px) {
      .hero {
        padding: 1.5rem 1rem 1rem;
      }

      .hero h1 {
        font-size: 1.5rem;
      }

      .status-section,
      .form-section {
        padding: 0 1rem;
      }

      .status-grid {
        grid-template-columns: 1fr;
      }

      .form-actions {
        flex-direction: column;
      }

      .secondary-actions {
        flex-direction: column;
      }

      .btn-ghost {
        justify-content: center;
      }
    }
  `]
})
export class PosCajaAperturaComponent {
  username = '';
  password = '';
  pin = '';
  shift = 'Morning';
  openingCash = 0;
  loading = false;
  okMessage = '';
  errorMessage = '';
  deviceReady = false;
  operatorReady = false;
  operatorName = '';
  currentOperatorUsuarioId: number | null = null;
  showSessionConflictModal = false;
  sessionConflictMessage = '';

  get canOpen(): boolean {
    return this.deviceReady && this.operatorReady && !this.showSessionConflictModal;
  }

  constructor(
    private readonly api: PosCajaService,
    private readonly operatorSessionService: OperatorSessionService,
    private readonly http: HttpClient,
    private readonly router: Router
  ) {
    void this.bootstrap();
  }

  private async bootstrap(): Promise<void> {
    await this.validateDevice();
    await this.restoreOperatorSession();
    await this.checkCurrentCashSessionOwnership();
  }

  private async validateDevice(): Promise<void> {
    const token = localStorage.getItem('pos_device_token');
    if (!token) {
      this.deviceReady = false;
      this.errorMessage = 'Dispositivo sin token. Configura POS antes de abrir caja.';
      return;
    }

    try {
      await firstValueFrom(this.http.get('/api/v1/auth/device/validate', {
        headers: new HttpHeaders({ 'X-Device-Token': token })
      }));
      localStorage.setItem('pos_device_token_validated', token);
      this.deviceReady = true;
    } catch {
      this.deviceReady = false;
      localStorage.removeItem('pos_device_token');
      localStorage.removeItem('pos_device_token_validated');
      this.operatorSessionService.clearSession();
      this.errorMessage = 'Token de dispositivo invalido. Reconfigura POS.';
    }
  }

  private async restoreOperatorSession(): Promise<void> {
    const token = this.operatorSessionService.getSessionToken();
    if (!token) {
      this.operatorReady = false;
      this.operatorName = '';
      return;
    }

    try {
      const refreshed = await this.operatorSessionService.refresh();
      this.operatorReady = true;
      this.operatorName = refreshed.username;
      this.currentOperatorUsuarioId = refreshed.usuarioId;
      this.okMessage = `Sesion activa: ${refreshed.username}`;
    } catch {
      this.operatorSessionService.clearSession();
      this.operatorReady = false;
      this.operatorName = '';
      this.currentOperatorUsuarioId = null;
    }
  }

  async loginOperator(): Promise<void> {
    this.errorMessage = '';
    this.okMessage = '';
    if (!this.deviceReady) {
      this.errorMessage = 'Primero valida el dispositivo en POS Setup.';
      return;
    }
    try {
      const session = await this.operatorSessionService.ensureSession({ username: this.username, password: this.password, pin: this.pin });
      this.okMessage = `Operador activo: ${session.username}`;
      this.operatorReady = true;
      this.operatorName = session.username;
      this.currentOperatorUsuarioId = session.usuarioId;
      await this.checkCurrentCashSessionOwnership();
    } catch (err: any) {
      this.operatorReady = false;
      this.operatorName = '';
      this.currentOperatorUsuarioId = null;
      this.errorMessage = err?.error?.message ?? err?.message ?? 'No se pudo iniciar sesión operador';
    }
  }

  async refreshOperator(): Promise<void> {
    this.errorMessage = '';
    try {
      const session = await this.operatorSessionService.refresh();
      this.okMessage = 'Sesión de operador refrescada';
      this.operatorReady = true;
      this.operatorName = session.username;
      this.currentOperatorUsuarioId = session.usuarioId;
      await this.checkCurrentCashSessionOwnership();
    } catch (err: any) {
      this.operatorReady = false;
      this.operatorName = '';
      this.currentOperatorUsuarioId = null;
      this.errorMessage = err?.error?.message ?? err?.message ?? 'No se pudo refrescar sesión';
    }
  }

  async open(): Promise<void> {
    this.loading = true;
    this.errorMessage = '';
    this.okMessage = '';
    if (!this.deviceReady) {
      this.errorMessage = 'Dispositivo no validado. Configura POS para continuar.';
      this.loading = false;
      return;
    }
    if (!this.operatorReady) {
      this.errorMessage = 'Inicia sesion de operador antes de abrir caja.';
      this.loading = false;
      return;
    }

    const blockedByOpenSession = await this.checkCurrentCashSessionOwnership();
    if (blockedByOpenSession) {
      this.loading = false;
      return;
    }

    try {
      const session = await this.api.openCashSession(this.shift, Number(this.openingCash || 0));
      this.okMessage = `Caja abierta. Session #${session.id}`;
      void this.router.navigateByUrl('/pos/caja/inbox');
    } catch (err: any) {
      const backendMessage = err?.error?.message ?? 'No se pudo abrir caja';
      if (typeof backendMessage === 'string' && backendMessage.includes("There's already an open cash session")) {
        this.showSessionConflict('Ya hay una caja abierta en este dispositivo. Para seguridad, no se permite abrir otra ni cambiar de operador con sesion activa.');
      } else {
        this.errorMessage = backendMessage;
      }
    } finally {
      this.loading = false;
    }
  }

  goInbox(): void {
    void this.router.navigateByUrl('/pos/caja/inbox');
  }

  goSetup(): void {
    void this.router.navigateByUrl('/pos/setup?forceSetup=1&target=caja');
  }

  dismissSessionConflictModal(): void {
    this.showSessionConflictModal = false;
  }

  private async checkCurrentCashSessionOwnership(): Promise<boolean> {
    if (!this.deviceReady || !this.operatorReady) {
      this.showSessionConflictModal = false;
      return false;
    }

    try {
      const current = await this.api.getCurrentCashSession();
      if (!current?.id) {
        this.showSessionConflictModal = false;
        return false;
      }

      const ownerId = current.openedByUsuarioId;
      const ownerName = current.openedByUsername;
      if (ownerId && this.currentOperatorUsuarioId && ownerId !== this.currentOperatorUsuarioId) {
        this.showSessionConflict(`La caja ya esta abierta por ${ownerName ?? 'otro operador'}. Por seguridad, no se permite continuar con un operador distinto.`);
        return true;
      }

      if (ownerId && this.currentOperatorUsuarioId && ownerId === this.currentOperatorUsuarioId) {
        this.showSessionConflict('Ya tenes una caja abierta en este dispositivo. Continua desde bandeja o cierra la sesion actual antes de abrir otra.');
        return true;
      }

      if (!ownerId && this.currentOperatorUsuarioId) {
        this.showSessionConflict('Ya hay una caja abierta en este dispositivo. Continua en bandeja o cierra la sesion antes de cambiar de operador.');
        return true;
      }

      this.showSessionConflictModal = false;
      return false;
    } catch {
      this.showSessionConflictModal = false;
      return false;
    }
  }

  private showSessionConflict(message: string): void {
    this.sessionConflictMessage = message;
    this.showSessionConflictModal = true;
  }
}
