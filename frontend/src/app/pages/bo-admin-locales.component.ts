import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BoAdminService } from '../core/services/bo-admin.service';
import { BoModuleNavComponent } from '../shared/components/bo-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-bo-admin-locales',
  imports: [CommonModule, FormsModule, RouterLink, BoModuleNavComponent],
  template: `
    <main class="wrap">
      <div class="bg-orb orb-a"></div>
      <div class="bg-orb orb-b"></div>
      <app-bo-module-nav />

      <section class="content">
        <header class="hero">
          <div>
            <h1>Administración de locales</h1>
            <p>Define locales por tenant y accede rápido a configuración y usuarios.</p>
          </div>
          <span class="hero-pill">Total: {{ stores.length }}</span>
        </header>

        <section class="card">
          <div class="row">
            <select [(ngModel)]="draft.tenantId"><option *ngFor="let t of tenants" [ngValue]="t.id">{{ t.name }}</option></select>
            <input placeholder="Nombre local" [(ngModel)]="draft.name" />
            <input placeholder="Código" [(ngModel)]="draft.code" />
            <button class="btn" (click)="save()">Crear</button>
          </div>
        </section>

        <section class="card list" *ngIf="stores.length; else emptyStores">
          <div class="store" *ngFor="let s of stores">
            <div class="store-title">#{{ s.id }} {{ s.name }} ({{ s.code }})</div>
            <div class="links">
              <a [routerLink]="['/bo/admin/locales', s.id, 'configuracion']">Configuración</a>
              <a [routerLink]="['/bo/admin/locales', s.id, 'usuarios']">Usuarios</a>
            </div>
          </div>
        </section>

        <ng-template #emptyStores>
          <section class="card">
            <p class="meta">Aún no hay locales cargados.</p>
          </section>
        </ng-template>

        <section class="alert error" *ngIf="error" aria-live="assertive">{{ error }}</section>
      </section>
    </main>
  `,
  styles:[
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
    `.card{border:1px solid rgba(16,94,67,.16);border-radius:18px;padding:14px;background:rgba(255,255,255,.94);box-shadow:0 14px 32px rgba(33,73,57,.12);backdrop-filter:blur(2px);display:flex;flex-direction:column;gap:12px}`,
    `.row{display:flex;gap:8px;flex-wrap:wrap}`,
    `.row > *{flex:1 1 220px}`,
    `.row .btn{flex:0 0 auto}`,
    `input,select{border:1px solid #d5e6dd;border-radius:10px;padding:9px 10px;background:#fff;outline:none;min-height:42px}`,
    `input:focus,select:focus{border-color:#2f8e67;box-shadow:0 0 0 3px rgba(47,142,103,.12)}`,
    `.btn{border:1px solid #2b7f5c;background:#e7f4ed;color:#15543d;border-radius:10px;padding:10px 12px;font-weight:700;cursor:pointer;min-height:42px;transition:transform .14s ease,box-shadow .2s ease,background .2s ease}`,
    `.btn:hover{background:#dff2e9;transform:translateY(-1px);box-shadow:0 8px 16px rgba(31,111,80,.18)}`,
    `.btn:focus-visible{outline:none;box-shadow:0 0 0 3px rgba(47,142,103,.2)}`,
    `.list{gap:8px}`,
    `.store{border:1px solid #dcebe3;background:#f8fcfa;border-radius:12px;padding:10px 12px;display:flex;justify-content:space-between;align-items:center;gap:10px;flex-wrap:wrap;transition:background .2s ease,border-color .2s ease}`,
    `.store:hover{background:#f4fbf8}`,
    `.store-title{color:#1f5a45;font-weight:700}`,
    `.links{display:flex;gap:8px;flex-wrap:wrap}`,
    `.links a{text-decoration:none;color:#15543d;background:#e7f4ed;border:1px solid #bfd9cc;border-radius:999px;padding:6px 10px;font-size:13px;font-weight:700;transition:all .18s ease}`,
    `.links a:hover{background:#dff2e9}`,
    `.meta{margin:0;color:#2f5f4c}`,
    `.alert{border-radius:12px;padding:12px 14px}`,
    `.alert.error{background:#feefef;border:1px solid #f3c6c6;color:#9f1f1f;font-weight:700}`,
    `@media (max-width: 900px){.wrap{padding:16px}.row > *{flex:1 1 100%}.row .btn{flex:1 1 100%}}`
  ]
})
export class BoAdminLocalesComponent {
  tenants: any[] = [];
  stores: any[] = [];
  draft: any = { tenantId: null, name: '', code: '', isActive: true };
  error = '';
  constructor(private readonly api: BoAdminService) { void this.bootstrap(); }
  async bootstrap(): Promise<void> { this.tenants = await this.api.getTenants(); if (this.tenants.length) this.draft.tenantId = this.tenants[0].id; await this.load(); }
  async load(): Promise<void> { this.stores = await this.api.getStores(); }
  async save(): Promise<void> { try { await this.api.createStore(this.draft); this.draft={tenantId:this.draft.tenantId,name:'',code:'',isActive:true}; await this.load(); } catch (e:any){ this.error=e?.error?.message ?? 'No se pudo guardar'; } }
}
