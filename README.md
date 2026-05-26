# Diplom-application — учёт отходов (C#)

## Быстрый запуск после скачивания с GitHub

1. Скачайте репозиторий (**Code → Download ZIP**) или клонируйте (`git clone`).
2. Установите **[.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)** (обязательно для `.exe` из папки `dist`).
3. Запустите один из файлов:
   - **`Run.bat`** в корне проекта (рекомендуется), или
   - **`WasteAccountingClient.CSharp\Run.bat`**, или
   - **`WasteAccountingClient.CSharp\dist\win-x64\WasteAccountingClient.exe`**

> Не используйте старый `Запуск.bat` из старых версий на GitHub — в нём была ошибка кодировки. После обновления репозитория работает `Run.bat`.

## Структура

| Путь | Назначение |
|------|------------|
| `WasteAccountingClient.CSharp\dist\win-x64\` | Готовая сборка для запуска |
| `WasteAccountingClient.CSharp\WasteAccountingClient\` | Исходный код |
| `WasteAccountingClient.CSharp\README.md` | Подробности, логины, API |

## API

`http://178.57.217.79:8080/api/v1`

## Обновить exe в репозитории

Запустите `WasteAccountingClient.CSharp\Собрать-релиз.bat`, затем commit и push в GitHub.
