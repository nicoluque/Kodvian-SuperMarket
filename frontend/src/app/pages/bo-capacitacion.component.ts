import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { AuthJwtService } from '../core/services/auth-jwt.service';
import { TrainingRoleSummary, TrainingService } from '../core/services/training.service';
import { BoModuleNavComponent } from '../shared/components/bo-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-bo-capacitacion',
  imports: [CommonModule, BoModuleNavComponent],
  template: `
    <main class="wrap">
      <div class="bg-orb orb-a"></div>
      <div class="bg-orb orb-b"></div>
      <app-bo-module-nav />

      <header class="hero">
        <h1>Capacitación</h1>
        <p>Seguimiento de checklists por rol, avance por corrida y trazabilidad del entrenamiento operativo.</p>
      </header>

      <div class="head">
        <div class="head-actions">
          <button class="btn btn-secondary" *ngIf="isAdmin" [disabled]="busy" (click)="resetTraining()">Reiniciar capacitación</button>
          <button class="btn btn-primary" [disabled]="busy" (click)="reload()">Recargar</button>
        </div>
      </div>

      <section class="grid">
        <article class="card" *ngFor="let r of roleSummaries">
          <h3>{{ r.role }}</h3>
          <p>Checklists: {{ r.checklistCount }} · Items: {{ r.totalItems }}</p>
            <p>Última corrida: {{ r.latestRunStatus || 'Sin iniciar' }}</p>
            <p *ngIf="r.latestRunAt">Inicio: {{ r.latestRunAt }}</p>
            <div class="actions">
              <button class="btn btn-secondary" [disabled]="busy" (click)="openRole(r.role)">Ver detalle</button>
              <button class="btn btn-primary" [disabled]="busy" (click)="startRole(r.role)">Iniciar checklist</button>
            </div>
          </article>
      </section>

      <section class="card" *ngIf="selectedRole && roleDetail">
        <h2>{{ selectedRole }} - Detalle checklist</h2>
        <p *ngIf="roleDetail.latestRun">
          Run #{{ roleDetail.latestRun.id }} · Estado: {{ roleDetail.latestRun.status }}
        </p>

        <div class="checklist" *ngFor="let c of roleDetail.checklists">
          <h4>{{ c.title }}</h4>
          <p>{{ c.description }}</p>
          <p>Progreso: {{ c.completedItems }}/{{ c.totalItems }}</p>
          <ul>
            <li *ngFor="let i of c.items">
              <span [class.done]="i.isCompleted">{{ i.sortOrder }}. {{ i.title }}</span>
              <button
                class="btn btn-secondary"
                *ngIf="roleDetail.latestRun && !i.isCompleted"
                [disabled]="busy || roleDetail.latestRun.status === 'Completed'"
                (click)="completeItem(roleDetail.latestRun.id, i.id)">
                Completar
              </button>
            </li>
          </ul>
        </div>
      </section>

      <section class="card">
        <h2>Historial</h2>
        <div class="table-scroll" *ngIf="historyItems.length > 0">
        <table>
          <tr><th>Run</th><th>Rol</th><th>Estado</th><th>Progreso</th><th>Inicio</th><th>Fin</th></tr>
          <tr *ngFor="let h of historyItems">
            <td>#{{ h.id }}</td>
            <td>{{ h.role }}</td>
            <td>{{ h.status }}</td>
            <td>{{ h.completedItems }}/{{ h.totalItems }} ({{ h.progress }}%)</td>
            <td>{{ h.startedAt }}</td>
            <td>{{ h.completedAt || '-' }}</td>
          </tr>
        </table>
        </div>
        <p *ngIf="historyItems.length === 0">Sin historial de capacitación.</p>
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
    `.orb-b{width:300px;height:300px;left:0;top:250px;transform:translateX(-35%);background:radial-gradient(circle,#9fe6c6 0%,rgba(159,230,198,0) 70%)}`,
    `.hero{position:relative;z-index:1;background:linear-gradient(120deg,#0e5a42,#247a58 54%,#3a9b75);color:#fff;border-radius:22px;padding:22px 24px;box-shadow:0 18px 40px rgba(22,66,46,.22)}`,
    `.hero h1{margin:0 0 8px;font-size:30px}`,
    `.hero p{margin:0;color:rgba(255,255,255,.9)}`,
    `.head{display:flex;justify-content:flex-end;align-items:center;gap:10px;flex-wrap:wrap}`,
    `.head-actions{display:flex;gap:8px;flex-wrap:wrap}`,
    `.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:12px}`,
    `.card{position:relative;z-index:1;border:1px solid rgba(16,94,67,.16);border-radius:18px;padding:14px;background:rgba(255,255,255,.94);box-shadow:0 14px 32px rgba(33,73,57,.12)}`,
    `.actions{display:flex;gap:8px;flex-wrap:wrap}`,
    `.checklist{border-top:1px solid #e6efe9;padding-top:10px;margin-top:10px}`,
    `ul{margin:8px 0 0 18px;padding:0}`,
    `li{margin:6px 0;display:flex;align-items:center;justify-content:space-between;gap:8px}`,
    `.done{text-decoration:line-through;color:#0a7a32;font-weight:600}`,
    `.table-scroll{overflow:auto;max-width:100%}`,
    `table{width:100%;border-collapse:collapse;min-width:740px;background:#fff}`,
    `th,td{padding:8px;border-bottom:1px solid #edf3ef;text-align:left}`,
    `th{color:#1e5f47;background:#f5fbf8}`,
    `.btn{border:1px solid transparent;border-radius:10px;padding:8px 12px;font-weight:700;cursor:pointer;min-height:42px;transition:transform .14s ease,box-shadow .2s ease,background .2s ease}`,
    `.btn-primary{border-color:#2c7f5d;background:linear-gradient(135deg,#2f8e67,#1f6f50);color:#fff}`,
    `.btn-secondary{border-color:#2f8e67;background:#e8f5ef;color:#1f6f50}`,
    `.btn:hover:not([disabled]){transform:translateY(-1px);box-shadow:0 8px 16px rgba(31,111,80,.2)}`,
    `.btn:focus-visible{outline:none;box-shadow:0 0 0 3px rgba(47,142,103,.2)}`,
    `.btn[disabled]{opacity:.6;cursor:not-allowed}`,
    `.ok{color:#0a7a32;font-weight:700}`,
    `.error{color:#b3261e;font-weight:700}`,
    `@media (max-width: 900px){.wrap{padding:16px}.hero h1{font-size:24px}}`
  ]
})
export class BoCapacitacionComponent {
  roleSummaries: TrainingRoleSummary[] = [];
  selectedRole = '';
  roleDetail: any = null;
  historyItems: any[] = [];

  busy = false;
  message = '';
  error = '';

  get isAdmin(): boolean {
    return this.auth.getRole() === 'Admin';
  }

  constructor(private readonly api: TrainingService, private readonly auth: AuthJwtService) {
    void this.reload();
  }

  async reload(): Promise<void> {
    await this.run(async () => {
      this.roleSummaries = await this.api.getRoleSummaries();
      this.historyItems = await this.api.history();
      if (this.selectedRole) {
        this.roleDetail = await this.api.getRoleDetail(this.selectedRole);
      }
    });
  }

  async openRole(role: string): Promise<void> {
    this.selectedRole = role;
    await this.run(async () => {
      this.roleDetail = await this.api.getRoleDetail(role);
    });
  }

  async startRole(role: string): Promise<void> {
    await this.run(async () => {
      await this.api.startRun({ role });
      this.selectedRole = role;
      this.roleDetail = await this.api.getRoleDetail(role);
      this.historyItems = await this.api.history();
      this.message = `Checklist de ${role} iniciado`;
    });
  }

  async completeItem(runId: number, itemId: number): Promise<void> {
    await this.run(async () => {
      await this.api.completeItem(runId, itemId);
      if (this.selectedRole) this.roleDetail = await this.api.getRoleDetail(this.selectedRole);
      this.historyItems = await this.api.history();
    });
  }

  async resetTraining(): Promise<void> {
    if (!this.isAdmin) return;
    await this.run(async () => {
      await this.api.resetTraining();
      this.roleDetail = null;
      this.selectedRole = '';
      this.message = 'Capacitación reiniciada';
      await this.reload();
    });
  }

  private async run(fn: () => Promise<void>): Promise<void> {
    this.busy = true;
    this.error = '';
    this.message = '';
    try {
      await fn();
    } catch (err: any) {
      this.error = err?.error?.message ?? 'Operación de capacitación fallida';
    } finally {
      this.busy = false;
    }
  }
}
