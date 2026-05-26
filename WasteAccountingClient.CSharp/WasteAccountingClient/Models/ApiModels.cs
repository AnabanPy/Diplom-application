using System.Text.Json.Serialization;

namespace WasteAccountingClient.Models;

public class LoginResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }
}

public class UserInfo
{
    public string Login { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "operator";
}

public class BatchDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("fkko_code")]
    public string? FkkoCode { get; set; }

    [JsonPropertyName("hazard_class")]
    public int? HazardClass { get; set; }

    [JsonPropertyName("volume_tons")]
    public double? VolumeTons { get; set; }

    [JsonPropertyName("received_at")]
    public string? ReceivedAt { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("source_department")]
    public string? SourceDepartment { get; set; }

    [JsonPropertyName("storage_deadline_hours")]
    public double? StorageDeadlineHours { get; set; }
}

public class WasteTypeDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("fkko_code")]
    public string? FkkoCode { get; set; }

    [JsonPropertyName("hazard_class")]
    public int? HazardClass { get; set; }
}

public class CreateBatchRequest
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("fkko_code")]
    public string FkkoCode { get; set; } = "";

    [JsonPropertyName("hazard_class")]
    public int HazardClass { get; set; }

    [JsonPropertyName("volume_tons")]
    public double VolumeTons { get; set; }

    [JsonPropertyName("storage_deadline_hours")]
    public double StorageDeadlineHours { get; set; }

    [JsonPropertyName("source_department")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SourceDepartment { get; set; }
}

public class ClassifyBatchRequest
{
    [JsonPropertyName("hazard_class")]
    public int HazardClass { get; set; }

    [JsonPropertyName("classification_note")]
    public string ClassificationNote { get; set; } = "";
}
