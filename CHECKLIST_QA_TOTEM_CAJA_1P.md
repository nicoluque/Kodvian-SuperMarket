# Checklist QA Totem + Caja (1 pagina)

Uso: control rapido por turno (5-10 min).
Fecha: ____/____/______  Turno: __________  Operador: __________

## Pre-check

- [ ] Totem logueado
- [ ] Caja logueada
- [ ] Sistema en linea
- [ ] Producto vendible disponible

## Validacion rapida

1) Totem - QR
- [ ] Crea carrito, cobra QR, confirma
- [ ] OK esperado: sin error de derivacion

2) Totem - Cuenta corriente cliente oficial
- [ ] Selecciona cliente existente y cobra
- [ ] OK esperado: venta registrada

3) Totem - Cuenta corriente cliente ocasional
- [ ] Sin cliente seleccionado, crea ocasional (nombre obligatorio)
- [ ] Cobra monto <= 25.000
- [ ] OK esperado: crea cliente + registra venta

4) Tope ocasional
- [ ] Intenta cobrar > 25.000
- [ ] OK esperado: bloquea operacion

5) Derivar a caja
- [ ] Deriva carrito desde Totem
- [ ] OK esperado: mensaje exito + vuelve a Nueva venta

6) Caja - Cobro de carrito derivado
- [ ] Abre bandeja y cobra carrito derivado
- [ ] OK esperado: venta completada

7) No doble operacion
- [ ] Reintenta operar carrito ya derivado/cobrado
- [ ] OK esperado: sistema no permite continuar

8) Pago posterior cuenta corriente
- [ ] Ir a Pago de cuenta y registrar pago
- [ ] OK esperado: deuda actualizada

## Resultado final

- [ ] APROBADO
- [ ] OBSERVADO
- [ ] RECHAZADO

Observaciones:

________________________________________________________________________
________________________________________________________________________
________________________________________________________________________
