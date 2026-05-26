# Модуль учета отходов (C# / WPF)

Десктоп-приложение на C# — аналог Python-версии `WasteAccountingClient`.

## Требования

- Windows 10 / 11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) — для запуска `dist\...\exe`
- Подключение к интернету (API: `http://178.57.217.79:8080/api/v1`)
- SDK нужен только для разработки

## Запуск (в т.ч. после clone с GitHub)

1. Установите **Desktop Runtime** (ссылка выше).
2. Дважды щёлкните **`Run.bat`** в этой папке (или exe ниже).

`dist\win-x64\WasteAccountingClient.exe`

### Разработка

Пересобрать `dist` для GitHub: `Собрать-релиз.bat` или `dotnet publish`.

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
