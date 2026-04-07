# AGENT - Especificacion Funcional y Operativa

## 1. Proposito

Este archivo define como funciona Kodvian SuperMarket a nivel negocio, operacion diaria y criterios tecnicos minimos para mantener coherencia entre POS, Backoffice (BO) y API.

Es una guia viva para:

- implementaciones nuevas,
- correccion de bugs,
- QA funcional,
- onboarding de equipo tecnico/operativo.

## 2. Vision del producto

Kodvian SuperMarket es una plataforma integral para minimarkets, autoservicios, verdulerias, carnicerias y supermercados de cercania.

Objetivos principales:

- sostener continuidad operativa de caja y piso de venta,
- dar control diario comercial y financiero,
- asegurar trazabilidad de stock/compras/proveedores,
- escalar por tenant y por sucursal.

## 3. Alcance funcional (actual)

### 3.1 POS (operacion)

- apertura, movimientos y cierre de caja,
- venta y devolucion,
- cobro con medios multiples,
- flujo por tareas (inbox, venta, cobro, cierre),
- operacion guiada con sesion de operador.

### 3.2 Backoffice (gestion)

- dashboard gerencial,
- stock operativo (resumen, ajustes, movimientos),
- reclamos a proveedor (retiro, credito, reposicion fisica),
- transformaciones de stock (plantillas y aplicacion),
- compras sugeridas y conversion a compra draft,
- productos pendientes para completar/activar desde BO,
- gestion de transiciones Totem por cambio de turno,
- exportaciones, capacitacion, admin/demo.

### 3.3 Datos y soporte operativo

- multi-tenant y multi-sucursal,
- branding por tenant/store,
- scripts smoke y documentacion de operacion local,
- logs de API en storage para troubleshooting.

## 4. Actores y permisos

Roles habilitados para modulos BO criticos (stock, compras sugeridas, dashboard):

- `Admin`
- `Supervisor`
- `Manager`

Regla base:

- Si no hay sesion operador valida (`X-Operator-Session`) el endpoint protegido responde `401`.
- Si la sesion existe pero rol no habilita, responde `403`.

## 5. Contexto operativo y autenticacion

### 5.1 Cabeceras relevantes

- `X-Operator-Session`: token de sesion operador (obligatorio en endpoints con `[OperatorSessionAuth]`).
- `X-Device-Token`: token de dispositivo (obligatorio donde hay `[DeviceAuth]`).
- `X-Store-Id`: sucursal activa para scope BO.
- `X-Tenant-Id`: opcional para resolver tenant de forma explicita.

### 5.2 Convenciones de backend

Filtros (`Filters/AuthFilters.cs`):

- `[DeviceAuth]` carga en `HttpContext.Items`: `DeviceId`, `StoreId`, `TenantId` (si aplica).
- `[OperatorSessionAuth]` carga: `SessionId`, `SessionUsuarioId`.

Resolucion de sucursal/tenant en controllers BO:

- prioridad a `X-Store-Id`,
- fallback a store de dispositivo o primer tenant/store segun flujo.

## 6. Modos operativos por rubro

Definidos para ajustar UX y modulos visibles:

- `MiniMarketFull`
- `MostradorExpress`
- `CajaRapida`
- `TotemQrOnly`

Reglas:

- el modo se valida en `auth/device/validate`,
- frontend aplica guard por modulo para ocultar o bloquear rutas no habilitadas,
- QA por modo se valida con `scripts/smoke_pos_flow.ps1 -OperatingMode <Modo>`.

Notas de Totem:

- mantiene sesion de operador,
- no exige apertura/cierre de caja para venta,
- opera con carrito + QR y cuenta corriente,
- alta rapida de producto crea item en catalogo pendiente para completar en BO.
- durante cierre de manana, ventas quedan en `Transition` hasta apertura turno entrante.

### 6.1 Configuracion de turnos por local

En `store.SettingsJson.shiftSchedule` se define:

- `timezone` (default: `America/Argentina/Buenos_Aires`),
- `morningStart` (default: `07:30`),
- `morningCloseWindowStart` (default: `14:00`),
- `morningCloseWindowEnd` (default: `15:00`),
- `afternoonEnd` (default: `22:00`),
- `graceMinutes` (default: `90`).

Endpoints BO:

- `GET /api/v1/stores/{id}/shift-config`
- `PUT /api/v1/stores/{id}/shift-config`

## 7. Reglas de negocio criticas

### 7.1 Stock por buckets

Buckets oficiales:

- `VENDIBLE`
- `RECLAMO`
- `MERMA`

Toda operacion de stock impacta `ProductStock` y genera `StockMovement` trazable.

### 7.2 Reclamos a proveedor

Flujo recomendado:

1. Crear reclamo (`POST /api/v1/stock/claims`) con items y cantidades.
2. Retiro del reclamo (`POST /api/v1/stock/claims/{id}/pickup`).
3. Resolucion:
   - credito (`POST /api/v1/stock/claims/{id}/credit`), o
   - reposicion fisica (`POST /api/v1/stock/claims/{id}/replace`).

Efectos esperados:

- al crear reclamo, cantidad va a `RECLAMO`,
- al acreditar, sale de `RECLAMO` y se crea credito proveedor,
- al reponer fisicamente, sale de `RECLAMO` y entra a `VENDIBLE`.

Estados relevantes de reclamo:

- `Pending`
- `PickedUp`
- `Credited`
- `Replaced`

### 7.3 Creditos de proveedor

- se generan por reclamo acreditado,
- se aplican a compras (`POST /api/v1/stock/credits/apply`),
- no pueden aplicarse por monto mayor al remanente.

### 7.4 Transformaciones de stock

Objetivo: convertir producto origen en producto destino con factor de rendimiento.

Plantillas:

- listar: `GET /api/v1/stock/transformations/templates`
- alta/edicion: `POST /api/v1/stock/transformations/templates`

Aplicacion:

- `POST /api/v1/stock/transformations/apply`
- descuenta origen en `VENDIBLE`,
- acredita destino en `VENDIBLE`,
- usa `YieldFactor` de request o plantilla,
- registra movimiento tipo `Transformation`.

Persistencia:

- plantillas se guardan en `Settings` con key `StockTransformationTemplates`.

### 7.5 Fechas y UTC

Regla obligatoria para filtros de fecha (reportes/movimientos/dashboard):

- normalizar a `DateTimeKind.Utc` antes de consultar BD,
- evitar `DateTimeKind.Unspecified` por compatibilidad con PostgreSQL/Npgsql.

## 8. Flujos E2E principales

### 8.1 Caja diaria

1. validar dispositivo y sesion operador,
2. apertura de caja,
3. venta y cobro,
4. movimientos de caja si aplica,
5. cierre de caja con control de diferencias.

### 8.2 Compra sugerida BO

1. generar sugerencia,
2. revisar lineas, aceptar/ignorar/ajustar,
3. convertir a compra draft,
4. continuar confirmacion por flujo de compras.

Nota BO:

- para selector de proveedor en BO usar `GET /api/v1/purchase-suggestions/suppliers`.
- evitar depender de `/api/v1/suppliers` cuando ese endpoint requiera `DeviceAuth` en contexto no POS.

### 8.3 Gestion de reclamo proveedor

1. crear reclamo con items,
2. registrar retiro,
3. decidir credito o reposicion fisica,
4. aplicar credito en compra futura o cerrar evento por reposicion.

### 8.4 Transformacion operativa

1. elegir producto origen y destino,
2. cargar cantidad origen,
3. usar rendimiento plantilla o manual,
4. aplicar transformacion,
5. auditar movimientos generados.

### 8.5 Totem en cambio de turno

1. Venta Totem en franja de transicion queda con `ShiftAssignmentStatus = Transition`.
2. No impacta cierre de caja saliente.
3. Al abrir turno entrante (caja fisica), se autoasigna al turno entrante.
4. Si apertura ocurre fuera de ventana esperada, se marca `LateShiftOpen = true`.
5. Supervisor/Admin puede reasignar manualmente con motivo.

## 9. Principios de UX y contenido BO

- idioma visible: castellano argentino,
- mensajes accionables y sin ruido de errores falsos,
- consistencia visual entre modulos BO,
- enfoque por tarea diaria antes que por menu tecnico.

Lineas operativas recientes:

- evitar mostrar selector BO en login sin sesion valida,
- incluir logout BO claro,
- mantener navegacion visible y usable en dashboard.

## 10. Calidad y validacion

### 10.1 Smoke tecnico minimo

- `powershell -ExecutionPolicy Bypass -File .\scripts\smoke_full_flow.ps1`
- `powershell -ExecutionPolicy Bypass -File .\scripts\smoke_pos_flow.ps1 -OperatingMode MiniMarketFull`
- `powershell -ExecutionPolicy Bypass -File .\scripts\smoke_pos_flow.ps1 -OperatingMode MostradorExpress`
- `powershell -ExecutionPolicy Bypass -File .\scripts\smoke_pos_flow.ps1 -OperatingMode CajaRapida`

### 10.2 UAT funcional BO sugerido

- dashboard carga sin errores de fecha/UTC,
- stock muestra resumen y movimientos por sucursal activa,
- reclamo permite `pickup -> credit` y `pickup -> replace`,
- transformaciones crean dos movimientos (salida origen + entrada destino),
- compras sugeridas cargan proveedores con endpoint BO dedicado,
- textos principales sin ingles en flujos productivos.

## 11. Observabilidad y troubleshooting rapido

- revisar API health: `GET /api/v1/health`,
- revisar logs en `storage/logs/`,
- ante `401`: validar `X-Operator-Session` y vigencia,
- ante `403`: validar rol de usuario (`Admin/Supervisor/Manager`),
- ante inconsistencias de sucursal: validar `X-Store-Id` y store scoping.

## 12. Limites actuales y decisiones

- no incluye contabilidad/impositivo completo de forma nativa,
- no incluye nomina avanzada,
- integraciones ERP/ecommerce/fiscal avanzadas dependen de plan y alcance,
- soporte de hardware no homologado queda fuera salvo acuerdo.

## 13. Pendientes prioritarios recomendados

1. Cerrar UAT presencial por rubro con acta de conformidad.
2. Completar cobertura visual/funcional en vistas secundarias que sigan con placeholders.
3. Endurecer pruebas automatizadas BO para auth por rol y scope por store.
4. Mejorar metricas de negocio por modulo (adopcion, tiempos, errores por flujo).

## 14. Regla de mantenimiento de este archivo

Cada cambio funcional relevante debe actualizar `AGENT.md` en la misma iteracion.

Checklist de actualizacion:

- impacto en flujo de negocio,
- endpoint nuevo/modificado,
- regla de permisos,
- criterio de QA/UAT asociado.
