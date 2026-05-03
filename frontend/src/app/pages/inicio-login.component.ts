import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthJwtService } from '../core/services/auth-jwt.service';
import { BoAuthService } from '../core/services/bo-auth.service';
import { OperatorSessionService } from '../core/services/operator-session.service';

@Component({
  standalone: true,
  selector: 'app-inicio-login',
  imports: [CommonModule, FormsModule],
  template: `
    <main class="login-wrap">
      <section class="login-card">
        <h1>Ingreso operativo</h1>
        <p>Inicia sesion para habilitar modulos segun tu cargo.</p>

        <label>Usuario
          <input type="text" [(ngModel)]="username" [disabled]="loading" />
        </label>

        <label>Contrasena
          <input type="password" [(ngModel)]="password" [disabled]="loading" />
        </label>

        <label>PIN
          <input type="password" [(ngModel)]="pin" maxlength="8" [disabled]="loading" />
        </label>

        <div class="warning" *ngIf="!hasDeviceToken">
          No hay dispositivo POS configurado. Para operar POS necesitas configurar token.
          <button type="button" (click)="goSetup()" [disabled]="loading">Configurar dispositivo</button>
        </div>

        <button class="btn-primary" (click)="login()" [disabled]="loading || !isValid()">Ingresar</button>

        <p class="ok" *ngIf="message">{{ message }}</p>
        <p class="error" *ngIf="error">{{ error }}</p>
      </section>
    </main>
  `,
  styles: [
    `.login-wrap{min-height:100vh;display:grid;place-items:center;padding:16px;background:linear-gradient(145deg,#eef6f2,#d7ebe2)}`,
    `.login-card{width:min(460px,92vw);background:#fff;border:1px solid #d6e7de;border-radius:14px;padding:18px;display:grid;gap:10px}`,
    `h1{margin:0;color:#1B4D3E}`,
    `p{margin:0;color:#4f6f65}`,
    `label{display:grid;gap:6px;color:#2e5248;font-weight:600}`,
    `input{border:1px solid #cfe0d8;border-radius:8px;padding:8px 10px}`,
    `.warning{background:#fff6dd;border:1px solid #f0dca5;border-radius:8px;padding:10px;color:#6d5a20;display:grid;gap:8px}`,
    `.warning button{justify-self:start}`,
    `.btn-primary{background:#1B4D3E;color:#fff;border:1px solid #1B4D3E;border-radius:8px;padding:10px 12px;cursor:pointer}`,
    `.ok{color:#17663f}`,
    `.error{color:#8f1d22}`
  ]
})
export class InicioLoginComponent {
  username = '';
  password = '';
  pin = '';
  loading = false;
  message = '';
  error = '';

  constructor(
    private readonly router: Router,
    private readonly boAuth: BoAuthService,
    private readonly authJwt: AuthJwtService,
    private readonly operatorSession: OperatorSessionService
  ) {
    sessionStorage.removeItem('bo_recovery_redirecting');
  }

  get hasDeviceToken(): boolean {
    return !!localStorage.getItem('pos_device_token');
  }

  isValid(): boolean {
    return this.username.trim().length > 0 && this.password.trim().length > 0 && this.pin.trim().length > 0;
  }

  goSetup(): void {
    void this.router.navigateByUrl('/pos/setup?reason=missing&prefill=demo-device-caja');
  }

  async login(): Promise<void> {
    if (!this.isValid()) return;

    this.loading = true;
    this.error = '';
    this.message = '';

    try {
      const bo = await this.boAuth.login(this.username.trim(), this.password, this.pin.trim());
      const role = bo.role;

      if (role === 'Operator' && !this.hasDeviceToken) {
        this.boAuth.logout();
        this.error = 'Operador necesita dispositivo configurado para iniciar operacion.';
        return;
      }

      if (this.hasDeviceToken) {
        try {
          await this.operatorSession.createSession(this.username.trim(), this.password, this.pin.trim());
        } catch (err: any) {
          if (role === 'Operator') {
            this.boAuth.logout();
            this.error = err?.error?.message ?? 'No se pudo crear sesion operativa para POS';
            return;
          }
          this.message = 'Sesion BO iniciada. POS requiere verificar token o PIN para operar.';
        }
      }

      if (!this.authJwt.isTokenValid()) {
        this.error = 'No se pudo iniciar sesion';
        return;
      }

      void this.router.navigateByUrl('/inicio');
    } catch (err: any) {
      this.error = err?.error?.message ?? 'Credenciales invalidas';
    } finally {
      this.loading = false;
    }
  }
}
