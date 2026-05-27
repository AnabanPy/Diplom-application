@echo off
setlocal EnableExtensions

if "%~dp0"=="" goto :err_path
cd /d "%~dp0"
if errorlevel 1 goto :err_path

if not defined LOCALAPPDATA set "LOCALAPPDATA=%USERPROFILE%\AppData\Local"
if "%LOCALAPPDATA%"=="" set "LOCALAPPDATA=%USERPROFILE%\AppData\Local"
set "OUT=%LOCALAPPDATA%\WasteAccountingClient"
if "%OUT%"=="" (
    echo ERROR: output folder is empty. Check LOCALAPPDATA.
    pause
    exit /b 1
)

echo.
echo Release build
echo   output: "%OUT%"
echo   shortcut: Учет отходов.lnk on Desktop
echo.

py -3 "%~dp0scripts\generate_app_icon.py" 2>nul
if errorlevel 1 echo   icon step skipped (install Pillow if needed)

dotnet publish "%~dp0WasteAccountingClient\WasteAccountingClient.csproj" -c Release -o "%OUT%" --self-contained false
if errorlevel 1 goto :err_build

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\publish-shortcut.ps1" -PublishDir "%OUT%"
if errorlevel 1 goto :err_shortcut

echo DONE. Desktop shortcut created.
pause
exit /b 0

:err_path
echo ERROR: cannot access script directory.
pause
exit /b 1

:err_build
echo BUILD FAILED.
pause
exit /b 1

:err_shortcut
echo SHORTCUT FAILED.
pause
exit /b 1
