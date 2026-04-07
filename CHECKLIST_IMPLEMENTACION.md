# CHECKLIST IMPLEMENTACION

## 1) Relevamiento

- [ ] Datos de negocio: razon social, CUIT/RUC, domicilios, contactos.
- [ ] Definicion de sucursales y usuarios por rol.
- [ ] Medios de pago habilitados y flujo de caja.
- [ ] Politicas de devolucion, credito y envases.
- [ ] Reglas de stock/merma/reclamos a proveedor.

## 2) Configuracion

- [ ] Alta tenant/store y branding.
- [ ] Alta usuarios, permisos y dispositivos.
- [ ] Parametrizacion de caja (turnos, apertura/cierre).
- [ ] Configuracion de integraciones iniciales (si aplica).
- [ ] Configuracion de reportes/exportaciones requeridos.

## 3) Carga inicial

- [ ] Importacion catalogo de productos.
- [ ] Carga de listas de precios.
- [ ] Stock inicial por sucursal/bucket.
- [ ] Alta de clientes y saldos iniciales.
- [ ] Alta de proveedores y condiciones.

## 4) Pruebas

- [ ] Venta completa (efectivo/tarjeta/transferencia).
- [ ] Devolucion y comprobante.
- [ ] Movimiento de caja y cierre.
- [ ] Compra draft/confirmada y efecto en stock.
- [ ] Reclamo proveedor y credito.
- [ ] Exportaciones clave (ventas, stock, caja, RRHH).

## 5) Entrega

- [ ] Capacitacion por rol (caja/tablet/encargado/dueño).
- [ ] Validacion de criterios de aceptacion.
- [ ] Acta de puesta en produccion.
- [ ] Handover de soporte y canales.

## 6) QA por rubro (Etapa D)

### Minimarket (`MiniMarketFull`)

- [x] `device/validate` devuelve `operatingMode = MiniMarketFull`.
- [ ] Modulos visibles: tablet, envases, cuenta corriente, compras sugeridas, reportes.
- [ ] Flujo POS completo: apertura -> venta -> cobro -> cierre.

### Verduleria (`MostradorExpress`)

- [x] `device/validate` devuelve `operatingMode = MostradorExpress`.
- [ ] Modulos ocultos: tablet, envases, cuenta corriente.
- [ ] Flujo POS rapido con pesables y cobro en caja.

### Carniceria (`CajaRapida`)

- [x] `device/validate` devuelve `operatingMode = CajaRapida`.
- [ ] Modulos ocultos: tablet, envases, cuenta corriente, compras sugeridas.
- [ ] Flujo POS corto de caja sin modulos accesorios.

### Comandos de validacion

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke_pos_flow.ps1 -OperatingMode MiniMarketFull
powershell -ExecutionPolicy Bypass -File .\scripts\smoke_pos_flow.ps1 -OperatingMode MostradorExpress
powershell -ExecutionPolicy Bypass -File .\scripts\smoke_pos_flow.ps1 -OperatingMode CajaRapida
```

### Evidencia ejecutada (2026-03-13)

- [x] `scripts/smoke_full_flow.ps1`.
- [x] `scripts/smoke_pos_flow.ps1 -OperatingMode MiniMarketFull`.
- [x] `scripts/smoke_pos_flow.ps1 -OperatingMode MostradorExpress`.
- [x] `scripts/smoke_pos_flow.ps1 -OperatingMode CajaRapida`.
