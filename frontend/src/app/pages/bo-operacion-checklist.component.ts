import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { BoChecklistItem, BoOperacionService } from '../core/services/bo-operacion.service';
import { BoModuleNavComponent } from '../shared/components/bo-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-bo-operacion-checklist',
  imports: [CommonModule, BoModuleNavComponent],
  template: `
    <main class="wrap">
      <div class="bg-orb orb-a"></div>
      <div class="bg-orb orb-b"></div>
      <app-bo-module-nav />

      <section class="content">
        <header class="hero">
          <div>
            <h1>Checklist de puesta en marcha</h1>
            <p>Control rápido de tareas operativas para dejar el local listo.</p>
          </div>
          <div class="hero-actions">
            <span class="hero-pill">Completadas: {{ completedItems }} / {{ items.length }}</span>
            <button class="btn" (click)="load()">Recargar</button>
          </div>
        </header>

        <section class="card list" *ngIf="items.length">
          <div class="item" *ngFor="let item of items" [class.done-item]="item.done">
            <label>
              <input type="checkbox" [checked]="item.done" (change)="toggle(item, $any($event.target).checked)" />
              <span>{{ item.label }}</span>
            </label>
            <small>{{ item.updatedAt || '-' }}</small>
          </div>
        </section>

        <section class="card" *ngIf="!items.length && !error">
          <p class="meta">No hay ítems de checklist cargados.</p>
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
    `.card{border:1px solid rgba(16,94,67,.16);border-radius:18px;padding:14px;background:rgba(255,255,255,.94);box-shadow:0 14px 32px rgba(33,73,57,.12);backdrop-filter:blur(2px);display:flex;flex-direction:column;gap:10px}`,
    `.list{gap:8px}`,
    `.item{display:flex;justify-content:space-between;align-items:center;gap:8px;padding:12px;border:1px solid #e2efe8;border-radius:12px;background:#fbfefd;transition:background .2s ease,border-color .2s ease}`,
    `.item:hover{background:#f4fbf8}`,
    `.done-item{border-color:#9fd4b8;background:#f2faf6}`,
    `.item label{display:flex;align-items:center;gap:8px;color:#1c5a44;font-weight:700}`,
    `.item small{color:#3b6555}`,
    `.btn{border:1px solid #2b7f5c;background:#e7f4ed;color:#15543d;border-radius:10px;padding:10px 12px;font-weight:700;cursor:pointer;min-height:42px;transition:transform .14s ease,box-shadow .2s ease,background .2s ease}`,
    `.btn:hover{background:#dff2e9;transform:translateY(-1px);box-shadow:0 8px 16px rgba(31,111,80,.18)}`,
    `.btn:focus-visible{outline:none;box-shadow:0 0 0 3px rgba(47,142,103,.2)}`,
    `.meta{margin:0;color:#2f5f4c}`,
    `.alert{border-radius:12px;padding:12px 14px}`,
    `.alert.error{background:#feefef;border:1px solid #f3c6c6;color:#9f1f1f;font-weight:700}`,
    `@media (max-width: 900px){.wrap{padding:16px}.hero-actions{align-items:flex-start;width:100%}.item{flex-direction:column;align-items:flex-start}}`
  ]
})
export class BoOperacionChecklistComponent {
  items: BoChecklistItem[] = [];
  error = '';

  get completedItems(): number {
    return this.items.filter(x => x.done).length;
  }

  constructor(private readonly ops: BoOperacionService) {
    void this.load();
  }

  async load(): Promise<void> {
    this.error = '';
    try {
      this.items = await this.ops.getChecklist();
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo cargar checklist';
    }
  }

  async toggle(item: BoChecklistItem, done: boolean): Promise<void> {
    this.error = '';
    try {
      this.items = await this.ops.toggleChecklist(item.key, done);
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo guardar checklist';
    }
  }
}
