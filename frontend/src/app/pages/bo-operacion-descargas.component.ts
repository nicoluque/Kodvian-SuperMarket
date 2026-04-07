import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { BoOperacionService } from '../core/services/bo-operacion.service';
import { BoModuleNavComponent } from '../shared/components/bo-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-bo-operacion-descargas',
  imports: [CommonModule, BoModuleNavComponent],
  template: `
    <main class="wrap">
      <div class="bg-orb orb-a"></div>
      <div class="bg-orb orb-b"></div>
      <app-bo-module-nav />

      <section class="content">
        <header class="hero">
          <div>
            <h1>Centro de descargas</h1>
            <p>Descargá material operativo y revisá la salud general del sistema.</p>
          </div>
          <div class="hero-actions">
            <span class="hero-pill">Monitoreo operativo</span>
            <button class="btn btn-secondary" (click)="loadStatus()">Actualizar estado</button>
          </div>
        </header>

        <section class="card actions">
          <a class="btn btn-primary" [href]="manualUrl" target="_blank" rel="noreferrer">Descargar Manual + Kit</a>
          <a class="btn btn-secondary" [href]="catalogUrl" target="_blank" rel="noreferrer">Descargar catálogo de emergencia</a>
          <a class="btn btn-secondary" href="/bo/exportaciones">Abrir exportaciones gerenciales</a>
        </section>

        <section class="card" *ngIf="status">
          <h3>Estado del sistema</h3>
          <div class="status-grid">
            <div class="status-item"><span>DB</span><strong [class.ok]="status.dbOk" [class.fail]="!status.dbOk">{{ status.dbOk ? 'OK' : 'FAIL' }}</strong></div>
            <div class="status-item"><span>Storage</span><strong [class.ok]="status.storageOk" [class.fail]="!status.storageOk">{{ status.storageOk ? 'OK' : 'FAIL' }}</strong></div>
            <div class="status-item"><span>API</span><strong [class.ok]="status.apiOk" [class.fail]="!status.apiOk">{{ status.apiOk ? 'OK' : 'FAIL' }}</strong></div>
            <div class="status-item"><span>Último backup</span><strong>{{ status.lastBackupTimestamp || '-' }}</strong></div>
            <div class="status-item"><span>Cola offline</span><strong>{{ status.offlineQueuePending ?? status.offlineQueue ?? 0 }}</strong></div>
            <div class="status-item"><span>Última sincronización</span><strong>{{ status.lastSync || '-' }}</strong></div>
          </div>
        </section>

        <section class="card" *ngIf="!status && !error" aria-live="polite">
          <div class="loading-box">
            <span class="spinner" aria-hidden="true"></span>
            <p class="meta">Consultando estado del sistema...</p>
          </div>
        </section>

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
    `.hero-actions{display:flex;flex-direction:column;gap:8px;align-items:flex-end}`,
    `.hero-pill{display:inline-flex;align-items:center;background:rgba(255,255,255,.22);border:1px solid rgba(255,255,255,.4);border-radius:999px;padding:6px 10px;font-size:12px;font-weight:700;color:#fff}`,
    `.card{border:1px solid rgba(16,94,67,.16);border-radius:18px;padding:14px;background:rgba(255,255,255,.94);box-shadow:0 14px 32px rgba(33,73,57,.12);backdrop-filter:blur(2px);display:flex;flex-direction:column;gap:12px}`,
    `.actions{display:flex;gap:10px;flex-wrap:wrap}`,
    `.btn{border:1px solid transparent;border-radius:10px;padding:10px 12px;font-weight:700;cursor:pointer;text-decoration:none;display:inline-flex;align-items:center;justify-content:center;min-height:42px;transition:transform .14s ease,box-shadow .2s ease,background .2s ease}`,
    `.btn-primary{border-color:#2c7f5d;background:linear-gradient(135deg,#2f8e67,#1f6f50);color:#fff}`,
    `.btn-secondary{border-color:#2b7f5c;background:#e7f4ed;color:#15543d}`,
    `.btn:hover:not([disabled]){transform:translateY(-1px);box-shadow:0 8px 16px rgba(31,111,80,.18)}`,
    `.btn:focus-visible{outline:none;box-shadow:0 0 0 3px rgba(47,142,103,.2)}`,
    `.status-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(200px,1fr));gap:10px}`,
    `.status-item{border:1px solid #dcebe3;background:#f8fcfa;border-radius:12px;padding:10px;display:flex;flex-direction:column;gap:4px}`,
    `.status-item span{font-size:12px;color:#2f5e4b}`,
    `.status-item strong{font-size:18px;color:#0f4f3a;font-variant-numeric:tabular-nums}`,
    `.status-item strong.ok{color:#0f7a45}`,
    `.status-item strong.fail{color:#a02222}`,
    `.meta{margin:0;color:#2f5f4c}`,
    `.loading-box{display:flex;align-items:center;gap:10px}`,
    `.spinner{width:18px;height:18px;border-radius:999px;border:2px solid #c9e2d7;border-top-color:#2f8e67;animation:spin .9s linear infinite}`,
    `@keyframes spin{to{transform:rotate(360deg)}}`,
    `.alert{border-radius:12px;padding:12px 14px}`,
    `.alert.error{background:#feefef;border:1px solid #f3c6c6;color:#9f1f1f;font-weight:700}`,
    `@media (max-width: 900px){.wrap{padding:16px}.hero-actions{align-items:flex-start;width:100%}.btn{width:100%}}`
  ]
})
export class BoOperacionDescargasComponent {
  status: any = null;
  error = '';

  manualUrl = '';
  catalogUrl = '';

  constructor(private readonly ops: BoOperacionService) {
    this.manualUrl = this.ops.getManualKitUrl();
    this.catalogUrl = this.ops.getEmergencyCatalogUrl();
    void this.loadStatus();
  }

  async loadStatus(): Promise<void> {
    this.error = '';
    try {
      this.status = await this.ops.getSystemStatus();
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo cargar estado del sistema';
    }
  }
}
