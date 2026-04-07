import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BrandingService, BrandingSettings } from '../core/services/branding.service';
import { BoModuleNavComponent } from '../shared/components/bo-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-bo-admin-branding',
  imports: [CommonModule, FormsModule, BoModuleNavComponent],
  template: `
    <main class="wrap" *ngIf="model">
      <div class="bg-orb orb-a"></div>
      <div class="bg-orb orb-b"></div>
      <app-bo-module-nav />

      <section class="content">
        <header class="hero">
          <div>
            <h1>Branding comercial</h1>
            <p>Define identidad visual, textos y soporte para login, tickets y encabezados.</p>
          </div>
          <span class="hero-pill">Marca activa</span>
        </header>

        <section class="card">
          <h3>Identidad visual</h3>
          <div class="grid">
            <label>Nombre comercial <input [(ngModel)]="model.displayName" /></label>
            <label>Color primario <input [(ngModel)]="model.primaryColor" /></label>
            <label>Color secundario <input [(ngModel)]="model.secondaryColor" /></label>
            <label>Logo URL <input [(ngModel)]="model.logoUrl" /></label>
            <label class="upload">Subir logo <input type="file" accept=".png,.jpg,.jpeg,.svg,.webp" (change)="onLogo($event)" /></label>
          </div>
        </section>

        <section class="card">
          <h3>Textos de ticket/comprobantes</h3>
          <label>Encabezado <textarea [(ngModel)]="model.ticketHeaderText"></textarea></label>
          <label>Pie de ticket <textarea [(ngModel)]="model.ticketFooterText"></textarea></label>
          <label>Política de devoluciones <textarea [(ngModel)]="model.returnPolicyText"></textarea></label>
        </section>

        <section class="card">
          <h3>Soporte</h3>
          <div class="grid">
            <label>Teléfono <input [(ngModel)]="model.supportPhone" /></label>
            <label>Correo <input [(ngModel)]="model.supportEmail" /></label>
          </div>
        </section>

        <section class="card previews">
          <h3>Vistas previas</h3>
          <article>
            <h4>Ingreso</h4>
            <div [innerHTML]="previewLoginHtml"></div>
          </article>
          <article>
            <h4>Comprobante</h4>
            <div [innerHTML]="previewTicketHtml"></div>
          </article>
          <article>
            <h4>Encabezado</h4>
            <div class="header-preview" [style.background]="'linear-gradient(120deg,' + model.primaryColor + ',' + model.secondaryColor + ')'">
              <img *ngIf="model.logoUrl" [src]="model.logoUrl" alt="logo" />
              <strong>{{ model.displayName }}</strong>
            </div>
          </article>
        </section>

        <div class="row">
          <button class="btn" [disabled]="saving" (click)="save()">Guardar branding</button>
        </div>

        <section class="alert ok" *ngIf="message" aria-live="polite">{{ message }}</section>
        <section class="alert error" *ngIf="error" aria-live="assertive">{{ error }}</section>
      </section>
    </main>
  `,
  styles: [
    `:host{display:block}`,
    `.wrap{position:relative;overflow-x:clip;min-height:100vh;padding:24px;display:flex;flex-direction:column;gap:14px;font-family:'Montserrat','Segoe UI',sans-serif;background:linear-gradient(160deg,#fdf7ef 0%,#fff8ee 46%,#f2faf7 100%)}`,
    `.content{position:relative;z-index:1;display:flex;flex-direction:column;gap:14px;max-width:1120px;width:100%;margin:0 auto}`,
    `.bg-orb{position:absolute;border-radius:999px;filter:blur(4px);pointer-events:none;opacity:.5}`,
    `.orb-a{width:320px;height:320px;right:0;top:80px;transform:translateX(22%);background:radial-gradient(circle,#ffcf9d 0%,rgba(255,207,157,0) 68%)}`,
    `.orb-b{width:300px;height:300px;left:0;top:240px;transform:translateX(-35%);background:radial-gradient(circle,#9fe6c6 0%,rgba(159,230,198,0) 70%)}`,
    `.hero{background:linear-gradient(120deg,#0e5a42,#247a58 54%,#3a9b75);color:#fff;border-radius:22px;padding:22px 24px;box-shadow:0 18px 40px rgba(22,66,46,.22);display:flex;justify-content:space-between;align-items:flex-start;gap:12px;flex-wrap:wrap}`,
    `.hero h1{margin:0 0 8px;font-size:clamp(24px,2.4vw,32px);line-height:1.1}`,
    `.hero p{margin:0;color:rgba(255,255,255,.95)}`,
    `.hero-pill{display:inline-flex;align-items:center;background:rgba(255,255,255,.22);border:1px solid rgba(255,255,255,.4);border-radius:999px;padding:6px 10px;font-size:12px;font-weight:700;color:#fff}`,
    `.card{border:1px solid rgba(16,94,67,.16);border-radius:18px;padding:14px;background:rgba(255,255,255,.94);box-shadow:0 14px 32px rgba(33,73,57,.12);backdrop-filter:blur(2px)}`,
    `.card h3{margin:0 0 10px;color:#1b5b44}`,
    `.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:10px}`,
    `label{display:flex;flex-direction:column;gap:4px;color:#1f6048;font-weight:700}`,
    `input,textarea{border:1px solid #d5e6dd;border-radius:10px;padding:9px 10px;background:#fff;outline:none;font-size:14px}`,
    `input:focus,textarea:focus{border-color:#2f8e67;box-shadow:0 0 0 3px rgba(47,142,103,.12)}`,
    `textarea{min-height:72px}`,
    `.previews{display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:10px}`,
    `.previews article{border:1px solid #dcebe3;background:#f8fcfa;border-radius:12px;padding:10px}`,
    `.header-preview{padding:12px;border-radius:8px;color:#fff;display:flex;align-items:center;gap:8px}`,
    `.header-preview img{height:28px;width:28px;object-fit:contain;border-radius:4px;background:#fff}`,
    `.upload input{padding:0}`,
    `.row{display:flex;gap:8px}`,
    `.btn{border:1px solid #2b7f5c;background:#e7f4ed;color:#15543d;border-radius:10px;padding:10px 12px;font-weight:700;cursor:pointer;min-height:42px;transition:transform .14s ease,box-shadow .2s ease,background .2s ease}`,
    `.btn:hover:not([disabled]){background:#dff2e9;transform:translateY(-1px);box-shadow:0 8px 16px rgba(31,111,80,.18)}`,
    `.btn:focus-visible{outline:none;box-shadow:0 0 0 3px rgba(47,142,103,.2)}`,
    `.btn[disabled]{opacity:.6;cursor:not-allowed}`,
    `.alert{border-radius:12px;padding:12px 14px}`,
    `.alert.ok{background:#edf8f2;border:1px solid #bfd9cc;color:#16543d;font-weight:700}`,
    `.alert.error{background:#feefef;border:1px solid #f3c6c6;color:#9f1f1f;font-weight:700}`,
    `@media (max-width: 900px){.wrap{padding:16px}.row .btn{width:100%}}`
  ]
})
export class BoAdminBrandingComponent {
  model: BrandingSettings | null = null;
  previewTicketHtml = '';
  previewLoginHtml = '';
  message = '';
  error = '';
  saving = false;

  constructor(private readonly branding: BrandingService) {
    void this.bootstrap();
  }

  async bootstrap(): Promise<void> {
    try {
      this.model = await this.branding.load();
      await this.reloadPreviews();
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo cargar branding';
    }
  }

  async save(): Promise<void> {
    if (!this.model) return;
    this.saving = true;
    this.message = '';
    this.error = '';
    try {
      this.model = await this.branding.update(this.model);
      await this.reloadPreviews();
      this.message = 'Branding actualizado';
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo guardar branding';
    } finally {
      this.saving = false;
    }
  }

  async onLogo(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file || !this.model) return;

    this.error = '';
    try {
      this.model.logoUrl = await this.branding.uploadLogo(file);
      await this.reloadPreviews();
      this.message = 'Logo actualizado';
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo subir logo';
    }
  }

  private async reloadPreviews(): Promise<void> {
    const [ticket, login] = await Promise.all([
      this.branding.previewTicket(),
      this.branding.previewLogin()
    ]);
    this.previewTicketHtml = ticket.html;
    this.previewLoginHtml = login.html;
  }
}
