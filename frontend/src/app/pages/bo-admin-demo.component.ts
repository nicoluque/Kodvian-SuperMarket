import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { BoDemoService, DemoStatus } from '../core/services/bo-demo.service';
import { BoModuleNavComponent } from '../shared/components/bo-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-bo-admin-demo',
  imports: [CommonModule, BoModuleNavComponent],
  template: `
    <main class="wrap">
      <div class="bg-orb orb-a"></div>
      <div class="bg-orb orb-b"></div>
      <app-bo-module-nav />

      <header class="hero">
        <h1>Demo comercial</h1>
        <p>Administra el tenant demo, resetea datos y ejecuta el guion comercial de manera controlada.</p>
      </header>

      <section class="card" *ngIf="status">
        <h3>Estado actual</h3>
        <div class="kpis">
          <div class="kpi"><span>Estado</span><strong>{{ status.exists ? 'Cargado' : 'No inicializado' }}</strong></div>
          <div class="kpi"><span>Tenant</span><strong>{{ status.tenantId ?? '-' }}</strong></div>
          <div class="kpi"><span>Store</span><strong>{{ status.storeId ?? '-' }}</strong></div>
          <div class="kpi"><span>Usuarios</span><strong>{{ status.users ?? 0 }}</strong></div>
          <div class="kpi"><span>Productos</span><strong>{{ status.products ?? 0 }}</strong></div>
          <div class="kpi"><span>Ventas</span><strong>{{ status.sales ?? 0 }}</strong></div>
        </div>
        <p class="meta"><strong>Ultimo seed:</strong> {{ status.lastSeedAt || '-' }}</p>
        <p class="meta"><strong>Ultimo reset:</strong> {{ status.lastResetAt || '-' }}</p>
      </section>

      <section class="row">
        <button class="btn btn-primary" [disabled]="busy" (click)="seed()">Seed demo</button>
        <button class="btn btn-secondary" [disabled]="busy" (click)="reset()">Reset demo</button>
        <button class="btn btn-secondary" [disabled]="busy" (click)="resetTraining()">Reiniciar capacitación</button>
        <button class="btn btn-primary" [disabled]="busy || !status?.tenantId || !status?.storeId" (click)="enterDemo()">Entrar al tenant demo</button>
        <a class="btn btn-secondary link-btn" href="/bo/capacitacion">Abrir capacitación</a>
      </section>

      <section class="card script">
        <h3>Guion sugerido de demo</h3>
        <ol>
          <li>Mostrar branding y login comercial del tenant demo.</li>
          <li>Recorrer dashboard: ventas de últimos días + transferencia pendiente.</li>
          <li>Mostrar devolucion, reclamo proveedor y credito disponible.</li>
          <li>Abrir RRHH inconsistencias y tarea Kanban requerida de cierre.</li>
        </ol>
      </section>

      <p *ngIf="message" class="ok">{{ message }}</p>
      <p *ngIf="error" class="error">{{ error }}</p>
    </main>
  `,
  styles: [
    `:host{display:block}`,
    `.wrap{position:relative;overflow-x:clip;min-height:100vh;padding:24px;display:flex;flex-direction:column;gap:14px;font-family:'Montserrat','Segoe UI',sans-serif;background:linear-gradient(160deg,#fdf7ef 0%,#fff8ee 46%,#f2faf7 100%)}`,
    `.bg-orb{position:absolute;border-radius:999px;filter:blur(4px);pointer-events:none;opacity:.5}`,
    `.orb-a{width:320px;height:320px;right:0;top:80px;transform:translateX(22%);background:radial-gradient(circle,#ffcf9d 0%,rgba(255,207,157,0) 68%)}`,
    `.orb-b{width:300px;height:300px;left:0;top:260px;transform:translateX(-35%);background:radial-gradient(circle,#9fe6c6 0%,rgba(159,230,198,0) 70%)}`,
    `.hero{position:relative;z-index:1;background:linear-gradient(120deg,#0e5a42,#247a58 54%,#3a9b75);color:#fff;border-radius:22px;padding:22px 24px;box-shadow:0 18px 40px rgba(22,66,46,.22)}`,
    `.hero h1{margin:0 0 8px;font-size:30px}`,
    `.hero p{margin:0;color:rgba(255,255,255,.9)}`,
    `.card{position:relative;z-index:1;border:1px solid rgba(16,94,67,.16);border-radius:18px;padding:14px;background:rgba(255,255,255,.94);box-shadow:0 14px 32px rgba(33,73,57,.12)}`,
    `.kpis{display:grid;grid-template-columns:repeat(auto-fit,minmax(140px,1fr));gap:10px}`,
    `.kpi{border:1px solid #dcebe3;background:#f8fcfa;border-radius:12px;padding:10px;display:flex;flex-direction:column;gap:4px}`,
    `.kpi span{font-size:12px;color:#4e7463}`,
    `.kpi strong{font-size:22px;color:#0f4f3a}`,
    `.meta{margin:8px 0 0;color:#4b6f60}`,
    `.row{display:flex;gap:8px;flex-wrap:wrap;position:relative;z-index:1}`,
    `.btn{border:1px solid transparent;border-radius:10px;padding:10px 12px;font-weight:700;cursor:pointer;text-decoration:none;display:inline-flex;align-items:center;justify-content:center;min-height:42px;transition:transform .14s ease,box-shadow .2s ease,background .2s ease}`,
    `.btn-primary{border-color:#2c7f5d;background:linear-gradient(135deg,#2f8e67,#1f6f50);color:#fff}`,
    `.btn-secondary{border-color:#2f8e67;background:#e8f5ef;color:#1f6f50}`,
    `.btn:hover:not([disabled]){transform:translateY(-1px);box-shadow:0 8px 16px rgba(31,111,80,.2)}`,
    `.btn:focus-visible{outline:none;box-shadow:0 0 0 3px rgba(47,142,103,.2)}`,
    `.btn[disabled]{opacity:.6;cursor:not-allowed}`,
    `.link-btn{line-height:1}`,
    `.script ol{margin:8px 0 0 18px}`,
    `.ok{color:#0a7a32;font-weight:700}`,
    `.error{color:#b3261e;font-weight:700}`,
    `@media (max-width: 900px){.wrap{padding:16px}.hero h1{font-size:24px}}`
  ]
})
export class BoAdminDemoComponent {
  status: DemoStatus | null = null;
  busy = false;
  message = '';
  error = '';

  constructor(private readonly api: BoDemoService, private readonly router: Router) {
    void this.load();
  }

  async load(): Promise<void> {
    try {
      this.status = await this.api.status();
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo cargar estado demo';
    }
  }

  async seed(): Promise<void> {
    await this.run(async () => {
      this.status = await this.api.seed();
      this.message = 'Seed demo completado';
    });
  }

  async reset(): Promise<void> {
    await this.run(async () => {
      this.status = await this.api.reset();
      this.message = 'Reset demo completado';
    });
  }

  async resetTraining(): Promise<void> {
    await this.run(async () => {
      await this.api.resetTraining();
      this.message = 'Reset training completado';
    });
  }

  enterDemo(): void {
    if (!this.status?.tenantId || !this.status?.storeId) return;
    localStorage.setItem('bo_active_tenant_id', String(this.status.tenantId));
    localStorage.setItem('bo_active_store_id', String(this.status.storeId));
    void this.router.navigateByUrl('/bo/dashboard');
  }

  private async run(fn: () => Promise<void>): Promise<void> {
    this.busy = true;
    this.message = '';
    this.error = '';
    try {
      await fn();
    } catch (err: any) {
      this.error = err?.error?.message ?? 'Operación demo fallida';
    } finally {
      this.busy = false;
    }
  }
}
