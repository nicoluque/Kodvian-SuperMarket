# CRITERIOS DE ACEPTACION

Se considera implementado el sistema cuando se cumplen como minimo los siguientes puntos:

## Operacion

- [ ] Se puede abrir/cerrar caja y registrar movimientos.
- [ ] Se pueden realizar ventas y devoluciones con comprobantes.
- [ ] Se validan medios de pago configurados en el alcance.

## Maestros y datos

- [ ] Catalogo, precios y stock inicial cargados y validados.
- [ ] Clientes/proveedores base cargados.
- [ ] Usuarios y permisos activos por rol.

## Gestion

- [ ] Se visualiza dashboard gerencial con datos reales.
- [ ] Se pueden emitir exportaciones clave acordadas.
- [ ] Flujos de compra (draft/confirm) operativos.

## Calidad de salida

- [ ] Casos criticos del checklist de pruebas aprobados.
- [ ] Capacitacion minima por rol ejecutada.
- [ ] Acta de conformidad funcional firmada o aprobada por correo.

## Soporte inicial

- [ ] Ventana de estabilizacion activada.
- [ ] Canal de soporte informado a usuarios clave.

## Estado pre-entrega (2026-03-13)

- [x] Build frontend en verde (`npm run build`).
- [x] Smoke full operativo en verde (`scripts/smoke_full_flow.ps1`).
- [x] Smoke POS por modo en verde (`MiniMarketFull`, `MostradorExpress`, `CajaRapida`).
- [~] Criterios funcionales de negocio: parcial, con cobertura automatizada de flujo base y pendientes de validacion UAT presencial por rubro.
- [ ] Acta de conformidad funcional: pendiente de aprobacion de usuario final.
