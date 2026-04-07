# GUION DEMO (10-15 MIN)

## Objetivo

Mostrar en tiempo corto el valor operativo y gerencial del sistema para cerrar decision comercial.

## Estructura sugerida

### 1) Apertura (1-2 min)

- Presentar contexto del negocio (minimarket).
- Mostrar login y branding por tenant.
- Explicar enfoque: "operacion + control + escalabilidad".

### 2) Operacion POS (4-5 min)

- Cargar carrito desde tablet/caja.
- Convertir a venta y cobrar con medio mixto.
- Mostrar comprobante termico.
- Ejecutar una devolucion y su comprobante.
- Registrar movimiento de caja.

### 3) Gestion comercial (3-4 min)

- Dashboard gerencial (ventas/pendientes).
- Cuenta corriente cliente + movimiento.
- Reclamo a proveedor + credito.
- Compras sugeridas: generar, aceptar lineas y convertir a compra draft.

### 4) Control y analitica (2-3 min)

- Exportaciones gerenciales (Excel/PDF).
- Estado de stock critico.
- RRHH basico (fichadas/horas).

### 5) Cierre comercial (1 min)

- Resumen de beneficios:
  - menos errores de operacion,
  - mejor control diario,
  - base para crecer a multi-sucursal.
- Proxima accion: piloto, onboarding e implementacion.

## Guion por rubro (Etapa D)

### Minimarket (modo `MiniMarketFull`)

- Apertura en `POS / Caja / Apertura` y venta mixta desde `POS / Caja / Inbox`.
- Mostrar uso de `Cuenta corriente`, `Envases` y `Tablet` habilitados.
- En BO: `Dashboard`, `Compras sugeridas`, `Exportaciones`.
- Cierre: validar que el local opera con todos los modulos activos.

### Verduleria (modo `MostradorExpress`)

- Apertura rapida y venta con pesables (escaneo + modal de kg/$kg).
- Mostrar foco operativo: cobro en caja y devolucion simple.
- Resaltar que `Tablet`, `Envases` y `Cuenta corriente` quedan ocultos por modo.
- En BO: `Lo diario`, `Compras sugeridas`, `Reportes` minimos.

### Carniceria (modo `CajaRapida`)

- Apertura de caja + flujo corto de venta/cobro.
- Mostrar velocidad de caja: menos opciones visibles, menos errores operativos.
- Resaltar modo de bajo riesgo: sin modulos accesorios en POS.
- En BO: `Lo diario` y `Reportes` para control de turno y cierre.

## Checklist previo a demo por rubro

- Ejecutar reset demo: `POST /api/v1/admin/demo/reset`.
- Configurar modo en `BO / Admin / Configuracion local`.
- Validar modo con `GET /api/v1/auth/device/validate`.
- Correr smoke por modo: `powershell -ExecutionPolicy Bypass -File .\scripts\smoke_pos_flow.ps1 -OperatingMode <Modo>`.
- Confirmar textos visibles en castellano en flujo principal (POS + BO).
