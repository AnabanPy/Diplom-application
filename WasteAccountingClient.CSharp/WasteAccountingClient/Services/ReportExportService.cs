using System.IO;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WasteAccountingClient.Models;

namespace WasteAccountingClient.Services;

/// <summary>Заголовки Excel совпадают с колонками вкладки «Отчёты».</summary>
public static class ReportExportService
{
    public static readonly string[] ExcelHeaders =
    [
        "Код", "Наименование", "Код ФККО", "Кл.", "Поступило", "Перераб.", "Вывезено", "Остаток", "Дата", "Статус", "Цех"
    ];

    public static void ExportExcel(IReadOnlyList<BatchDto> batches, string filePath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Отчет по партиям");

        for (var col = 0; col < ExcelHeaders.Length; col++)
        {
            var cell = ws.Cell(1, col + 1);
            cell.Value = ExcelHeaders[col];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0066cc");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        for (var row = 0; row < batches.Count; row++)
        {
            var batch = batches[row];
            var r = row + 2;
            ws.Cell(r, 1).Value = batch.Code ?? "";
            ws.Cell(r, 2).Value = batch.Name ?? "";
            ws.Cell(r, 3).Value = batch.FkkoCode ?? "";
            ws.Cell(r, 4).Value = StatusTranslations.HazardToRoman(batch.HazardClass);
            ws.Cell(r, 5).Value = batch.VolumeTons ?? 0;
            ws.Cell(r, 6).Value = batch.ProcessedTons ?? 0;
            ws.Cell(r, 7).Value = batch.DisposedTons ?? 0;
            ws.Cell(r, 8).Value = batch.RemainingTons ?? 0;
            ws.Cell(r, 9).Value = batch.ReceivedAt?.Length >= 10 ? batch.ReceivedAt[..10] : "";
            ws.Cell(r, 10).Value = StatusTranslations.ToRu(batch.Status);
            ws.Cell(r, 11).Value = batch.SourceDepartment ?? "";
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
    }

    public static void ExportWordAct(BatchDto batch, string filePath)
    {
        using var doc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        var body = mainPart.Document.Body!;

        AddParagraph(body, $"АКТ ПРИЕМА-ПЕРЕДАЧИ ОТХОДОВ №{batch.Code}", bold: true, center: true);
        AddParagraph(body, $"Дата составления: {DateTime.Now:dd.MM.yyyy}");
        AddParagraph(body, "");
        AddParagraph(body, "1. Сведения о партии отходов", bold: true);
        AddParagraph(body, $"Код партии: {batch.Code}");
        AddParagraph(body, $"Наименование отхода: {batch.Name}");
        AddParagraph(body, $"Код ФККО: {batch.FkkoCode ?? "—"}");
        AddParagraph(body, $"Класс опасности: {StatusTranslations.HazardToLongText(batch.HazardClass)}");
        AddParagraph(body, $"Объем: {batch.VolumeTons ?? 0} тонн");
        AddParagraph(body,
            $"Дата поступления: {(batch.ReceivedAt?.Length >= 10 ? batch.ReceivedAt[..10] : "—")}");
        AddParagraph(body, $"Цех-источник: {batch.SourceDepartment ?? "—"}");
        AddParagraph(body, $"Статус: {StatusTranslations.ToRu(batch.Status)}");
        AddParagraph(body, "");
        AddParagraph(body, "От оператора склада: ___________________");
        AddParagraph(body, "От цеха-источника: ___________________");

        mainPart.Document.Save();
    }

    private static void AddParagraph(Body body, string text, bool bold = false, bool center = false)
    {
        var run = new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        if (bold)
            run.RunProperties = new RunProperties(new Bold());

        var props = new ParagraphProperties();
        if (center)
            props.Justification = new Justification { Val = JustificationValues.Center };

        body.Append(new Paragraph(props, run));
    }

    /// <summary>Убирает недопустимые для имени файла символы.</summary>
    public static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        var s = new string(chars).Trim();
        return string.IsNullOrEmpty(s) ? "batch" : s;
    }
}
