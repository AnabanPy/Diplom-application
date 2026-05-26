namespace WasteAccountingClient.Services;

public static class StatusTranslations
{
    public static readonly Dictionary<string, string> ToRussian = new(StringComparer.OrdinalIgnoreCase)
    {
        ["accepted"] = "Принят",
        ["classified"] = "Классифицирован",
        ["processing"] = "В обработке",
        ["completed"] = "Завершен",
        ["registered"] = "Зарегистрирован",
        ["verified"] = "Проверен",
        ["rejected"] = "Отклонен"
    };

    public static string ToRu(string? status)
    {
        if (string.IsNullOrEmpty(status)) return "—";
        return ToRussian.TryGetValue(status, out var ru) ? ru : status;
    }

    public static string? ToEn(string russianStatus)
    {
        if (russianStatus == "Все") return null;
        foreach (var pair in ToRussian)
        {
            if (pair.Value == russianStatus) return pair.Key;
        }
        return null;
    }

    public static string HazardToRoman(int? hazard)
    {
        return hazard switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            _ => hazard?.ToString() ?? ""
        };
    }

    public static string HazardToLongText(int? hazard)
    {
        return hazard switch
        {
            1 => "I (чрезвычайно опасный)",
            2 => "II (высокоопасный)",
            3 => "III (умеренно опасный)",
            4 => "IV (малоопасный)",
            5 => "V (практически неопасный)",
            _ => hazard?.ToString() ?? ""
        };
    }

    public static string RoleDisplayName(string role) => role switch
    {
        "operator" => "Оператор склада",
        "chief" => "Начальник склада",
        "ecologist" => "Эколог",
        "admin" => "Администратор",
        _ => role
    };
}
