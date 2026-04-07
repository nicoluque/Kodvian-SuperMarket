import { Injectable } from '@angular/core';

export type OperatingMode = 'MostradorExpress' | 'MiniMarketFull' | 'CajaRapida' | 'TotemQrOnly';

export interface OperatingModules {
  tablet: boolean;
  envases: boolean;
  cuentaCorriente: boolean;
  comprasSugeridas: boolean;
  reportes: boolean;
}

export interface OperatingConfig {
  mode: OperatingMode;
  modules: OperatingModules;
}

export type OperatingModuleKey = keyof OperatingModules;

const DEFAULT_CONFIG: OperatingConfig = {
  mode: 'MiniMarketFull',
  modules: {
    tablet: true,
    envases: true,
    cuentaCorriente: true,
    comprasSugeridas: true,
    reportes: true
  }
};

@Injectable({ providedIn: 'root' })
export class OperatingModeService {
  private readonly modeKey = 'operating_mode';
  private readonly modulesKey = 'operating_modules';
  private readonly deviceTypeKey = 'operating_device_type';

  getConfig(): OperatingConfig {
    const modeRaw = (localStorage.getItem(this.modeKey) as OperatingMode | null) ?? DEFAULT_CONFIG.mode;
    const mode: OperatingMode = this.isValidMode(modeRaw) ? modeRaw : DEFAULT_CONFIG.mode;

    let modules = DEFAULT_CONFIG.modules;
    const modulesRaw = localStorage.getItem(this.modulesKey);
    if (modulesRaw) {
      try {
        const parsed = JSON.parse(modulesRaw);
        modules = {
          tablet: parsed?.tablet ?? modules.tablet,
          envases: parsed?.envases ?? modules.envases,
          cuentaCorriente: parsed?.cuentaCorriente ?? modules.cuentaCorriente,
          comprasSugeridas: parsed?.comprasSugeridas ?? modules.comprasSugeridas,
          reportes: parsed?.reportes ?? modules.reportes
        };
      } catch {
      }
    }

    if (mode === 'MostradorExpress') {
      modules = { ...modules, tablet: false, envases: false, cuentaCorriente: false };
    }
    if (mode === 'CajaRapida') {
      modules = { ...modules, envases: false, cuentaCorriente: false };
    }
    if (mode === 'TotemQrOnly') {
      modules = { ...modules, tablet: true, envases: false, cuentaCorriente: true };
    }

    return { mode, modules };
  }

  setConfig(mode: OperatingMode, modules?: Partial<OperatingModules>): void {
    const nextModules = { ...DEFAULT_CONFIG.modules, ...modules };
    localStorage.setItem(this.modeKey, mode);
    localStorage.setItem(this.modulesKey, JSON.stringify(nextModules));
  }

  getDeviceType(): string {
    return localStorage.getItem(this.deviceTypeKey) ?? '';
  }

  getPreferredPosRoute(): string {
    const cfg = this.getConfig();
    const deviceType = this.getDeviceType();

    if (cfg.mode === 'TotemQrOnly') {
      return '/pos/tablet/nueva';
    }

    if (deviceType === 'Tablet' && cfg.modules.tablet) {
      return '/pos/tablet/nueva';
    }

    return '/pos/caja/apertura';
  }

  setFromDeviceValidation(payload: any): void {
    const mode = payload?.operatingMode as OperatingMode | undefined;
    const modules = payload?.enabledModules as Partial<OperatingModules> | undefined;
    const deviceType = typeof payload?.deviceType === 'string' ? payload.deviceType : '';
    if (mode && this.isValidMode(mode)) {
      this.setConfig(mode, modules);
    }
    if (deviceType) {
      localStorage.setItem(this.deviceTypeKey, deviceType);
    }
  }

  hasModule(moduleKey: OperatingModuleKey): boolean {
    return !!this.getConfig().modules[moduleKey];
  }

  getModeLabel(mode: OperatingMode): string {
    switch (mode) {
      case 'MostradorExpress':
        return 'Mostrador Express';
      case 'CajaRapida':
        return 'Caja Rapida';
      case 'TotemQrOnly':
        return 'Totem QR';
      default:
        return 'MiniMarket Full';
    }
  }

  private isValidMode(value: string): value is OperatingMode {
    return value === 'MostradorExpress' || value === 'MiniMarketFull' || value === 'CajaRapida' || value === 'TotemQrOnly';
  }
}
