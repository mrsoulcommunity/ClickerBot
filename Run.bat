@echo off
setlocal

rem ---------------------------------------------------------------------------
rem  ClickerBot - one-click launcher
rem
rem  Double-click this file to start the app. It builds first, every time: the
rem  build is incremental, so a run with nothing to compile costs a couple of
rem  seconds, and that is the price of never launching a stale binary after a
rem  pull. An earlier version skipped the build whenever the .exe already
rem  existed, which meant new code was fetched and the old .exe kept starting.
rem ---------------------------------------------------------------------------

cd /d "%~dp0"

set "PROJECT=ClickerBot\ClickerBot.csproj"
set "EXE=ClickerBot\bin\Release\net8.0-windows\ClickerBot.exe"

where dotnet >nul 2>&1
if errorlevel 1 goto no_dotnet

title Building ClickerBot...

echo.
echo  Building the latest version...
echo.

dotnet build "%PROJECT%" -c Release --nologo
if errorlevel 1 goto build_failed

if not exist "%EXE%" goto build_failed

start "" "%EXE%"
exit /b 0

:no_dotnet
rem  No SDK, so nothing can be compiled. An .exe left over from an earlier build
rem  is still better than refusing to start, as long as it is clear that is what
rem  is being launched.
if not exist "%EXE%" goto no_dotnet_no_exe

echo.
echo  The .NET 8 SDK was not found, so the app could not be rebuilt.
echo  Starting the previously built copy instead - it may be out of date.
echo.
echo  Install the SDK from https://dotnet.microsoft.com/download/dotnet/8.0
echo  to build the latest version.
echo.
pause
start "" "%EXE%"
exit /b 0

:no_dotnet_no_exe
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
