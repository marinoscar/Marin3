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

:: Save variables to current session
setx MARIN_APP_DB_SERVER "!MARIN_APP_DB_SERVER!"
setx MARIN_APP_DB_NAME "!MARIN_APP_DB_NAME!"
setx MARIN_APP_DB_USER "!MARIN_APP_DB_USER!"
setx MARIN_APP_DB_PASSWORD "!MARIN_APP_DB_PASSWORD!"
setx MARIN_APP_DB_PORT "!MARIN_APP_DB_PORT!"

echo.
echo =====================================
echo Environment variables have been set:
echo   MARIN_APP_DB_SERVER=!MARIN_APP_DB_SERVER!
echo   MARIN_APP_DB_NAME=!MARIN_APP_DB_NAME!
echo   MARIN_APP_DB_USER=!MARIN_APP_DB_USER!
echo   MARIN_APP_DB_PASSWORD=********
echo   MARIN_APP_DB_PORT=!MARIN_APP_DB_PORT!
echo =====================================

pause
