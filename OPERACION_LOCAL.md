# Operacion Local - Kodvian SuperMarket

## 1) Red local con IP fija (recomendado)

Para que tablets/caja encuentren siempre el servidor:

1. Entrar al router (panel admin).
2. Buscar **DHCP Reservation** o **Static Lease**.
3. Reservar una IP para la PC/mini servidor por MAC address (ej: `192.168.1.50`).
4. Reiniciar interfaz de red o renovar DHCP.
5. Verificar IP con:
   - Windows: `ipconfig`
   - Linux: `ip addr`

Usar esa IP para configurar clientes POS.

## 2) Levantar stack local (Docker Compose)

Requisitos:
- Docker Desktop / Docker Engine + Compose.

Desde la raiz del repo:

```bash
docker compose up --build -d
```

Servicios:
- `db` (PostgreSQL)
- `api` (.NET 8)
- `frontend` (nginx + Angular)

Accesos:
- Frontend: `http://<IP_FIJA>/`
- API health: `http://<IP_FIJA>:8080/api/v1/health`

Estado de servicios:

```bash
docker compose ps
docker compose logs -f api
```

## 3) Variables de entorno importantes

En `docker-compose.yml`:
- `POSTGRES_DB`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `JWT_KEY`
- `App__StoragePath` (ej: `/storage`)
- `App__BackupPath` (ej: `/backups`)

## 4) Backup y Restore de base de datos

Scripts Linux/macOS en `scripts/`:
- `backup_db.sh`
- `restore_db.sh`

### Backup

```bash
chmod +x scripts/backup_db.sh scripts/restore_db.sh
PGPASSWORD=postgres BACKUP_DIR=./backups scripts/backup_db.sh
```

- Genera archivo `.dump`.
- Borra backups con mas de 14 dias.
- Actualiza `last_backup.txt` en el directorio de backups.

### Restore

```bash
PGPASSWORD=postgres scripts/restore_db.sh ./backups/KodvianSuperMarket_YYYYMMDD_HHMMSS.dump
```

## 5) Endpoint de estado del sistema

`GET /api/v1/admin/system-status` (solo Admin/Manager)

Devuelve:
- `dbOk`
- `apiVersion`
- `disk.freeBytes` y `disk.totalBytes`
- `lastBackupTimestamp`

## 6) Checklist operativo Apertura/Cierre

### Apertura
1. Verificar servicios `db/api/frontend` en `healthy`.
2. Verificar banner POS en **Online**.
3. Iniciar sesion de operador.
4. Abrir caja con efectivo inicial correcto.
5. Test rapido: escaneo + venta chica.

### Durante turno
1. Controlar `offline-queue` si hubo cortes de red.
2. Si vuelve online, ejecutar sync y validar tickets `synced`.
3. Revisar logs API en `storage/logs`.

### Cierre
1. Confirmar pendientes (transferencias/tareas obligatorias).
2. Ejecutar conteo requerido (cigarrillos si aplica).
3. Cerrar caja y validar diferencias.
4. Ejecutar backup DB y confirmar `last_backup.txt`.

## 7) Smoke test rapido POS/API

Para validar conectividad y flujo minimo (health + auth + apertura + inbox):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke_pos_flow.ps1
```

Smoke extendido (venta + print + export + intento de cierre):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke_full_flow.ps1
```

Parametros opcionales:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke_pos_flow.ps1 -BaseUrl http://localhost:5000 -DeviceToken demo-device-caja -Username demo.caja -Password demo123 -Pin 3333
```

Validacion por modo operativo:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke_pos_flow.ps1 -OperatingMode MiniMarketFull
powershell -ExecutionPolicy Bypass -File .\scripts\smoke_pos_flow.ps1 -OperatingMode MostradorExpress
powershell -ExecutionPolicy Bypass -File .\scripts\smoke_pos_flow.ps1 -OperatingMode CajaRapida
```
