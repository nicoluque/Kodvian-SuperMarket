import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, firstValueFrom } from 'rxjs';

export interface BrandingSettings {
  tenantId: number;
  displayName: string;
  logoUrl?: string;
  primaryColor: string;
  secondaryColor: string;
  ticketHeaderText: string;
  ticketFooterText: string;
  returnPolicyText: string;
  supportPhone?: string;
  supportEmail?: string;
  updatedAt?: string;
}

@Injectable({ providedIn: 'root' })
export class BrandingService {
  private readonly base = '/api/v1/admin/branding';
  private readonly brandingSubject = new BehaviorSubject<BrandingSettings | null>(null);
  readonly branding$ = this.brandingSubject.asObservable();

  constructor(private readonly http: HttpClient) {}

  get current(): BrandingSettings | null {
    return this.brandingSubject.value;
  }

  async load(): Promise<BrandingSettings> {
    const branding = await firstValueFrom(this.http.get<BrandingSettings>(this.base));
    this.brandingSubject.next(branding);
    this.applyTheme(branding);
    return branding;
  }

  async update(payload: Partial<BrandingSettings>): Promise<BrandingSettings> {
    const branding = await firstValueFrom(this.http.put<BrandingSettings>(this.base, payload));
    this.brandingSubject.next(branding);
    this.applyTheme(branding);
    return branding;
  }

  async uploadLogo(file: File): Promise<string> {
    const form = new FormData();
    form.append('file', file);
    const res = await firstValueFrom(this.http.post<{ logoUrl: string }>(`${this.base}/logo`, form));
    await this.load();
    return res.logoUrl;
  }

  previewTicket(): Promise<{ html: string }> {
    return firstValueFrom(this.http.get<{ html: string }>(`${this.base}/preview/ticket`));
  }

  previewLogin(): Promise<{ html: string }> {
    return firstValueFrom(this.http.get<{ html: string }>(`${this.base}/preview/login`));
  }

  private applyTheme(branding: BrandingSettings): void {
    if (typeof document === 'undefined') return;
    document.documentElement.style.setProperty('--brand-primary', branding.primaryColor || '#1f7f57');
    document.documentElement.style.setProperty('--brand-secondary', branding.secondaryColor || '#27313f');
    if (branding.displayName) {
      document.title = `${branding.displayName} - Backoffice`;
    }
  }
}
