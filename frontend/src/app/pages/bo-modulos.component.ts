import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthJwtService } from '../core/services/auth-jwt.service';
import { BoModuleNavComponent } from '../shared/components/bo-module-nav.component';

@Component({
  standalone: true,
  selector: 'app-bo-modulos',
  imports: [CommonModule, RouterLink, BoModuleNavComponent],
  template: `
    <main class="wrap">
      <app-bo-module-nav />
      <header class="hero">
        <h1>Mapa de módulos</h1>
        <p>Accesos directos disponibles para tu rol actual.</p>
      </header>

      <section class="card">
        <h3>Comercial</h3>
        <div class="links">
          <a routerLink="/bo/dashboard-gerencial">Panel gerencial</a>
          <a routerLink="/bo/compras/manuales">Compras manuales</a>
          <a routerLink="/bo/compras/sugeridas">Compras sugeridas</a>
          <a routerLink="/bo/exportaciones">Exportaciones</a>
          <a routerLink="/bo/productos" *ngIf="canSeeProducts">Productos</a>
          <a routerLink="/bo/totem/transiciones" *ngIf="canSeeProducts">Totem transición</a>
        </div>
      </section>

      <section class="card" *ngIf="canSeeDataFlows">
        <h3>Datos</h3>
        <div class="links">
          <a routerLink="/bo/importaciones/unificada">Importacion unificada</a>
          <a routerLink="/bo/importaciones/ajuste-masivo-stock">Ajuste masivo stock</a>
          <a routerLink="/bo/importaciones">Importaciones</a>
          <a routerLink="/bo/importaciones/stock-inicial">Stock inicial</a>
        </div>
      </section>

      <section class="card" *ngIf="canSeeOpsFlows">
        <h3>Operación</h3>
        <div class="links">
          <a routerLink="/bo/operacion/checklist">Checklist</a>
          <a routerLink="/bo/operacion/descargas">Descargas</a>
          <a routerLink="/bo/operacion/puesta-en-marcha">Puesta en marcha</a>
          <a routerLink="/bo/onboarding">Onboarding</a>
        </div>
      </section>

      <section class="card" *ngIf="canSeeAdminFlows">
        <h3>Administración</h3>
        <div class="links">
          <a routerLink="/bo/admin/empresa">Empresa</a>
          <a routerLink="/bo/admin/locales">Locales</a>
          <a routerLink="/bo/admin/branding">Branding</a>
          <a routerLink="/bo/admin/demo">Demo</a>
        </div>
      </section>
    </main>
  `,
  styles: [
    `.wrap{padding:16px;display:flex;flex-direction:column;gap:10px}`,
    `.hero{background:linear-gradient(135deg,#0f6547,#2b8c69);color:#fff;border-radius:14px;padding:14px}`,
    `.hero h1{margin:0 0 6px}`,
    `.hero p{margin:0;color:rgba(255,255,255,.92)}`,
    `.card{background:#fff;border:1px solid #dce9e3;border-radius:12px;padding:12px}`,
    `.card h3{margin:0 0 8px;color:#1B4D3E}`,
    `.links{display:flex;flex-wrap:wrap;gap:8px}`,
    `.links a{text-decoration:none;color:#1f7f57;background:#e7f4ee;border:1px solid #c6e5d6;border-radius:999px;padding:6px 10px;font-size:13px}`
  ]
})
export class BoModulosComponent {
  canSeeProducts = false;
  canSeeDataFlows = false;
  canSeeOpsFlows = false;
  canSeeAdminFlows = false;

  constructor(authJwt: AuthJwtService) {
    this.canSeeProducts = authJwt.hasAnyRole(['Admin', 'Supervisor']);
    this.canSeeDataFlows = authJwt.hasAnyRole(['Admin', 'Supervisor', 'Manager']);
    this.canSeeOpsFlows = authJwt.hasAnyRole(['Admin', 'Supervisor', 'Manager']);
    this.canSeeAdminFlows = authJwt.hasAnyRole(['Admin', 'Supervisor']);
  }
}
