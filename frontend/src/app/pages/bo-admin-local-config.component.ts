import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { BoAdminService } from '../core/services/bo-admin.service';
import { OperatingMode, OperatingModeService } from '../core/services/operating-mode.service';

@Component({
  standalone: true,
  selector: 'app-bo-admin-local-config',
  imports: [CommonModule, FormsModule],
  template: `
    <main class="wrap">
      <h1>Configuracion Local #{{ storeId }}</h1>

      <section class="card">
        <h3>Modo operativo (comercial)</h3>
        <div class="grid">
          <label>Modo
            <select [(ngModel)]="operatingMode">
              <option value="MiniMarketFull">MiniMarket Full</option>
              <option value="MostradorExpress">Mostrador Express</option>
              <option value="CajaRapida">Caja Rapida</option>
              <option value="TotemQrOnly">Totem QR</option>
            </select>
          </label>
          <label><input type="checkbox" [(ngModel)]="modules.tablet" /> Habilitar Tablet</label>
          <label><input type="checkbox" [(ngModel)]="modules.envases" /> Habilitar Envases</label>
          <label><input type="checkbox" [(ngModel)]="modules.cuentaCorriente" /> Habilitar Cuenta corriente</label>
          <label><input type="checkbox" [(ngModel)]="modules.comprasSugeridas" /> Habilitar Compras sugeridas</label>
          <label><input type="checkbox" [(ngModel)]="modules.reportes" /> Habilitar Reportes</label>
        </div>
        <div class="row">
          <button (click)="applyPreset()">Aplicar preset del modo</button>
          <button (click)="saveModeOnly()">Guardar modo operativo</button>
        </div>
      </section>

      <section class="card">
        <h3>Turnos y transicion Totem</h3>
        <div class="grid">
          <label>Zona horaria
            <input type="text" [(ngModel)]="shiftConfig.timezone" placeholder="America/Argentina/Buenos_Aires" />
          </label>
          <label>Inicio turno manana
            <input type="time" [(ngModel)]="shiftConfig.morningStart" />
          </label>
          <label>Inicio ventana cierre manana
            <input type="time" [(ngModel)]="shiftConfig.morningCloseWindowStart" />
          </label>
          <label>Fin ventana cierre manana
            <input type="time" [(ngModel)]="shiftConfig.morningCloseWindowEnd" />
          </label>
          <label>Fin turno tarde
            <input type="time" [(ngModel)]="shiftConfig.afternoonEnd" />
          </label>
          <label>Minutos de gracia
            <input type="number" min="1" [(ngModel)]="shiftConfig.graceMinutes" />
          </label>
        </div>
        <div class="row">
          <button (click)="saveShiftConfig()">Guardar turnos</button>
        </div>
      </section>

      <h3>JSON avanzado</h3>
      <textarea rows="14" [(ngModel)]="jsonText"></textarea>
      <button (click)="save()">Guardar</button>
      <p *ngIf="message" class="ok">{{ message }}</p>
      <p *ngIf="error" class="error">{{ error }}</p>
    </main>
  `,
  styles:[
    `.wrap{padding:20px;font-family:Arial,sans-serif;display:flex;flex-direction:column;gap:10px}`,
    `.card{border:1px solid #ddd;border-radius:10px;padding:12px}`,
    `.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:8px}`,
    `.row{display:flex;gap:8px;margin-top:8px}`,
    `textarea{width:100%;font-family:monospace}`,
    `.ok{color:#0a7a32}`,
    `.error{color:#b3261e}`
  ]
})
export class BoAdminLocalConfigComponent {
  storeId = 0;
  jsonText = '{}';
  message = '';
  error = '';

  operatingMode: OperatingMode = 'MiniMarketFull';
  modules = {
    tablet: true,
    envases: true,
    cuentaCorriente: true,
    comprasSugeridas: true,
    reportes: true
  };

  shiftConfig = {
    timezone: 'America/Argentina/Buenos_Aires',
    morningStart: '07:30',
    morningCloseWindowStart: '14:00',
    morningCloseWindowEnd: '15:00',
    afternoonEnd: '22:00',
    graceMinutes: 90
  };

  constructor(
    private readonly route: ActivatedRoute,
    private readonly api: BoAdminService,
    private readonly operatingModeService: OperatingModeService
  ) {
    this.storeId = Number(this.route.snapshot.paramMap.get('id'));
    void this.load();
  }

  async load(): Promise<void> {
    const data = await this.api.getStoreSettings(this.storeId);
    this.jsonText = JSON.stringify(data, null, 2);

    const modeRaw = data?.operatingMode as OperatingMode | undefined;
    if (modeRaw === 'MiniMarketFull' || modeRaw === 'MostradorExpress' || modeRaw === 'CajaRapida' || modeRaw === 'TotemQrOnly') {
      this.operatingMode = modeRaw;
    }

    this.modules = {
      tablet: data?.enabledModules?.tablet ?? true,
      envases: data?.enabledModules?.envases ?? true,
      cuentaCorriente: data?.enabledModules?.cuentaCorriente ?? true,
      comprasSugeridas: data?.enabledModules?.comprasSugeridas ?? true,
      reportes: data?.enabledModules?.reportes ?? true
    };

    try {
      const shift = await this.api.getStoreShiftConfig(this.storeId);
      this.shiftConfig = {
        timezone: shift?.timezone ?? 'America/Argentina/Buenos_Aires',
        morningStart: shift?.morningStart ?? '07:30',
        morningCloseWindowStart: shift?.morningCloseWindowStart ?? '14:00',
        morningCloseWindowEnd: shift?.morningCloseWindowEnd ?? '15:00',
        afternoonEnd: shift?.afternoonEnd ?? '22:00',
        graceMinutes: Number(shift?.graceMinutes ?? 90)
      };
    } catch {
    }
  }

  async save(): Promise<void> {
    try { await this.api.updateStoreSettings(this.storeId, JSON.parse(this.jsonText || '{}')); this.message='Configuracion guardada'; this.error=''; }
    catch (e:any){ this.error=e?.error?.message ?? 'JSON invalido o error'; }
  }

  applyPreset(): void {
    if (this.operatingMode === 'MostradorExpress') {
      this.modules = { tablet: false, envases: false, cuentaCorriente: false, comprasSugeridas: true, reportes: true };
      return;
    }

    if (this.operatingMode === 'CajaRapida') {
      this.modules = { tablet: false, envases: false, cuentaCorriente: false, comprasSugeridas: false, reportes: true };
      return;
    }

    if (this.operatingMode === 'TotemQrOnly') {
      this.modules = { tablet: true, envases: false, cuentaCorriente: true, comprasSugeridas: false, reportes: true };
      return;
    }

    this.modules = { tablet: true, envases: true, cuentaCorriente: true, comprasSugeridas: true, reportes: true };
  }

  async saveModeOnly(): Promise<void> {
    try {
      const current = JSON.parse(this.jsonText || '{}');
      const payload = {
        ...current,
        operatingMode: this.operatingMode,
        enabledModules: this.modules
      };
      await this.api.updateStoreSettings(this.storeId, payload);
      this.jsonText = JSON.stringify(payload, null, 2);
      this.operatingModeService.setConfig(this.operatingMode, this.modules);
      this.message = 'Modo operativo guardado';
      this.error = '';
    } catch (e: any) {
      this.error = e?.error?.message ?? 'No se pudo guardar modo operativo';
    }
  }

  async saveShiftConfig(): Promise<void> {
    try {
      await this.api.updateStoreShiftConfig(this.storeId, this.shiftConfig);
      this.message = 'Turnos guardados';
      this.error = '';
    } catch (e: any) {
      this.error = e?.error?.message ?? 'No se pudo guardar turnos';
    }
  }
}
