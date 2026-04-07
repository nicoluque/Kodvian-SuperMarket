import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiConfigService } from './api.config';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly jwtKey = 'bo_jwt';

  constructor(private readonly http: HttpClient, private readonly api: ApiConfigService) {}

  setJwt(jwt: string): void {
    localStorage.setItem(this.jwtKey, jwt);
  }

  getJwt(): string | null {
    return localStorage.getItem(this.jwtKey);
  }

  clearJwt(): void {
    localStorage.removeItem(this.jwtKey);
  }

  async loginBackoffice(credentials: { username: string; password: string }): Promise<unknown> {
    return firstValueFrom(this.http.post(`${this.api.boBase}/auth/login`, credentials));
  }
}
