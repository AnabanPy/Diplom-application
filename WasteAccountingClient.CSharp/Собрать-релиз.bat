@echo off
cd /d "%~dp0"

echo Building release to dist\win-x64 ...
dotnet publish "WasteAccountingClient\WasteAccountingClient.csproj" -c Release -o "dist\win-x64" --self-contained false
if errorlevel 1 (
    echo Build failed.
    pause
    exit /b 1
)

echo.
echo Done: dist\win-x64\WasteAccountingClient.exe
pause
