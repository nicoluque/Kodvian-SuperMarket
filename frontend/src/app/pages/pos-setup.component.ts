import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { OperatingModeService } from '../core/services/operating-mode.service';
import { OperatorSessionService } from '../core/services/operator-session.service';

@Component({
  standalone: true,
  selector: 'app-pos-setup',
  imports: [CommonModule, FormsModule],
  template: `
    <main class="setup-container">
      <div class="hero-bg">
        <div class="hero-shape shape-1"></div>
        <div class="hero-shape shape-2"></div>
      </div>

      <header class="hero">
        <div class="hero-content">
          <div class="hero-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="3"></circle>
              <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"></path>
            </svg>
          </div>
          <h1>Configuracion POS</h1>
          <p class="hero-subtitle">Vincula este dispositivo con una caja o tablet usando el token</p>
        </div>
      </header>

      <section class="content-section">
        <div class="alert warning" *ngIf="reasonMessage">
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="12" y1="8" x2="12" y2="12"></line>
            <line x1="12" y1="16" x2="12.01" y2="16"></line>
          </svg>
          {{ reasonMessage }}
        </div>

        <div class="recovery-overlay" *ngIf="showRecoveryGuide">
          <article class="recovery-modal">
            <h3>{{ recoveryTitle }}</h3>
            <p>Segui estos pasos para volver a operar sin soporte tecnico.</p>
            <ol>
              <li *ngFor="let step of recoverySteps">{{ step }}</li>
            </ol>
            <div class="recovery-actions">
              <button class="btn-primary" (click)="applyRecoverySuggestion()" [disabled]="validating">{{ validating ? 'Reactivando...' : 'Reactivar ahora' }}</button>
              <button class="btn-demo" (click)="dismissRecoveryGuide()" [disabled]="validating">Cerrar guia</button>
            </div>
          </article>
        </div>

        <div class="card">
          <div class="card-header">
            <h2>
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect>
                <line x1="8" y1="21" x2="16" y2="21"></line>
                <line x1="12" y1="17" x2="12" y2="21"></line>
              </svg>
              Token del dispositivo
            </h2>
          </div>
          
          <div class="card-body">
            <p class="hint-text">No es el PIN del operador. Es el token que identifica este equipo.</p>
            
            <div class="form-group">
              <label>Token</label>
              <input type="text" [(ngModel)]="deviceToken" placeholder="Ej: demo-device-caja" />
            </div>

            <div class="demo-buttons">
              <button class="btn-demo" (click)="useDemoCaja()" [disabled]="validating">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="2" y="5" width="20" height="14" rx="2"></rect>
                  <line x1="2" y1="10" x2="22" y2="10"></line>
                </svg>
                Token demo Caja
              </button>
              <button class="btn-demo" (click)="useDemoTablet()" [disabled]="validating">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="4" y="2" width="16" height="20" rx="2" ry="2"></rect>
                  <line x1="12" y1="18" x2="12.01" y2="18"></line>
                </svg>
                Token demo Tablet
              </button>
            </div>

            <button class="btn-primary" (click)="save()" [disabled]="validating">
              <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"></path>
                <polyline points="17 21 17 13 7 13 7 21"></polyline>
                <polyline points="7 3 7 8 15 8"></polyline>
              </svg>
              {{ validating ? 'Validando...' : 'Guardar' }}
            </button>

            <div class="success-message" *ngIf="saved">
              <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
                <polyline points="22 4 12 14.01 9 11.01"></polyline>
              </svg>
              <span>Token guardado</span>
              <span *ngIf="modeLabel" class="mode-badge">Modo: {{ modeLabel }}</span>
            </div>

            <div class="error-message" *ngIf="error">
              <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="10"></circle>
                <line x1="15" y1="9" x2="9" y2="15"></line>
                <line x1="9" y1="9" x2="15" y2="15"></line>
              </svg>
              {{ error }}
            </div>
          </div>
        </div>

        <div class="quick-actions">
          <h3>Accesos rapidos</h3>
          <div class="action-buttons">
            <button class="btn-action" (click)="goCaja()">
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="2" y="5" width="20" height="14" rx="2"></rect>
                <line x1="2" y1="10" x2="22" y2="10"></line>
              </svg>
              <span>Ir a Caja</span>
            </button>
            <button class="btn-action" (click)="goTablet()">
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="4" y="2" width="16" height="20" rx="2" ry="2"></rect>
                <line x1="12" y1="18" x2="12.01" y2="18"></line>
              </svg>
              <span>Ir a Tablet</span>
            </button>
            <button class="btn-action" (click)="goBackoffice()">
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
                <polyline points="9 22 9 12 15 12 15 22"></polyline>
              </svg>
              <span>Ir a Administración</span>
            </button>
          </div>
        </div>
      </section>
    </main>
  `,
  styles: [`
    .setup-container {
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
      height: 300px;
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
      width: 380px;
      height: 380px;
      background: #BFEBF1;
      top: -140px;
      right: -100px;
      opacity: 0.2;
    }

    .shape-2 {
      width: 280px;
      height: 280px;
      background: #a8d8e0;
      bottom: -100px;
      left: -60px;
      opacity: 0.25;
    }

    .hero {
      position: relative;
      z-index: 1;
      padding: 2.5rem 1.5rem 2rem;
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

    .alert.warning {
      background: #fff3cd;
      color: #856404;
    }

    .recovery-overlay {
      position: fixed;
      inset: 0;
      background: rgba(13, 28, 24, 0.45);
      display: grid;
      place-items: center;
      z-index: 130;
      padding: 1rem;
    }

    .recovery-modal {
      width: min(560px, 96vw);
      background: #ffffff;
      border: 1px solid #cde1d8;
      border-radius: 14px;
      box-shadow: 0 18px 46px rgba(0, 0, 0, 0.2);
      padding: 1rem 1.1rem;
      display: grid;
      gap: 0.7rem;
    }

    .recovery-modal h3 {
      margin: 0;
      color: #1b4d3e;
      font-size: 1.05rem;
    }

    .recovery-modal p {
      margin: 0;
      color: #355b4f;
      font-size: 0.9rem;
    }

    .recovery-modal ol {
      margin: 0;
      padding-left: 1.1rem;
      color: #27493f;
      display: grid;
      gap: 0.35rem;
      font-size: 0.88rem;
    }

    .recovery-actions {
      display: flex;
      gap: 0.6rem;
      flex-wrap: wrap;
    }

    .card {
      background: #FFFFFF;
      border-radius: 16px;
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
      overflow: hidden;
    }

    .card-header {
      padding: 1.25rem 1.5rem;
      border-bottom: 1px solid #e9ecef;
    }

    .card-header h2 {
      display: flex;
      align-items: center;
      gap: 0.6rem;
      font-size: 1.1rem;
      font-weight: 600;
      color: #1B4D3E;
      margin: 0;
    }

    .card-header h2 svg {
      color: #BFEBF1;
      background: #1B4D3E;
      padding: 4px;
      border-radius: 6px;
    }

    .card-body {
      padding: 1.5rem;
    }

    .hint-text {
      font-size: 0.85rem;
      color: #6c757d;
      margin: 0 0 1.25rem 0;
    }

    .form-group {
      margin-bottom: 1rem;
    }

    .form-group label {
      display: block;
      font-size: 0.85rem;
      font-weight: 500;
      color: #495057;
      margin-bottom: 0.4rem;
    }

    .form-group input {
      width: 100%;
      padding: 0.75rem 1rem;
      font-size: 1rem;
      border: 2px solid #e9ecef;
      border-radius: 10px;
      background: #f8fafc;
      color: #1B4D3E;
      transition: all 0.2s ease;
    }

    .form-group input:focus {
      outline: none;
      border-color: #BFEBF1;
      background: #FFFFFF;
      box-shadow: 0 0 0 4px rgba(191, 235, 241, 0.2);
    }

    .form-group input::placeholder {
      color: #9EABB1;
    }

    .demo-buttons {
      display: flex;
      gap: 0.75rem;
      margin-bottom: 1.25rem;
    }

    .btn-demo {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding: 0.6rem 0.75rem;
      background: #e9ecef;
      color: #495057;
      border: none;
      border-radius: 8px;
      font-size: 0.8rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .btn-demo:hover:not(:disabled) {
      background: #dee2e6;
    }

    .btn-demo:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .btn-primary {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      width: 100%;
      padding: 0.85rem 1rem;
      background: #1B4D3E;
      color: #FFFFFF;
      border: none;
      border-radius: 10px;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .btn-primary:hover:not(:disabled) {
      background: #234F45;
      transform: translateY(-1px);
    }

    .btn-primary:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .success-message {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin-top: 1rem;
      padding: 0.75rem 1rem;
      background: #d4edda;
      color: #155724;
      border-radius: 8px;
      font-size: 0.9rem;
      font-weight: 500;
    }

    .mode-badge {
      margin-left: auto;
      padding: 0.2rem 0.5rem;
      background: #155724;
      color: #FFFFFF;
      border-radius: 4px;
      font-size: 0.75rem;
    }

    .error-message {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin-top: 1rem;
      padding: 0.75rem 1rem;
      background: #f8d7da;
      color: #721c24;
      border-radius: 8px;
      font-size: 0.9rem;
      font-weight: 500;
    }

    .quick-actions {
      margin-top: 1.5rem;
    }

    .quick-actions h3 {
      font-size: 0.85rem;
      text-transform: uppercase;
      letter-spacing: 0.08em;
      color: #9EABB1;
      margin: 0 0 0.75rem 0;
      font-weight: 600;
    }

    .action-buttons {
      display: flex;
      gap: 0.75rem;
      flex-wrap: wrap;
    }

    .btn-action {
      flex: 1;
      min-width: 140px;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding: 0.75rem 1rem;
      background: #FFFFFF;
      color: #1B4D3E;
      border: 1px solid #e9ecef;
      border-radius: 10px;
      font-size: 0.9rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
      text-decoration: none;
    }

    .btn-action:hover {
      background: #1B4D3E;
      color: #FFFFFF;
      border-color: #1B4D3E;
    }

    @media (max-width: 640px) {
      .hero {
        padding: 2rem 1rem 1.5rem;
      }

      .hero h1 {
        font-size: 1.5rem;
      }

      .content-section {
        padding: 0 1rem;
      }

      .demo-buttons {
        flex-direction: column;
      }

      .action-buttons {
        flex-direction: column;
      }

      .btn-action {
        width: 100%;
      }
    }
  `]
})
export class PosSetupComponent {
  deviceToken = localStorage.getItem('pos_device_token') ?? '';
  saved = false;
  validating = false;
  error = '';
  modeLabel = '';
  reasonMessage = '';
  showRecoveryGuide = false;
  recoveryTitle = 'Reactivacion operativa';
  recoverySteps: string[] = [];

  private get target(): 'caja' | 'tablet' | 'auto' {
    const raw = this.route.snapshot.queryParamMap.get('target');
    if (raw === 'caja' || raw === 'tablet') return raw;
    return 'auto';
  }

  constructor(
    private readonly router: Router,
    private readonly route: ActivatedRoute,
    private readonly http: HttpClient,
    private readonly operatingMode: OperatingModeService,
    private readonly operatorSessionService: OperatorSessionService
  ) {
    sessionStorage.removeItem('pos_recovery_redirecting');
    this.loadQueryHints();
    void this.tryAutoContinue();
  }

  private loadQueryHints(): void {
    const reason = this.route.snapshot.queryParamMap.get('reason');
    const prefill = this.route.snapshot.queryParamMap.get('prefill');
    const guide = this.route.snapshot.queryParamMap.get('guide') === '1';

    if (!this.deviceToken && prefill) {
      this.deviceToken = prefill;
    }

    if (reason === 'missing') {
      if (this.target === 'caja') {
        this.reasonMessage = 'Para entrar a Caja primero tenes que vincular una caja fisica.';
      } else if (this.target === 'tablet') {
        this.reasonMessage = 'Para entrar a Totem/Tablet primero tenes que vincular un dispositivo tablet.';
      } else {
        this.reasonMessage = 'Primero tenes que vincular este dispositivo POS.';
      }
      return;
    }

    if (reason === 'invalid') {
      this.reasonMessage = 'El token guardado vencio o no es valido. Reingresalo para continuar.';
      return;
    }

    if (reason === 'device_mismatch') {
      this.reasonMessage = this.target === 'caja'
        ? 'El dispositivo actual es tablet. Para Caja necesitas token de caja fisica.'
        : 'El dispositivo actual es caja. Para Totem/Tablet necesitas token de tablet.';
    }

    if (guide) {
      this.showRecoveryGuide = true;
      this.configureRecoveryGuide(reason);
    }
  }

  private configureRecoveryGuide(reason: string | null): void {
    if (reason === 'invalid') {
      this.recoveryTitle = 'Token de dispositivo invalido';
      this.recoverySteps = [
        'Selecciona el token correcto del equipo (Caja o Tablet).',
        'Guarda y valida el token del dispositivo.',
        'Inicia sesion del operador para continuar.'
      ];
      return;
    }

    if (reason === 'device_mismatch') {
      this.recoveryTitle = 'Dispositivo en modo incorrecto';
      this.recoverySteps = [
        'Este equipo no coincide con el modulo actual.',
        'Selecciona el token sugerido para este flujo.',
        'Guarda configuracion y vuelve a operar.'
      ];
      return;
    }

    if (reason === 'missing') {
      this.recoveryTitle = 'Falta token de dispositivo';
      this.recoverySteps = [
        'Selecciona un token valido para este equipo.',
        'Guarda y valida el token.',
        'Inicia sesion del operador y retoma la venta.'
      ];
      return;
    }

    this.recoveryTitle = 'Sesion operativa vencida';
    this.recoverySteps = [
      'Verifica token del dispositivo y valida configuracion.',
      'Inicia sesion con el operador correcto para este equipo.',
      'Si persiste, revisa que no haya otra sesion activa con el mismo usuario.'
    ];
  }

  dismissRecoveryGuide(): void {
    this.showRecoveryGuide = false;
  }

  async applyRecoverySuggestion(): Promise<void> {
    if (this.target === 'tablet') {
      this.useDemoTablet();
    } else if (this.target === 'caja') {
      this.useDemoCaja();
    }

    await this.save();
  }

  private async tryAutoContinue(): Promise<void> {
    const forceSetup = this.route.snapshot.queryParamMap.get('forceSetup') === '1';
    const token = this.deviceToken.trim();
    if (forceSetup || !token) return;

    this.validating = true;
    try {
      const validation = await firstValueFrom(this.http.get('/api/v1/auth/device/validate', {
        headers: new HttpHeaders({ 'X-Device-Token': token })
      }));

      this.operatingMode.setFromDeviceValidation(validation);
      this.modeLabel = this.operatingMode.getModeLabel(this.operatingMode.getConfig().mode);
      const mismatchError = this.validateTargetDevice(validation as any);
      if (mismatchError) {
        this.error = mismatchError;
        localStorage.removeItem('pos_device_token');
        localStorage.removeItem('pos_device_token_validated');
        localStorage.removeItem('operating_device_type');
        return;
      }
      this.saved = true;
      localStorage.setItem('pos_device_token', token);
      localStorage.setItem('pos_device_token_validated', token);
      void this.router.navigateByUrl(this.resolveTargetRoute());
    } catch {
      localStorage.removeItem('pos_device_token');
      localStorage.removeItem('pos_device_token_validated');
      this.deviceToken = '';
      this.error = 'El token guardado no es valido. Reconfigura el dispositivo.';
    } finally {
      this.validating = false;
    }
  }

  async save(): Promise<void> {
    this.error = '';
    this.saved = false;
    const token = this.deviceToken.trim();
    if (!token) {
      this.error = 'Ingresa un token de dispositivo valido';
      return;
    }

    if (/^\d{4}$/.test(token)) {
      this.error = 'Ese valor parece un PIN de operador. Aca va el token del dispositivo (ejemplo: demo-device-caja).';
      return;
    }

    this.validating = true;
    try {
      const validation = await firstValueFrom(this.http.get('/api/v1/auth/device/validate', {
        headers: new HttpHeaders({ 'X-Device-Token': token })
      }));

      this.operatingMode.setFromDeviceValidation(validation);
      this.modeLabel = this.operatingMode.getModeLabel(this.operatingMode.getConfig().mode);
      const mismatchError = this.validateTargetDevice(validation as any);
      if (mismatchError) {
        this.error = mismatchError;
        localStorage.removeItem('pos_device_token_validated');
        localStorage.removeItem('operating_device_type');
        return;
      }

      localStorage.setItem('pos_device_token', token);
      localStorage.setItem('pos_device_token_validated', token);
      this.operatorSessionService.clearSession();
      this.saved = true;
      void this.router.navigateByUrl(this.resolveTargetRoute());
    } catch {
      this.error = 'Token de dispositivo invalido o backend no disponible';
      localStorage.removeItem('pos_device_token_validated');
    } finally {
      this.validating = false;
    }
  }

  useDemoCaja(): void {
    this.deviceToken = 'demo-device-caja';
  }

  useDemoTablet(): void {
    this.deviceToken = 'demo-device-tablet';
  }

  goCaja(): void {
    void this.router.navigateByUrl('/pos/caja/apertura');
  }

  goTablet(): void {
    void this.router.navigateByUrl('/pos/tablet/nueva');
  }

  goBackoffice(): void {
    void this.router.navigateByUrl('/inicio/login');
  }

  private resolveTargetRoute(): string {
    if (this.target === 'caja') return '/pos/caja/apertura';
    if (this.target === 'tablet') return '/pos/tablet/nueva';
    return this.operatingMode.getPreferredPosRoute();
  }

  private validateTargetDevice(validation: { deviceType?: string }): string | null {
    const deviceType = validation?.deviceType ?? '';
    if (this.target === 'caja' && deviceType !== 'CashRegister') {
      return 'El token ingresado no corresponde a Caja fisica. Usa un token de caja.';
    }
    if (this.target === 'tablet' && deviceType !== 'Tablet') {
      return 'El token ingresado no corresponde a Totem/Tablet. Usa un token de tablet.';
    }
    return null;
  }
}
