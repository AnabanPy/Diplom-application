using WasteAccountingClient.Models;

namespace WasteAccountingClient.Services;

public static class FkkoSort
{
    private static readonly HashSet<string> LegacyDemoCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "WT-SLUDGE", "WT-OIL", "WT-CHEM"
    };

    public static string ToSortKey(string? fkkoCode) =>
        string.Concat((fkkoCode ?? "").Where(char.IsDigit));

    public static List<WasteTypeDto> PrepareForDisplay(this IEnumerable<WasteTypeDto> items)
    {
        var list = items.ToList();
        if (list.Count > 100)
            list = list.Where(w => w.Code is null || !LegacyDemoCodes.Contains(w.Code)).ToList();

        return list.OrderByFkkoCode();
    }

    public static bool IsRegistrableWaste(WasteTypeDto w) =>
        w.HazardClass is > 0 and <= 5;

    /// <summary>Для регистрации партии — только конкретные виды (класс I–V), без групп каталога.</summary>
    public static List<WasteTypeDto> PrepareForRegistration(this IEnumerable<WasteTypeDto> items)
    {
        var list = items.Where(IsRegistrableWaste).ToList();
        if (list.Count > 100)
            list = list.Where(w => w.Code is null || !LegacyDemoCodes.Contains(w.Code)).ToList();
        return list.OrderByFkkoCode();
    }

    public static List<WasteTypeDto> OrderByFkkoCode(this IEnumerable<WasteTypeDto> items) =>
        items.OrderBy(w => ToSortKey(w.FkkoCode), StringComparer.Ordinal)
            .ThenBy(w => w.Name ?? "", StringComparer.OrdinalIgnoreCase)
            .ToList();
}
