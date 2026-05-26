@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo Запуск модуля учёта отходов...
dotnet run --project WasteAccountingClient\WasteAccountingClient.csproj
if errorlevel 1 (
    echo.
    echo Ошибка запуска. Установите .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
)
