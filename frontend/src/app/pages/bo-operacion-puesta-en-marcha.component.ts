import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BoOperacionService } from '../core/services/bo-operacion.service';
import { BoModuleNavComponent } from '../shared/components/bo-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-bo-operacion-puesta-en-marcha',
  imports: [CommonModule, FormsModule, BoModuleNavComponent],
  template: `
    <main class="wrap">
      <div class="bg-orb orb-a"></div>
      <div class="bg-orb orb-b"></div>
      <app-bo-module-nav />

      <section class="content">
        <header class="hero">
          <div>
            <h1>Puesta en marcha</h1>
            <p>Recorrido guiado para dejar local, usuarios y dispositivos listos para operar.</p>
          </div>
          <div class="hero-actions">
            <span class="hero-pill">Paso {{ step }} / 6</span>
            <span class="hero-pill">Avance: {{ progressPercent }}%</span>
          </div>
        </header>

        <section class="card progress-card">
          <div class="progress-line">
            <span>Paso {{ step }} de 6</span>
            <div class="bar"><div class="fill" [style.width.%]="progressPercent"></div></div>
          </div>
        </section>

        <section class="card" *ngIf="step === 1">
        <h3>1) Datos del local</h3>
        <input placeholder="Nombre local" [(ngModel)]="local.localName" />
        <input placeholder="Dirección" [(ngModel)]="local.address" />
        <input placeholder="Teléfono" [(ngModel)]="local.phone" />
         <button class="btn btn-primary" [disabled]="actionBusy" (click)="saveLocalInfo()">Guardar y continuar</button>
        </section>

        <section class="card" *ngIf="step === 2">
        <h3>2) Dispositivos</h3>
        <div class="row">
          <input placeholder="Nombre" [(ngModel)]="device.deviceName" />
           <select [(ngModel)]="device.deviceType">
            <option value="CashRegister">Caja</option>
            <option value="Tablet">Tablet</option>
          </select>
          <input type="number" placeholder="Parent caja (opcional)" [(ngModel)]="device.parentCashRegisterDeviceId" />
           <button class="btn btn-secondary" [disabled]="actionBusy" (click)="createDevice()">Crear dispositivo</button>
        </div>
        <ul class="list"><li *ngFor="let d of devices">#{{ d.id }} {{ d.deviceName }} ({{ d.deviceType }}) parent: {{ d.parentCashRegisterDeviceId || '-' }}</li></ul>
        </section>

        <section class="card" *ngIf="step === 3">
        <h3>3) Usuarios y PIN</h3>
        <div class="row">
          <input placeholder="Usuario" [(ngModel)]="user.username" />
          <input type="password" placeholder="Contraseña" [(ngModel)]="user.password" />
          <input placeholder="PIN" [(ngModel)]="user.pin" />
          <select [(ngModel)]="user.role">
            <option value="Operator">Operador</option>
            <option value="Supervisor">Supervisor</option>
            <option value="Admin">Administrador</option>
          </select>
           <button class="btn btn-secondary" [disabled]="actionBusy" (click)="createUser()">Crear usuario</button>
        </div>
        <ul class="list"><li *ngFor="let u of users">#{{ u.id }} {{ u.username }} ({{ u.role }})</li></ul>
        </section>

        <section class="card" *ngIf="step === 4">
        <h3>4) Parámetros clave</h3>
        <div class="row">
          <input type="number" placeholder="Compra mínima promo" [(ngModel)]="settings.bigPurchaseMinAmount" />
          <input type="number" placeholder="Tope descuento %" [(ngModel)]="settings.bigPurchaseDiscountCapPercent" />
          <input type="number" placeholder="Recargo cigarrillos %" [(ngModel)]="settings.cigaretteSurchargePercent" />
          <input type="number" placeholder="Mora mensual %" [(ngModel)]="settings.lateFeePercentMonthly" />
        </div>
        <label class="check"><input type="checkbox" [(ngModel)]="settings.lateFeeEnabled" /> Mora habilitada</label>
         <button class="btn btn-primary" [disabled]="actionBusy" (click)="saveSettings()">Guardar parámetros</button>
        </section>

        <section class="card" *ngIf="step === 5">
        <h3>5) Pruebas operativas</h3>
        <ul>
          <li>Crear carrito y enviar a caja</li>
          <li>Cobro cash y cierre parcial</li>
          <li>Validar reporte simple</li>
        </ul>
         <button class="btn btn-secondary" [disabled]="actionBusy" (click)="next()">Marcar pruebas completas</button>
        </section>

        <section class="card" *ngIf="step === 6">
        <h3>6) Contingencia</h3>
        <p>Descargar manual, talonario y catálogo de emergencia.</p>
        <div class="row">
          <a class="btn btn-secondary" [href]="ops.getManualKitUrl()" target="_blank" rel="noreferrer">Manual + Kit</a>
          <a class="btn btn-secondary" [href]="ops.getEmergencyCatalogUrl()" target="_blank" rel="noreferrer">Catálogo de emergencia</a>
        </div>
        <p>Checklist disponible en /bo/operacion/checklist</p>
        </section>

        <div class="nav card">
         <button class="btn btn-secondary" (click)="prev()" [disabled]="step===1 || actionBusy">Anterior</button>
         <button class="btn btn-secondary" (click)="next()" [disabled]="step===6 || actionBusy">Siguiente</button>
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
    `.hero-actions{display:flex;flex-direction:column;gap:8px;align-items:flex-end}`,
    `.hero-pill{display:inline-flex;align-items:center;background:rgba(255,255,255,.22);border:1px solid rgba(255,255,255,.4);border-radius:999px;padding:6px 10px;font-size:12px;font-weight:700;color:#fff}`,
    `.card{border:1px solid rgba(16,94,67,.16);border-radius:18px;padding:14px;background:rgba(255,255,255,.94);box-shadow:0 14px 32px rgba(33,73,57,.12);backdrop-filter:blur(2px);display:flex;flex-direction:column;gap:10px}`,
    `.progress-card{padding:12px 14px}`,
    `.progress-line{display:flex;flex-direction:column;gap:8px;color:#1f5f49;font-weight:700}`,
    `.bar{height:10px;border-radius:999px;background:#e6f2ec;overflow:hidden}`,
    `.fill{height:100%;background:linear-gradient(120deg,#2f8e67,#1f6f50)}`,
    `.row{display:flex;gap:8px;flex-wrap:wrap;align-items:center}`,
    `.row > *{flex:1 1 200px}`,
    `.row .btn,.row .check{flex:0 0 auto}`,
    `.list{margin:0;padding-left:20px;color:#265b47}`,
    `.check{display:inline-flex;align-items:center;gap:6px;color:#1f5b44;font-weight:700}`,
    `.nav{display:flex;gap:8px;flex-wrap:wrap}`,
    `input,select{border:1px solid #d5e6dd;border-radius:10px;padding:10px 12px;background:#fff;outline:none;min-height:44px}`,
    `input:focus,select:focus{border-color:#2f8e67;box-shadow:0 0 0 3px rgba(47,142,103,.12)}`,
    `.btn{border:1px solid transparent;border-radius:10px;padding:10px 12px;font-weight:700;cursor:pointer;text-decoration:none;display:inline-flex;align-items:center;justify-content:center;min-height:42px;transition:transform .14s ease,box-shadow .2s ease,background .2s ease}`,
    `.btn-primary{border-color:#2c7f5d;background:linear-gradient(135deg,#2f8e67,#1f6f50);color:#fff}`,
    `.btn-secondary{border-color:#2b7f5c;background:#e7f4ed;color:#15543d}`,
    `.btn:hover:not([disabled]){transform:translateY(-1px);box-shadow:0 8px 16px rgba(31,111,80,.18)}`,
    `.btn:focus-visible{outline:none;box-shadow:0 0 0 3px rgba(47,142,103,.2)}`,
    `.btn[disabled]{opacity:.6;cursor:not-allowed}`,
    `.alert{border-radius:12px;padding:12px 14px}`,
    `.alert.ok{background:#edf8f2;border:1px solid #bfd9cc;color:#16543d;font-weight:700}`,
    `.alert.error{background:#feefef;border:1px solid #f3c6c6;color:#9f1f1f;font-weight:700}`,
    `@media (max-width: 900px){.wrap{padding:16px}.hero-actions{align-items:flex-start;width:100%}.row > *{flex:1 1 100%}.row .btn,.row .check{flex:1 1 100%}.nav .btn{width:100%}}`
  ]
})
export class BoOperacionPuestaEnMarchaComponent {
  step = 1;
  message = '';
  error = '';
  actionBusy = false;

  local = { localName: '', address: '', phone: '' };
  users: any[] = [];
  devices: any[] = [];

  user = { username: '', password: '', pin: '', role: 'Operator' };
  device: any = { deviceName: '', deviceType: 'CashRegister', parentCashRegisterDeviceId: undefined as number | undefined };

  settings: any = {
    bigPurchaseMinAmount: 0,
    bigPurchaseDiscountCapPercent: 0,
    cigaretteSurchargePercent: 0,
    cigaretteSurchargeMethods: ['Cash', 'Card'],
    lateFeeEnabled: true,
    lateFeePercentMonthly: 0
  };

  constructor(public readonly ops: BoOperacionService) {
    void this.bootstrap();
  }

  get progressPercent(): number {
    return Math.round((this.step / 6) * 100);
  }

  async bootstrap(): Promise<void> {
    try {
      this.local = await this.ops.getLocalInfo();
      this.users = await this.ops.getUsers();
      this.devices = await this.ops.getDevices();
      const settingsMap = await this.ops.getPosSettings();
      this.settings.bigPurchaseMinAmount = Number(settingsMap['big_purchase_min_amount'] ?? 0);
      this.settings.bigPurchaseDiscountCapPercent = Number(settingsMap['big_purchase_discount_cap_percent'] ?? 0);
      this.settings.cigaretteSurchargePercent = Number(settingsMap['cigarette_surcharge_percent'] ?? 0);
      this.settings.lateFeeEnabled = (settingsMap['late_fee_enabled'] ?? 'true') === 'true';
      this.settings.lateFeePercentMonthly = Number(settingsMap['late_fee_percent_monthly'] ?? 0);
    } catch {
    }
  }

  next(): void {
    if (this.step < 6) this.step += 1;
  }

  prev(): void {
    if (this.step > 1) this.step -= 1;
  }

  async saveLocalInfo(): Promise<void> {
    await this.withBusy(async () => {
      await this.ops.saveLocalInfo(this.local);
      this.message = 'Datos del local guardados';
      this.error = '';
      this.next();
    });
  }

  async createUser(): Promise<void> {
    this.error = '';
    try {
      await this.withBusy(async () => {
        await this.ops.createUser(this.user);
        this.users = await this.ops.getUsers();
        this.message = 'Usuario creado';
        this.user = { username: '', password: '', pin: '', role: 'Operator' };
      });
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo crear usuario';
    }
  }

  async createDevice(): Promise<void> {
    this.error = '';
    try {
      await this.withBusy(async () => {
        await this.ops.createDevice(this.device);
        this.devices = await this.ops.getDevices();
        this.message = 'Dispositivo creado';
        this.device = { deviceName: '', deviceType: 'CashRegister', parentCashRegisterDeviceId: undefined };
      });
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo crear dispositivo';
    }
  }

  async saveSettings(): Promise<void> {
    this.error = '';
    try {
      await this.withBusy(async () => {
        await this.ops.updatePosSettings(this.settings);
        this.message = 'Parámetros guardados';
        this.next();
      });
    } catch (err: any) {
      this.error = err?.error?.message ?? 'No se pudo guardar parámetros';
    }
  }

  private async withBusy<T>(fn: () => Promise<T>): Promise<T> {
    this.actionBusy = true;
    try {
      return await fn();
    } finally {
      this.actionBusy = false;
    }
  }
}
