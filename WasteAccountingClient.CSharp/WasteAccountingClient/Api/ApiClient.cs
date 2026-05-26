using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using WasteAccountingClient.Models;

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

    public async Task<Dictionary<string, object?>> GetHealthAsync()
    {
        var response = await _http.GetAsync("core/health");
        response.EnsureSuccessStatusCode();
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

    public async Task<List<WasteTypeDto>> GetWasteTypesAsync()
    {
        var response = await _http.GetAsync("core/waste-types");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<WasteTypeDto>>(JsonOptions) ?? [];
    }

    public async Task<List<BatchDto>> GetBatchesAsync()
    {
        var response = await _http.GetAsync("accounting/batches");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<BatchDto>>(JsonOptions) ?? [];
    }

    public async Task<BatchDto> CreateBatchAsync(CreateBatchRequest data)
    {
        var response = await _http.PostAsJsonAsync("accounting/batches", data);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BatchDto>(JsonOptions)
               ?? throw new InvalidOperationException("Пустой ответ сервера");
    }

    public async Task<BatchDto> ClassifyBatchAsync(int batchId, ClassifyBatchRequest data)
    {
        var response = await _http.PatchAsJsonAsync($"accounting/batches/{batchId}/classify", data);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BatchDto>(JsonOptions)
               ?? throw new InvalidOperationException("Пустой ответ сервера");
    }
}
