@echo off
setlocal enabledelayedexpansion

if "%BACKUP_DIR%"=="" set BACKUP_DIR=backups
if "%PGHOST%"=="" set PGHOST=localhost
if "%PGPORT%"=="" set PGPORT=5432
if "%PGDATABASE%"=="" set PGDATABASE=KodvianSuperMarket
if "%PGUSER%"=="" set PGUSER=postgres

if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"

for /f %%i in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd_HHmmss"') do set STAMP=%%i
set FILE=%BACKUP_DIR%\%PGDATABASE%_%STAMP%.dump

pg_dump -h %PGHOST% -p %PGPORT% -U %PGUSER% -Fc %PGDATABASE% > "%FILE%"
if errorlevel 1 (
  echo Backup failed
  exit /b 1
)

forfiles /p "%BACKUP_DIR%" /m "%PGDATABASE%_*.dump" /d -14 /c "cmd /c del /q @path" >nul 2>&1

echo Backup created: %FILE%
endlocal
