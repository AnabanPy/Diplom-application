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

public class UserProfileDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
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

    [JsonPropertyName("volume")]
    public double? Volume { get; set; }

    [JsonPropertyName("volume_unit")]
    public string? VolumeUnit { get; set; }

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

    [JsonPropertyName("processed_tons")]
    public double? ProcessedTons { get; set; }

    [JsonPropertyName("disposed_tons")]
    public double? DisposedTons { get; set; }

    [JsonPropertyName("remaining_tons")]
    public double? RemainingTons { get; set; }
}

public class BatchQueryParams
{
    public string? Status { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public string? SourceDepartment { get; set; }
}

public class BatchBalanceDto
{
    [JsonPropertyName("batch_id")]
    public int BatchId { get; set; }

    [JsonPropertyName("batch_code")]
    public string? BatchCode { get; set; }

    [JsonPropertyName("received_tons")]
    public double ReceivedTons { get; set; }

    [JsonPropertyName("processed_tons")]
    public double ProcessedTons { get; set; }

    [JsonPropertyName("disposed_tons")]
    public double DisposedTons { get; set; }

    [JsonPropertyName("remaining_tons")]
    public double RemainingTons { get; set; }
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

public class DepartmentDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }
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

    [JsonPropertyName("volume")]
    public double Volume { get; set; }

    [JsonPropertyName("volume_unit")]
    public string VolumeUnit { get; set; } = "t";

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

public class RejectBatchRequest
{
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "";
}

public class CreateOperationRequest
{
    [JsonPropertyName("batch_id")]
    public int BatchId { get; set; }

    [JsonPropertyName("operation_type")]
    public string OperationType { get; set; } = "processing";

    [JsonPropertyName("quantity_tons")]
    public double QuantityTons { get; set; }

    [JsonPropertyName("organization_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OrganizationId { get; set; }

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = "";
}

public class WasteOperationDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("batch_id")]
    public int? BatchId { get; set; }

    [JsonPropertyName("operation_type")]
    public string? OperationType { get; set; }

    [JsonPropertyName("quantity_tons")]
    public double QuantityTons { get; set; }

    [JsonPropertyName("operation_at")]
    public string? OperationAt { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }

    [JsonPropertyName("old_hazard_class")]
    public int? OldHazardClass { get; set; }

    [JsonPropertyName("new_hazard_class")]
    public int? NewHazardClass { get; set; }
}

public class ReportingDashboardDto
{
    [JsonPropertyName("organization_name")]
    public string? OrganizationName { get; set; }

    [JsonPropertyName("total_batches")]
    public int TotalBatches { get; set; }

    [JsonPropertyName("total_volume_tons")]
    public double TotalVolumeTons { get; set; }

    [JsonPropertyName("total_processed_tons")]
    public double TotalProcessedTons { get; set; }

    [JsonPropertyName("total_disposed_tons")]
    public double TotalDisposedTons { get; set; }

    [JsonPropertyName("total_remaining_tons")]
    public double TotalRemainingTons { get; set; }

    [JsonPropertyName("operations_count")]
    public int OperationsCount { get; set; }
}
