# Checklist QA Totem + Caja

Duracion sugerida: 5 a 10 minutos por turno.

## Precondicion

- [ ] Operador logueado en Totem.
- [ ] Operador logueado en Caja.
- [ ] Estado del sistema en linea.
- [ ] Hay al menos un producto vendible en catalogo.

## Casos de validacion

### 1) Totem cobra QR sin derivar

- [ ] Crear carrito en Totem y agregar producto.
- [ ] Ir a cobro, cargar monto QR y confirmar.
- [ ] Resultado esperado: no aparece error de derivacion; la venta queda registrada.

### 2) Totem cobra cuenta corriente con cliente oficial

- [ ] Seleccionar cliente existente.
- [ ] Cargar monto a cuenta corriente y confirmar.
- [ ] Resultado esperado: venta registrada sin pedir "enviar a caja".

### 3) Totem cobra cuenta corriente con cliente ocasional

- [ ] No seleccionar cliente registrado.
- [ ] Abrir "Crear cliente ocasional".
- [ ] Completar nombre (telefono opcional).
- [ ] Cargar monto <= 25.000 y confirmar.
- [ ] Resultado esperado: cliente ocasional creado y venta registrada.

### 4) Tope de ocasional

- [ ] Intentar cobrar ocasional por monto > 25.000.
- [ ] Resultado esperado: bloqueo con mensaje de tope; no se crea la venta.

### 5) Derivar carrito a Caja desde Totem

- [ ] Crear carrito en Totem y agregar producto.
- [ ] Tocar "Derivar a caja".
- [ ] Resultado esperado: mensaje de exito y retorno automatico a "Nueva venta".

### 6) Caja cobra carrito derivado

- [ ] En Caja, abrir bandeja y localizar carrito derivado.
- [ ] Cobrar (ejemplo: efectivo) y confirmar.
- [ ] Resultado esperado: venta completada sin errores.

### 7) Evitar doble operacion del mismo carrito

- [ ] Intentar volver a operar en Totem un carrito ya derivado/cobrado.
- [ ] Resultado esperado: el sistema no permite continuar con ese carrito.

### 8) Pago posterior de cuenta corriente

- [ ] Ir a POS > Caja > Pago de cuenta.
- [ ] Buscar cliente oficial u ocasional creado en la prueba.
- [ ] Registrar un pago.
- [ ] Resultado esperado: deuda actualizada correctamente.

## Criterio de aprobacion

- [ ] Aprobado si todos los casos pasan sin errores bloqueantes.
- [ ] Si falla algun caso, registrar evidencia (modulo, paso, mensaje, hora, usuario).

## Planilla rapida de control

| Paso | OK/FAIL | Observacion |
| --- | --- | --- |
| Precondicion |  |  |
| Caso 1 - QR Totem |  |  |
| Caso 2 - Credito oficial Totem |  |  |
| Caso 3 - Credito ocasional Totem |  |  |
| Caso 4 - Tope ocasional |  |  |
| Caso 5 - Derivar a Caja |  |  |
| Caso 6 - Cobro en Caja |  |  |
| Caso 7 - Sin doble operacion |  |  |
| Caso 8 - Pago posterior |  |  |
