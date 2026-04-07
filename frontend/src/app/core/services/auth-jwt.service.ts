import { Injectable } from '@angular/core';

export interface JwtPayload {
  role?: string;
  exp?: number;
  [key: string]: unknown;
}

export interface BoSessionData {
  token: string;
  role?: string;
  username?: string;
  expiresAt?: string;
}

@Injectable({ providedIn: 'root' })
export class AuthJwtService {
  private readonly tokenKey = 'bo_jwt';
  private readonly roleKey = 'bo_role';
  private readonly usernameKey = 'bo_username';
  private readonly expiresAtKey = 'bo_expires_at';

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  setToken(token: string): void {
    localStorage.setItem(this.tokenKey, token);
  }

  setSession(session: BoSessionData): void {
    localStorage.setItem(this.tokenKey, session.token);
    if (session.role) localStorage.setItem(this.roleKey, session.role);
    if (session.username) localStorage.setItem(this.usernameKey, session.username);
    if (session.expiresAt) localStorage.setItem(this.expiresAtKey, session.expiresAt);
  }

  clearToken(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.roleKey);
    localStorage.removeItem(this.usernameKey);
    localStorage.removeItem(this.expiresAtKey);
  }

  isAuthenticated(): boolean {
    return this.isTokenValid();
  }

  isTokenValid(): boolean {
    const token = this.getToken();
    if (!token) return false;

    const payload = this.getPayload();
    if (!payload) {
      const expiresAtRaw = localStorage.getItem(this.expiresAtKey);
      if (!expiresAtRaw) return true;
      const ms = Date.parse(expiresAtRaw);
      if (Number.isNaN(ms)) return true;
      return ms > Date.now();
    }

    const exp = payload['exp'];
    if (typeof exp !== 'number') return true;

    const now = Math.floor(Date.now() / 1000);
    return exp > now;
  }

  getRole(): string | null {
    const payload = this.getPayload();
    if (!payload) return localStorage.getItem(this.roleKey);
    const role = payload['role'];
    return typeof role === 'string' ? role : null;
  }

  getUsername(): string | null {
    const payload = this.getPayload();
    if (payload && typeof payload['unique_name'] === 'string') return payload['unique_name'] as string;
    return localStorage.getItem(this.usernameKey);
  }

  hasAnyRole(roles: string[]): boolean {
    const role = this.getRole();
    if (!role) return false;
    return roles.includes(role);
  }

  private getPayload(): JwtPayload | null {
    const token = this.getToken();
    if (!token) return null;

    const parts = token.split('.');
    if (parts.length < 2) return null;

    try {
      const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const normalized = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
      const decoded = atob(normalized);
      return JSON.parse(decoded) as JwtPayload;
    } catch {
      return null;
    }
  }
}
