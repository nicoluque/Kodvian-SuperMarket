# Reestructuracion Operativa Argentina

## Objetivo

Mejorar usabilidad y conversion comercial para minimarket, verduleria y carniceria con foco en idioma castellano y flujos simples de operacion diaria.

## Principios

- Idioma visible al usuario final en castellano argentino.
- Menos pasos para vender y cobrar.
- Modo operativo segun rubro.
- Backoffice orientado a tareas diarias, no a menus tecnicos.

## Etapas

### Etapa A (en ejecucion)

- [x] Home de inicio operativo con accesos por modo.
- [x] Setup POS en castellano.
- [x] Apertura de caja con PIN visible en formulario.
- [x] Validacion temprana de token de dispositivo.

### Etapa B

- [x] Login backoffice comercial (usuario/contrasena/PIN) sin depender de JWT manual.
- [x] Navegacion por tareas base: Lo diario, Compras, Reportes, Capacitacion.
- [~] Reduccion de placeholders visibles en rutas productivas (parcial).
  - POS: rutas `caja/venta/:cartId`, `caja/cobro/:saleId` y `caja/cierre` ahora usan flujo real de caja (edicion/cobro/cierre) en lugar de placeholder.
  - BO: rutas `productos`, `stock`, `clientes`, `claims`, `rrhh`, `kanban` muestran vista operativa minima con acciones y accesos rapidos.
  - BO fase 2: `stock` y `clientes` incluyen tabla operativa con carga real via API y filtro rapido.
  - BO fase 2: `claims`, `rrhh` y `kanban` incluyen listados operativos reales y acciones basicas (retirar/acreditar, ver inconsistencias, completar tareas).
  - Endurecimiento UX: paginacion simple, estados vacios claros y manejo de errores homogeneo en vistas operativas BO.

### Etapa C

- [x] Modos por rubro base:
  - Mostrador Express (verduleria/carniceria)
  - MiniMarket Full
  - Caja Rapida
- [x] Presets por tenant/store para activar o desactivar modulos (via Store Settings).
- [x] Guard por modulos en rutas POS/BO para bloquear acceso a modulos deshabilitados.
- [x] Navegacion modular reusable en POS y Backoffice (menu por tareas).
- [~] Aplicacion visual de modulos por modo en todas las pantallas (pendiente de cobertura completa en vistas secundarias).
  - Cobertura ampliada en vistas secundarias BO (importaciones, operacion, onboarding, demo, admin) y placeholders con navegacion contextual por modo.

### Etapa D

- [~] QA funcional por rubro.
  - Matriz integrada en `CHECKLIST_IMPLEMENTACION.md` (seccion "QA por rubro").
- [x] Script de smoke por modo.
  - `scripts/smoke_pos_flow.ps1` ahora permite `-OperatingMode` y valida modo devuelto por `auth/device/validate`.
  - Ejecuciones verificadas: `MiniMarketFull`, `MostradorExpress`, `CajaRapida`.
- [x] Guion demo comercial especifico por rubro.
  - Escenarios por minimarket, verduleria y carniceria agregados en `GUION_DEMO.md`.

### Optimizacion tecnica complementaria

- [x] Reduccion del bundle inicial con lazy loading de rutas principales.
  - `app.routes.ts` migra a `loadChildren` para POS/BO y `loadComponent` para inicio/prints.
  - Resultado: `initial total` baja por debajo del budget (aprox. 349 kB).
  - Validacion posterior: smoke full OK + smoke por modo OK (`MiniMarketFull`, `MostradorExpress`, `CajaRapida`).

- [x] Endurecimiento de comunicacion frontend-backend para operacion diaria.
  - POS setup auto-continua a caja si existe token valido, y solo exige reconfigurar cuando token no valida.
  - Sync offline se ejecuta solo con credenciales POS completas (token + sesion operador) y en contexto POS.
  - Se reducen toasts de 401 ruidosos en setup/sync.
  - Backend de stock corrige captura de `SessionId` para trazabilidad de operador.
  - Inicio de API pasa a `Database.Migrate()` para alinear esquema en arranque.
  - StockService reduce `SaveChanges` por item en reclamos/creditos (batch por operacion).
  - `auth/operator-session` ahora exige `DeviceAuth` (sesion de operador ligada a dispositivo valido).
  - Apertura de caja incorpora estado guiado (dispositivo/operador), bloqueo preventivo de apertura y mensajes de error mas precisos.
  - Guard POS revalida token por sesion de navegador para evitar estados locales viejos.

- [x] Rediseño operativo de Caja Inbox (UX por tarea).
  - `Inbox` queda enfocado en bandeja y pendientes (sin mezcla de edicion/cobro/cierre).
  - Flujos separados: `venta/:cartId`, `cobro/:cartId`, `cierre` con pantallas dedicadas.
  - Navegacion mas clara para operador: Bandeja -> Venta -> Cobro -> Cierre.

## Criterio de aceptacion de idioma

- Cero textos en ingles en flujos principales de POS y Backoffice.
- Mensajes de error orientados a accion en castellano.
- Comprobantes y exportes en castellano.
