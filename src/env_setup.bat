@echo off
setlocal enabledelayedexpansion

echo =====================================
echo   Configure MARIN_APP Environment
echo =====================================
echo.

:: Ask for Database Server
set /p MARIN_APP_DB_SERVER="Database server: "

:: Ask for Database Name (default marinapp)
set /p MARIN_APP_DB_NAME="Database name [marinapp]: "
if "!MARIN_APP_DB_NAME!"=="" set "MARIN_APP_DB_NAME=marinapp"

:: Ask for Username (default admin)
set /p MARIN_APP_DB_USER="User name [admin]: "
if "!MARIN_APP_DB_USER!"=="" set "MARIN_APP_DB_USER=admin"

:: Ask for Password
set /p MARIN_APP_DB_PASSWORD="Password: "

:: Ask for Port (default 5432)
set /p MARIN_APP_DB_PORT="Port [5432]: "
if "!MARIN_APP_DB_PORT!"=="" set "MARIN_APP_DB_PORT=5432"

:: Save variables to the current user environment
echo Saving environment variables for user...

:: Remove old values first (avoid duplicates)
reg delete "HKCU\Environment" /F /V MARIN_APP_DB_SERVER >nul 2>&1
reg delete "HKCU\Environment" /F /V MARIN_APP_DB_NAME >nul 2>&1
reg delete "HKCU\Environment" /F /V MARIN_APP_DB_USER >nul 2>&1
reg delete "HKCU\Environment" /F /V MARIN_APP_DB_PASSWORD >nul 2>&1
reg delete "HKCU\Environment" /F /V MARIN_APP_DB_PORT >nul 2>&1

:: Add new values
setx MARIN_APP_DB_SERVER "!MARIN_APP_DB_SERVER!"
setx MARIN_APP_DB_NAME "!MARIN_APP_DB_NAME!"
setx MARIN_APP_DB_USER "!MARIN_APP_DB_USER!"
setx MARIN_APP_DB_PASSWORD "!MARIN_APP_DB_PASSWORD!"
setx MARIN_APP_DB_PORT "!MARIN_APP_DB_PORT!"

echo.
echo =====================================
echo Environment variables have been saved:
echo   MARIN_APP_DB_SERVER=!MARIN_APP_DB_SERVER!
echo   MARIN_APP_DB_NAME=!MARIN_APP_DB_NAME!
echo   MARIN_APP_DB_USER=!MARIN_APP_DB_USER!
echo   MARIN_APP_DB_PASSWORD=********
echo   MARIN_APP_DB_PORT=!MARIN_APP_DB_PORT!
echo =====================================
echo.
echo ⚠️  You must log off and back on, or restart your terminal,
echo    for the new environment variables to take effect.
pause
