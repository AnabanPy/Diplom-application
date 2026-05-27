using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using WasteAccountingClient.Models;

namespace WasteAccountingClient.Services;

public static class FkkoLocalCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static List<WasteTypeDto>? TryLoad()
    {
        var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        foreach (var fileName in new[] { "fkko_operator.json.gz", "fkko_operator.json", "fkko_full.json" })
        {
            var path = Path.Combine(dataDir, fileName);
            if (!File.Exists(path))
                continue;

            var rows = ReadRows(path);
            if (rows is not { Count: > 0 })
                continue;

            return rows.Select((row, index) => new WasteTypeDto
            {
                Id = index + 1,
                Code = row.Code,
                Name = row.Name,
                FkkoCode = row.FkkoCode,
                HazardClass = row.HazardClass
            }).PrepareForRegistration();
        }

        return null;
    }

    private static List<FkkoImportRow>? ReadRows(string path)
    {
        try
        {
            if (path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            {
                using var file = File.OpenRead(path);
                using var gzip = new GZipStream(file, CompressionMode.Decompress);
                return JsonSerializer.Deserialize<List<FkkoImportRow>>(gzip, JsonOptions);
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<FkkoImportRow>>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private sealed class FkkoImportRow
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("fkko_code")]
        public string? FkkoCode { get; set; }

        [JsonPropertyName("hazard_class")]
        public int? HazardClass { get; set; }
    }
}
