# Модуль учета отходов (C# / WPF)

Десктоп-приложение на C# — аналог Python-версии `WasteAccountingClient`.

## Требования

- Windows 10 / 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Подключение к интернету (API: `http://178.57.217.79:8080/api/v1`)

## Запуск

### Без установки SDK (готовый .exe из репозитория)

1. Установите [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (если ещё не установлен).
2. Запустите:

`WasteAccountingClient.CSharp\dist\win-x64\WasteAccountingClient.exe`

### Разработка

**Проще всего:** дважды щёлкните `Запуск.bat` в папке `WasteAccountingClient.CSharp`.

Пересобрать папку `dist` для GitHub: `Собрать-релиз.bat`.

Или из командной строки:

```bash
cd WasteAccountingClient.CSharp
dotnet run --project WasteAccountingClient
```

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
