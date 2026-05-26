# Модуль учета отходов (C# / WPF)

Десктоп-приложение на C# — аналог Python-версии `WasteAccountingClient`.

## Требования

- Windows 10 / 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Подключение к интернету (API: `http://178.57.217.79:8080/api/v1`)

## Запуск

**Проще всего:** дважды щёлкните файл `Запуск.bat` в папке `WasteAccountingClient.CSharp`.

Или из командной строки:

```bash
cd WasteAccountingClient.CSharp
dotnet run --project WasteAccountingClient
```

Или запустите `WasteAccountingClient\bin\Debug\net8.0-windows\WasteAccountingClient.exe` (после сборки).

Из Visual Studio: откройте `WasteAccountingClient.sln` и нажмите F5.

## Вход в систему

| Роль | Email | Пароль |
|------|-------|--------|
| Оператор склада | `operator@example.com` | `123546` |
| Начальник склада | `chief@example.com` | `123546` |
| Эколог | `ecologist@example.com` | `123456` |
| Администратор | `admin@example.com` | `admin123` |

На экране входа можно нажать карточку «Оператор» или «Начальник» для быстрого входа (эколог и администратор — ввод вручную).

## Функции

- Авторизация и проверка сервера при старте
- История поступлений
- Отчеты с фильтрами, экспорт в Excel и Word (папка `Отчеты` на рабочем столе)
- Регистрация партии (роль `operator`)
- Контроль классификации и справочник ФККО (`chief`, `ecologist`, `admin`)

## Стек

- WPF (.NET 8)
- `HttpClient` + System.Text.Json
- ClosedXML (Excel)
- DocumentFormat.OpenXml (Word)
