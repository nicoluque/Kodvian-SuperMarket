@echo off
setlocal

if "%~1"=="" (
  echo Usage: restore_db.bat ^<backup_file.dump^>
  exit /b 1
)

set BACKUP_FILE=%~1
if "%PGHOST%"=="" set PGHOST=localhost
if "%PGPORT%"=="" set PGPORT=5432
if "%PGDATABASE%"=="" set PGDATABASE=KodvianSuperMarket
if "%PGUSER%"=="" set PGUSER=postgres

if not exist "%BACKUP_FILE%" (
  echo Backup file not found: %BACKUP_FILE%
  exit /b 1
)

pg_restore -h %PGHOST% -p %PGPORT% -U %PGUSER% -d %PGDATABASE% --clean --if-exists "%BACKUP_FILE%"
if errorlevel 1 (
  echo Restore failed
  exit /b 1
)

echo Restore completed from: %BACKUP_FILE%
endlocal
