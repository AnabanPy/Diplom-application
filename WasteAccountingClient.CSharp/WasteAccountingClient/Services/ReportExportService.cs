using System.IO;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WasteAccountingClient.Models;

namespace WasteAccountingClient.Services;

public static class ReportExportService
{
    private static string ReportsDirectory
    {
        get
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var dir = System.IO.Path.Combine(desktop, "Отчеты");
            System.IO.Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string ExportExcel(IReadOnlyList<BatchDto> batches)
    {
        var path = System.IO.Path.Combine(ReportsDirectory, $"report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Отчет по партиям");

        var headers = new[]
        {
            "Код партии", "Наименование отхода", "Класс опасности",
            "Поступило (т)", "Переработано (т)", "Вывезено (т)", "Остаток (т)",
            "Код ФККО", "Дата поступления", "Статус", "Цех-источник"
        };

        for (var col = 0; col < headers.Length; col++)
        {
            var cell = ws.Cell(1, col + 1);
            cell.Value = headers[col];
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
            ws.Cell(r, 3).Value = StatusTranslations.HazardToRoman(batch.HazardClass);
            ws.Cell(r, 4).Value = batch.VolumeTons ?? 0;
            ws.Cell(r, 5).Value = batch.ProcessedTons ?? 0;
            ws.Cell(r, 6).Value = batch.DisposedTons ?? 0;
            ws.Cell(r, 7).Value = batch.RemainingTons ?? 0;
            ws.Cell(r, 8).Value = batch.FkkoCode ?? "";
            ws.Cell(r, 9).Value = batch.ReceivedAt?.Length >= 10 ? batch.ReceivedAt[..10] : "";
            ws.Cell(r, 10).Value = StatusTranslations.ToRu(batch.Status);
            ws.Cell(r, 11).Value = batch.SourceDepartment ?? "";
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(path);
        return path;
    }

    public static string ExportWordAct(BatchDto batch)
    {
        var path = System.IO.Path.Combine(ReportsDirectory,
            $"act_{batch.Code}_{DateTime.Now:yyyyMMdd_HHmmss}.docx");

        using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
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
        return path;
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
}
