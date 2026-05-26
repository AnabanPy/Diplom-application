@echo off
cd /d "%~dp0"

if exist "dist\win-x64\WasteAccountingClient.exe" (
    start "" "dist\win-x64\WasteAccountingClient.exe"
    exit /b 0
)

where dotnet >nul 2>&1
if errorlevel 1 (
    echo.
    echo ERROR: .NET 8 not found.
    echo Install Desktop Runtime: https://dotnet.microsoft.com/download/dotnet/8.0
    echo Or run: dist\win-x64\WasteAccountingClient.exe
    echo.
    pause
    exit /b 1
)

dotnet run --project "WasteAccountingClient\WasteAccountingClient.csproj"
if errorlevel 1 (
    echo.
    echo Build/run failed. Install .NET 8 SDK or use dist\win-x64\WasteAccountingClient.exe
    echo.
    pause
    exit /b 1
)
