import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthJwtService } from './auth-jwt.service';

interface BoLoginResponse {
  sessionToken: string;
  expiresAt: string;
  usuarioId: number;
  username: string;
  role: string;
}

@Injectable({ providedIn: 'root' })
export class BoAuthService {
  constructor(private readonly http: HttpClient, private readonly auth: AuthJwtService) {}

  async login(username: string, password: string, pin: string): Promise<BoLoginResponse> {
    const response = await firstValueFrom(this.http.post<BoLoginResponse>('/api/v1/auth/login', {
      username,
      password,
      pin
    }));

    this.auth.setSession({
      token: response.sessionToken,
      role: response.role,
      username: response.username,
      expiresAt: response.expiresAt
    });

    return response;
  }

  async boLogin(username: string, password: string, pin: string): Promise<BoLoginResponse> {
    const response = await firstValueFrom(this.http.post<BoLoginResponse>('/api/v1/auth/bo-login', {
      username,
      password,
      pin
    }));

    this.auth.setSession({
      token: response.sessionToken,
      role: response.role,
      username: response.username,
      expiresAt: response.expiresAt
    });

    return response;
  }

  logout(): void {
    this.auth.clearToken();
  }
}
