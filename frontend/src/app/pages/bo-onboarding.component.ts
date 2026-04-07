import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { BoOnboardingService } from '../core/services/bo-onboarding.service';
import { BoModuleNavComponent } from '../shared/components/bo-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-bo-onboarding',
  imports: [CommonModule, BoModuleNavComponent],
  template: `
    <main class="wrap">
      <div class="bg-orb orb-a"></div>
      <div class="bg-orb orb-b"></div>
      <app-bo-module-nav />

      <section class="content">
        <header class="hero">
          <div>
            <h1>Onboarding final / Alta guiada</h1>
            <p>Gestioná el progreso por pasos y completá la configuración operativa inicial.</p>
          </div>
          <div class="hero-actions">
            <span class="hero-pill" *ngIf="session; else noSessionPill">Sesión #{{ session.id }}</span>
            <ng-template #noSessionPill><span class="hero-pill">Sin sesión activa</span></ng-template>
            <button class="btn btn-secondary" (click)="reload()" [disabled]="loading">Actualizar estado</button>
          </div>
        </header>

        <section class="card actions">
          <button class="btn btn-primary" (click)="start()" [disabled]="loading">Iniciar / Retomar</button>
          <button class="btn btn-secondary" (click)="reload()" [disabled]="loading || !session">Recargar progreso</button>
          <button class="btn btn-secondary" (click)="finish()" [disabled]="loading || !session">Completar onboarding</button>
        </section>

        <section *ngIf="session" class="card">
          <div class="kpis">
            <div class="kpi"><span>Sesión</span><strong>#{{ session.id }}</strong></div>
            <div class="kpi"><span>Estado</span><strong>{{ session.status }}</strong></div>
            <div class="kpi"><span>Paso actual</span><strong>{{ session.currentStepKey }}</strong></div>
            <div class="kpi"><span>Completados</span><strong>{{ session.completedSteps }}/{{ session.totalSteps }}</strong></div>
          </div>
        </section>

        <section class="card" *ngIf="loading" aria-live="polite">
          <div class="loading-box">
            <span class="spinner" aria-hidden="true"></span>
            <p class="meta">Actualizando sesión de onboarding...</p>
          </div>
        </section>

        <section *ngIf="steps.length" class="card">
          <h3>Pasos</h3>
          <div class="step" *ngFor="let s of steps" [class.step-done]="s.isCompleted">
            <div>
              <strong>{{ s.index }}. {{ s.title }}</strong>
              <div class="meta">key: {{ s.stepKey }}</div>
              <div class="meta" *ngIf="s.endpoints?.length">orquesta: {{ s.endpoints.join(', ') }}</div>
            </div>
            <div class="step-actions">
              <span class="badge" [class.ok-badge]="s.isCompleted" [class.pending-badge]="!s.isCompleted">{{ s.isCompleted ? 'Completado' : 'Pendiente' }}</span>
              <button class="btn-small" (click)="goStep(s.stepKey)" [disabled]="loading">Ir</button>
              <button class="btn-small" (click)="markDone(s.stepKey)" [disabled]="loading || s.isCompleted">Marcar completo</button>
            </div>
          </div>
        </section>

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
    `.hero-actions{display:flex;flex-direction:column;gap:8px;align-items:flex-end}`,
    `.hero-pill{display:inline-flex;align-items:center;background:rgba(255,255,255,.22);border:1px solid rgba(255,255,255,.4);border-radius:999px;padding:6px 10px;font-size:12px;font-weight:700;color:#fff}`,
    `.card{border:1px solid rgba(16,94,67,.16);border-radius:18px;padding:14px;background:rgba(255,255,255,.94);box-shadow:0 14px 32px rgba(33,73,57,.12);backdrop-filter:blur(2px);display:flex;flex-direction:column;gap:12px}`,
    `.actions{display:flex;gap:8px;flex-wrap:wrap}`,
    `.btn{border:1px solid transparent;border-radius:10px;padding:10px 12px;font-weight:700;cursor:pointer;min-height:42px;transition:transform .14s ease,box-shadow .2s ease,background .2s ease}`,
    `.btn-primary{border-color:#2c7f5d;background:linear-gradient(135deg,#2f8e67,#1f6f50);color:#fff}`,
    `.btn-secondary{border-color:#2b7f5c;background:#e7f4ed;color:#15543d}`,
    `.btn:hover:not([disabled]){transform:translateY(-1px);box-shadow:0 8px 16px rgba(31,111,80,.18)}`,
    `.btn:focus-visible{outline:none;box-shadow:0 0 0 3px rgba(47,142,103,.2)}`,
    `.btn[disabled]{opacity:.6;cursor:not-allowed}`,
    `.kpis{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:10px}`,
    `.kpi{border:1px solid #dcebe3;background:#f8fcfa;border-radius:12px;padding:10px;display:flex;flex-direction:column;gap:4px}`,
    `.kpi span{font-size:12px;color:#2f5e4b}`,
    `.kpi strong{font-size:18px;color:#0f4f3a;font-variant-numeric:tabular-nums}`,
    `.step{display:flex;justify-content:space-between;gap:12px;align-items:center;padding:10px;border:1px solid #e2efe8;border-radius:12px;background:#fbfefd}`,
    `.step-done{border-color:#9fd4b8;background:#f2faf6}`,
    `.step-actions{display:flex;gap:8px;align-items:center;flex-wrap:wrap}`,
    `.meta{font-size:12px;color:#3d6757}`,
    `.badge{display:inline-block;padding:4px 10px;border-radius:999px;font-size:12px;font-weight:700}`,
    `.ok-badge{background:#e4f7ea;color:#0f6a3d;border:1px solid #8fc8a9}`,
    `.pending-badge{background:#fff3dc;color:#8a5a00;border:1px solid #e6ca8f}`,
    `.btn-small{border:1px solid #cde3d7;background:#f5fbf8;color:#23644b;border-radius:10px;padding:8px 10px;font-weight:700;cursor:pointer;transition:all .18s ease}`,
    `.btn-small:hover:not([disabled]){background:#e8f5ef}`,
    `.btn-small:focus-visible{outline:none;box-shadow:0 0 0 3px rgba(47,142,103,.2)}`,
    `.btn-small[disabled]{opacity:.6;cursor:not-allowed}`,
    `.loading-box{display:flex;align-items:center;gap:10px}`,
    `.spinner{width:18px;height:18px;border-radius:999px;border:2px solid #c9e2d7;border-top-color:#2f8e67;animation:spin .9s linear infinite}`,
    `@keyframes spin{to{transform:rotate(360deg)}}`,
    `.alert{border-radius:12px;padding:12px 14px}`,
    `.alert.ok{background:#edf8f2;border:1px solid #bfd9cc;color:#16543d;font-weight:700}`,
    `.alert.error{background:#feefef;border:1px solid #f3c6c6;color:#9f1f1f;font-weight:700}`,
    `@media (max-width: 900px){.wrap{padding:16px}.hero-actions{align-items:flex-start;width:100%}.actions .btn{width:100%}.step{flex-direction:column;align-items:flex-start}}`
  ]
})
export class BoOnboardingComponent {
  session: any = null;
  steps: any[] = [];
  loading = false;
  message = '';
  error = '';

  constructor(private readonly api: BoOnboardingService) {
    void this.reload();
  }

  async start(): Promise<void> {
    this.loading = true;
    this.error = '';
    this.message = '';
    try {
      this.session = await this.api.start({});
      await this.loadSteps();
      this.message = 'Sesión iniciada o retomada';
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo iniciar onboarding';
    } finally {
      this.loading = false;
    }
  }

  async reload(): Promise<void> {
    this.loading = true;
    this.error = '';
    this.message = '';
    try {
      this.session = await this.api.current();
      await this.loadSteps();
    } catch {
      this.session = null;
      this.steps = [];
    } finally {
      this.loading = false;
    }
  }

  async goStep(stepKey: string): Promise<void> {
    if (!this.session) return;
    this.loading = true;
    this.error = '';
    try {
      this.session = await this.api.setStep(this.session.id, stepKey);
      await this.loadSteps();
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo cambiar de paso';
    } finally {
      this.loading = false;
    }
  }

  async markDone(stepKey: string): Promise<void> {
    if (!this.session) return;
    this.loading = true;
    this.error = '';
    try {
      this.session = await this.api.completeStep(this.session.id, stepKey);
      await this.loadSteps();
      this.message = `Paso ${stepKey} completado`;
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo completar el paso';
    } finally {
      this.loading = false;
    }
  }

  async finish(): Promise<void> {
    if (!this.session) return;
    this.loading = true;
    this.error = '';
    try {
      this.session = await this.api.complete(this.session.id);
      await this.loadSteps();
      this.message = 'Alta guiada completada';
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo completar el alta guiada';
    } finally {
      this.loading = false;
    }
  }

  private async loadSteps(): Promise<void> {
    if (!this.session) return;
    this.steps = await this.api.getSteps(this.session.id);
  }
}
