@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo Сборка релиза в dist\win-x64 ...
dotnet publish WasteAccountingClient\WasteAccountingClient.csproj -c Release -o "dist\win-x64" --self-contained false
if errorlevel 1 (
    echo Ошибка сборки.
    pause
    exit /b 1
)
echo.
echo Готово: dist\win-x64\WasteAccountingClient.exe
echo Загрузите изменения в GitHub после: git add dist && git commit && git push
pause
