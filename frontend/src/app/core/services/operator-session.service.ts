import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { DialogService } from './dialog.service';

export interface OperatorSessionResponse {
  sessionToken: string;
  expiresAt: string;
  usuarioId: number;
  username: string;
  role: string;
}

@Injectable({ providedIn: 'root' })
export class OperatorSessionService {
  private readonly sessionKey = 'pos_operator_session';
  private readonly usernameKey = 'pos_operator_username';
  private readonly roleKey = 'pos_operator_role';
  private readonly apiBase = '/api/v1/auth';

  constructor(private readonly http: HttpClient, private readonly dialog: DialogService) {}

  getSessionToken(): string | null {
    return localStorage.getItem(this.sessionKey);
  }

  clearSession(): void {
    localStorage.removeItem(this.sessionKey);
    localStorage.removeItem(this.usernameKey);
    localStorage.removeItem(this.roleKey);
  }

  getOperatorName(): string {
    return localStorage.getItem(this.usernameKey) ?? 'Operadora';
  }

  getOperatorRole(): string | null {
    return localStorage.getItem(this.roleKey);
  }

  setActiveSession(session: OperatorSessionResponse): void {
    localStorage.setItem(this.sessionKey, session.sessionToken);
    localStorage.setItem(this.usernameKey, session.username);
    localStorage.setItem(this.roleKey, session.role);
  }

  async requestPin(): Promise<string> {
    const pin = await this.dialog.prompt({
      title: 'Verificacion de operador',
      message: 'Ingresa tu PIN para confirmar identidad.',
      inputLabel: 'PIN del operador',
      inputPlaceholder: 'PIN (4 a 6 digitos)',
      inputType: 'password',
      inputMode: 'numeric',
      inputMinLength: 4,
      inputMaxLength: 6,
      inputPattern: '^[0-9]{4,6}$',
      inputDigitsOnly: true,
      inputErrorMessage: 'El PIN debe tener entre 4 y 6 numeros.',
      yesLabel: 'Confirmar',
      noLabel: 'Cancelar',
      inputRequired: true
    });
    if (!pin) {
      throw new Error('PIN requerido');
    }
    return pin;
  }

  async ensureSession(credentials?: { username?: string; password?: string; pin?: string }): Promise<OperatorSessionResponse> {
    const providedPin = credentials?.pin?.trim();
    const pin = providedPin && providedPin.length > 0 ? providedPin : await this.requestPin();
    const deviceToken = localStorage.getItem('pos_device_token');
    const hasCredentials = !!(credentials?.username && credentials?.password);

    if (!deviceToken) {
      throw new Error('Device token no configurado');
    }

    if (!hasCredentials) {
      const activeSession = this.getSessionToken();
      if (!activeSession) {
        throw new Error('No hay sesion activa. Inicia sesion nuevamente.');
      }

      return this.refresh(pin);
    }

    const headers = new HttpHeaders({
      'X-Device-Token': deviceToken
    });

    const body = {
      pin,
      ...(credentials?.username ? { username: credentials.username } : {}),
      ...(credentials?.password ? { password: credentials.password } : {})
    };

    const response = await firstValueFrom(
      this.http.post<OperatorSessionResponse>(`${this.apiBase}/operator-session`, body, { headers })
    );

    this.setActiveSession(response);
    return response;
  }

  async refresh(pin?: string): Promise<OperatorSessionResponse> {
    const deviceToken = localStorage.getItem('pos_device_token');
    const operatorSession = this.getSessionToken();

    if (!deviceToken || !operatorSession) {
      throw new Error('No hay sesión de operador para refrescar');
    }

    const headers = new HttpHeaders({
      'X-Device-Token': deviceToken,
      'X-Operator-Session': operatorSession
    });

    let response: OperatorSessionResponse;
    try {
      response = await firstValueFrom(
        this.http.post<OperatorSessionResponse>(
          `${this.apiBase}/operator-session/refresh`,
          pin && pin.trim().length > 0 ? { pin: pin.trim() } : {},
          { headers }
        )
      );
    } catch (err: any) {
      if (err?.status === 401) {
        this.clearSession();
      }
      throw err;
    }

    this.setActiveSession(response);
    return response;
  }

  async createSession(username: string, password: string, pin?: string): Promise<OperatorSessionResponse> {
    return this.ensureSession({ username, password, pin });
  }

  async refreshSession(): Promise<OperatorSessionResponse> {
    return this.refresh();
  }
}
