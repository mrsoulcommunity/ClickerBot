@echo off
setlocal

rem ---------------------------------------------------------------------------
rem  Auto Key & Click - one-click launcher
rem
rem  Double-click this file to start the app. On the first run (or after a
rem  fresh clone) it builds the project once, then launches it. Every run
rem  after that starts the app straight away.
rem ---------------------------------------------------------------------------

cd /d "%~dp0"

set "PROJECT=ClickerApp\ClickerApp.csproj"
set "EXE=ClickerApp\bin\Release\net8.0-windows\ClickerApp.exe"

if exist "%EXE%" goto launch

title Building Auto Key ^& Click...

where dotnet >nul 2>&1
if errorlevel 1 goto no_dotnet

echo.
echo  First run - building the app, this only happens once.
echo.

dotnet build "%PROJECT%" -c Release --nologo
if errorlevel 1 goto build_failed

if not exist "%EXE%" goto build_failed

:launch
start "" "%EXE%"
exit /b 0

:no_dotnet
echo.
echo  The .NET 8 SDK was not found on this machine.
echo.
echo  Install it from https://dotnet.microsoft.com/download/dotnet/8.0
echo  then run this file again.
echo.
pause
exit /b 1

:build_failed
echo.
echo  The build failed. Scroll up to see what went wrong.
echo.
pause
exit /b 1
