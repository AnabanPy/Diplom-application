using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using WasteAccountingClient.Models;
using WasteAccountingClient.Services;

namespace WasteAccountingClient.Api;

public class ApiClient
{
    public const string BaseUrl = "http://178.57.217.79:8080/api/v1";

    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl.TrimEnd('/') + "/")
        };
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public void SetToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization = null;
        else
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task EnsureSuccessOrThrow(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        var message = string.IsNullOrWhiteSpace(body)
            ? $"Ошибка сервера ({(int)response.StatusCode})"
            : ApiErrors.Format(body);
        throw new HttpRequestException(message, null, response.StatusCode);
    }

    public async Task<Dictionary<string, object?>> GetHealthAsync()
    {
        var response = await _http.GetAsync("core/health");
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<Dictionary<string, object?>>(JsonOptions)
               ?? new Dictionary<string, object?>();
    }

    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("auth/login", new { email, password });
            if (!response.IsSuccessStatusCode) return null;

            var data = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
            if (!string.IsNullOrEmpty(data?.AccessToken))
                SetToken(data.AccessToken);
            return data;
        }
        catch
        {
            return null;
        }
    }

    public async Task<UserProfileDto?> GetCurrentUserAsync()
    {
        var response = await _http.GetAsync("auth/user");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<UserProfileDto>(JsonOptions);
    }

    public async Task<List<WasteTypeDto>> GetWasteTypesAsync()
    {
        var response = await _http.GetAsync("core/waste-types");
        await EnsureSuccessOrThrow(response);
        var items = await response.Content.ReadFromJsonAsync<List<WasteTypeDto>>(JsonOptions) ?? [];
        return items.PrepareForDisplay();
    }

    public async Task<List<DepartmentDto>> GetDepartmentsAsync()
    {
        var response = await _http.GetAsync("core/departments");
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<List<DepartmentDto>>(JsonOptions) ?? [];
    }

    public async Task<List<BatchDto>> GetBatchesAsync(BatchQueryParams? query = null)
    {
        var url = BuildQuery("accounting/batches", query);
        var response = await _http.GetAsync(url);
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<List<BatchDto>>(JsonOptions) ?? [];
    }

    public async Task<BatchDto> CreateBatchAsync(CreateBatchRequest data)
    {
        var response = await _http.PostAsJsonAsync("accounting/batches", data);
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<BatchDto>(JsonOptions)
               ?? throw new InvalidOperationException("Пустой ответ сервера");
    }

    public async Task<BatchDto> ClassifyBatchAsync(int batchId, ClassifyBatchRequest data)
    {
        var response = await _http.PatchAsJsonAsync($"accounting/batches/{batchId}/classify", data);
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<BatchDto>(JsonOptions)
               ?? throw new InvalidOperationException("Пустой ответ сервера");
    }

    public async Task<BatchDto> RejectBatchAsync(int batchId, RejectBatchRequest data)
    {
        var response = await _http.PatchAsJsonAsync($"accounting/batches/{batchId}/reject", data);
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<BatchDto>(JsonOptions)
               ?? throw new InvalidOperationException("Пустой ответ сервера");
    }

    public async Task<List<WasteOperationDto>> GetClassificationHistoryAsync(int batchId)
    {
        var response = await _http.GetAsync($"accounting/batches/{batchId}/classification-history");
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<List<WasteOperationDto>>(JsonOptions) ?? [];
    }

    public async Task<BatchBalanceDto> GetBatchBalanceAsync(int batchId)
    {
        var response = await _http.GetAsync($"accounting/batches/{batchId}/balance");
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<BatchBalanceDto>(JsonOptions)
               ?? throw new InvalidOperationException("Пустой ответ сервера");
    }

    public async Task<WasteOperationDto> CreateOperationAsync(CreateOperationRequest data)
    {
        var response = await _http.PostAsJsonAsync("accounting/operations", data);
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<WasteOperationDto>(JsonOptions)
               ?? throw new InvalidOperationException("Пустой ответ сервера");
    }

    public async Task<List<WasteOperationDto>> GetOperationsAsync(int? batchId = null, string? operationType = null)
    {
        var parts = new List<string>();
        if (batchId.HasValue) parts.Add($"batch_id={batchId.Value}");
        if (!string.IsNullOrEmpty(operationType)) parts.Add($"operation_type={Uri.EscapeDataString(operationType)}");
        var query = parts.Count > 0 ? "?" + string.Join("&", parts) : "";
        var response = await _http.GetAsync($"accounting/operations{query}");
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<List<WasteOperationDto>>(JsonOptions) ?? [];
    }

    public async Task<ReportingDashboardDto> GetDashboardAsync()
    {
        var response = await _http.GetAsync("reporting/dashboard");
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<ReportingDashboardDto>(JsonOptions)
               ?? throw new InvalidOperationException("Пустой ответ сервера");
    }

    private static string BuildQuery(string path, BatchQueryParams? query)
    {
        if (query == null) return path;
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(query.Status))
            parts.Add($"status={Uri.EscapeDataString(query.Status)}");
        if (!string.IsNullOrEmpty(query.DateFrom))
            parts.Add($"date_from={Uri.EscapeDataString(query.DateFrom)}");
        if (!string.IsNullOrEmpty(query.DateTo))
            parts.Add($"date_to={Uri.EscapeDataString(query.DateTo)}");
        if (!string.IsNullOrEmpty(query.SourceDepartment))
            parts.Add($"source_department={Uri.EscapeDataString(query.SourceDepartment)}");
        return parts.Count == 0 ? path : $"{path}?{string.Join("&", parts)}";
    }
}
