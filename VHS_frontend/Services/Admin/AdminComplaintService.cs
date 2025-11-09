using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VHS_frontend.Areas.Admin.Models.Complaint;

namespace VHS_frontend.Services.Admin
{
    public class AdminComplaintService
    {
        private readonly HttpClient _http;
        private string? _bearer;
        
        public AdminComplaintService(HttpClient http) => _http = http;
        public void SetBearerToken(string token) => _bearer = token;

        private void AttachAuth()
        {
            if (!string.IsNullOrWhiteSpace(_bearer))
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _bearer);
        }

        private static async Task HandleErrorAsync(HttpResponseMessage res, CancellationToken ct)
        {
            string msg = "Đã có lỗi xảy ra.";
            try
            {
                using var s = await res.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct);
                if (doc.RootElement.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                    msg = m.GetString() ?? msg;
            }
            catch { /* ignore parse error */ }

            res.EnsureSuccessStatusCode();
        }

        public async Task<PaginatedAdminComplaintDTO?> GetAllAsync(AdminComplaintFilterDTO filter, CancellationToken ct = default)
        {
            Console.WriteLine($"[AdminComplaintService] GetAllAsync called with filter: Page={filter?.Page}, PageSize={filter?.PageSize}, Status={filter?.Status}");
            
            AttachAuth();
            
            // Try OData first, fallback to regular query params if it fails
            var queryParams = new List<string>();
            
            // Build $filter clause
            var filterParts = new List<string>();
            if (!string.IsNullOrEmpty(filter.Status))
                filterParts.Add($"Status eq '{filter.Status}'");
            if (!string.IsNullOrEmpty(filter.Type))
                filterParts.Add($"ComplaintType eq '{filter.Type}'");
            if (!string.IsNullOrEmpty(filter.Search))
            {
                // Search in Title or Description
                filterParts.Add($"(contains(Title, '{filter.Search}') or contains(Description, '{filter.Search}'))");
            }
            
            if (filterParts.Any())
            {
                queryParams.Add($"$filter={Uri.EscapeDataString(string.Join(" and ", filterParts))}");
            }
            
            // Build $orderby clause - use proper OData syntax
            var orderBy = filter.SortBy ?? "CreatedAt";
            var orderDirection = filter.SortOrder?.ToLower() == "asc" ? "asc" : "desc";
            // OData orderby format: $orderby=PropertyName direction (no space, use comma for multiple)
            queryParams.Add($"$orderby={Uri.EscapeDataString($"{orderBy} {orderDirection}")}");
            
            // Build $skip and $top for pagination
            var skip = (filter.Page - 1) * filter.PageSize;
            queryParams.Add($"$skip={skip}");
            queryParams.Add($"$top={filter.PageSize}");
            
            // Add $count to get total count
            queryParams.Add("$count=true");
            
            var url = $"/api/admin/admincomplaint?{string.Join("&", queryParams)}";
            
            Console.WriteLine($"[AdminComplaintService] Making request to: {url}");
            
            string json = string.Empty;
            try
            {
                var res = await _http.GetAsync(url, ct);
                Console.WriteLine($"[AdminComplaintService] Response status: {res.StatusCode}");
                
                if (!res.IsSuccessStatusCode)
                {
                    var errorContent = await res.Content.ReadAsStringAsync(ct);
                    Console.WriteLine($"[AdminComplaintService] Error response: {errorContent}");
                    throw new HttpRequestException($"Request failed with status {res.StatusCode}: {errorContent}");
                }
                
                await HandleErrorAsync(res, ct);

                json = await res.Content.ReadAsStringAsync(ct);
            
                // Debug logging
                Console.WriteLine($"[AdminComplaintService] Response length: {json.Length}");
                if (json.Length < 500)
                {
                    Console.WriteLine($"[AdminComplaintService] Response: {json}");
                }
                
                var response = JsonSerializer.Deserialize<JsonElement>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                // Parse OData response (format: { value: [...], @odata.count: ... })
                if (response.ValueKind == JsonValueKind.Object && response.TryGetProperty("value", out var valueArray))
                {
                    Console.WriteLine($"[AdminComplaintService] Found 'value' property, parsing...");
                    var complaints = JsonSerializer.Deserialize<List<AdminComplaintDTO>>(valueArray.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<AdminComplaintDTO>();
                    
                    Console.WriteLine($"[AdminComplaintService] Parsed {complaints.Count} complaints");
                    
                    var totalCount = 0;
                    if (response.TryGetProperty("@odata.count", out var countElement) && countElement.ValueKind == JsonValueKind.Number)
                    {
                        totalCount = countElement.GetInt32();
                        Console.WriteLine($"[AdminComplaintService] Found @odata.count: {totalCount}");
                    }
                    else if (response.TryGetProperty("count", out var countElement2) && countElement2.ValueKind == JsonValueKind.Number)
                    {
                        totalCount = countElement2.GetInt32();
                        Console.WriteLine($"[AdminComplaintService] Found count: {totalCount}");
                    }
                    else
                    {
                        // If no count provided, use complaints count (not accurate for pagination, but better than 0)
                        totalCount = complaints.Count;
                        Console.WriteLine($"[AdminComplaintService] No count found, using complaints.Count: {totalCount}");
                    }
                    
                    var result = new PaginatedAdminComplaintDTO
                    {
                        Complaints = complaints,
                        TotalCount = totalCount,
                        Page = filter.Page,
                        PageSize = filter.PageSize
                    };
                    Console.WriteLine($"[AdminComplaintService] Returning result: {result.Complaints.Count} complaints, TotalCount: {result.TotalCount}");
                    return result;
                }
                
                // Check if response is a direct array (fallback)
                if (response.ValueKind == JsonValueKind.Array)
                {
                    Console.WriteLine($"[AdminComplaintService] Response is array, parsing...");
                    var complaints = JsonSerializer.Deserialize<List<AdminComplaintDTO>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<AdminComplaintDTO>();
                    
                    Console.WriteLine($"[AdminComplaintService] Parsed {complaints.Count} complaints from array");
                    
                    return new PaginatedAdminComplaintDTO
                    {
                        Complaints = complaints,
                        TotalCount = complaints.Count,
                        Page = filter.Page,
                        PageSize = filter.PageSize
                    };
                }
                
                Console.WriteLine($"[AdminComplaintService] Response is neither object with 'value' nor array. ValueKind: {response.ValueKind}");
            }
            catch (Exception ex)
            {
                // Log error for debugging
                Console.WriteLine($"[AdminComplaintService] Error: {ex.Message}");
                Console.WriteLine($"[AdminComplaintService] StackTrace: {ex.StackTrace}");
                if (!string.IsNullOrEmpty(json))
                {
                    Console.WriteLine($"[AdminComplaintService] Response: {json.Substring(0, Math.Min(500, json.Length))}");
                }
            }
            
            // Fallback to old format (PaginatedAdminComplaintDTO)
            try
            {
                return JsonSerializer.Deserialize<PaginatedAdminComplaintDTO>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                // Last resort: return empty result
                return new PaginatedAdminComplaintDTO
                {
                    Complaints = new List<AdminComplaintDTO>(),
                    TotalCount = 0,
                    Page = filter.Page,
                    PageSize = filter.PageSize
                };
            }
        }

        public async Task<ComplaintDetailsDTO?> GetDetailsAsync(Guid id, CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync($"/api/admin/admincomplaint/{id}", ct);
            if (!res.IsSuccessStatusCode)
            {
                if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
                await HandleErrorAsync(res, ct);
            }
            
            var json = await res.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<ComplaintDetailsDTO>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<object?> HandleComplaintAsync(Guid id, HandleComplaintDTO dto, CancellationToken ct = default)
        {
            AttachAuth();
            
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var res = await _http.PutAsync($"/api/admin/admincomplaint/{id}/handle", content, ct);
            await HandleErrorAsync(res, ct);
            
            var responseJson = await res.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<object>(responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<ComplaintStatisticsDTO?> GetStatisticsAsync(CancellationToken ct = default)
        {
            AttachAuth();
            var res = await _http.GetAsync("/api/admin/admincomplaint/statistics", ct);
            await HandleErrorAsync(res, ct);

            var json = await res.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<ComplaintStatisticsDTO>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}










