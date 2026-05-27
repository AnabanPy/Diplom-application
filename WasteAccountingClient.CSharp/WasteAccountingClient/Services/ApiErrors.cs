using System.Text.Json;

namespace WasteAccountingClient.Services;

public static class ApiErrors
{
    public static string Format(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Неизвестная ошибка сервера";
        var text = raw.Trim();
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.TryGetProperty("detail", out var detail))
            {
                return detail.ValueKind switch
                {
                    JsonValueKind.String => detail.GetString() ?? text,
                    _ => detail.ToString()
                };
            }
        }
        catch
        {
            // not JSON
        }

        return text.Length > 300 ? text[..300] + "…" : text;
    }
}
