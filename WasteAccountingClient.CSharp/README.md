# WasteAccountingClient.CSharp

Исходный код и сборка клиента «Учёт промышленных отходов».

**Документация проекта:** [README в корне репозитория](../README.md)

## Интерфейс

- Таблицы (**История**, **Отчёт**, **Контроль**, **ФККО**, **Операции**) заполняют ширину области: у колонок с длинным текстом заданы **веса `*` (пропорции)**, чтобы свободное место распределялось между «Наименованием», «Цехом», «Кодом ФККО», «Примечанием» и т.д., а не уходило в одну колонку.
- Релиз и первый вход: ярлык на рабочем столе **`Учет отходов.lnk`** (см. `Собрать-релиз.bat`, `Services/DesktopShortcutService.cs`).

## Быстрые команды

```bat
dotnet run --project WasteAccountingClient\WasteAccountingClient.csproj   :: запуск из исходников
Собрать-релиз.bat    :: %LocalAppData%\WasteAccountingClient + ярлык «Учет отходов.lnk» на столе
py -3 scripts\generate_app_icon.py   :: пересобрать app.ico из Resources\AppIconSource.png (нужен Pillow)
```
