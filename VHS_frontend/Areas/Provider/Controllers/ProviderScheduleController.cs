using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Areas.Provider.Models.Schedule;
using System.Text.Json;

namespace VHS_frontend.Areas.Provider.Controllers
{
    [Area("Provider")]
    public class ProviderScheduleController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ProviderScheduleController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        private string GetApiBaseUrl()
        {
            return _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5154";
        }

        private string? GetJwtToken()
        {
            return HttpContext.Session.GetString("JWToken");
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var jwt = GetJwtToken();
                if (string.IsNullOrEmpty(jwt))
                {
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                var overview = await GetOverviewAsync(jwt);
                
                if (overview == null)
                {
                    Console.WriteLine("WARNING - Overview is null, creating empty model");
                    overview = new ScheduleOverviewViewModel
                    {
                        Schedules = new List<ReadScheduleViewModel>(),
                        UpcomingTimeOffs = new List<ReadTimeOffViewModel>(),
                        DailyLimits = new List<ReadDailyLimitViewModel>()
                    };
                }
                else
                {
                    // Ensure lists are initialized
                    overview.Schedules ??= new List<ReadScheduleViewModel>();
                    overview.UpcomingTimeOffs ??= new List<ReadTimeOffViewModel>();
                    overview.DailyLimits ??= new List<ReadDailyLimitViewModel>();
                    
                    Console.WriteLine($"SUCCESS - Loaded Overview - Schedules: {overview.Schedules.Count}, TimeOffs: {overview.UpcomingTimeOffs.Count}, DailyLimits: {overview.DailyLimits.Count}");
                }
                
                ViewData["Title"] = "Quản lý lịch làm việc";
                return View(overview);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR - ProviderSchedule Index: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                var emptyModel = new ScheduleOverviewViewModel
                {
                    Schedules = new List<ReadScheduleViewModel>(),
                    UpcomingTimeOffs = new List<ReadTimeOffViewModel>(),
                    DailyLimits = new List<ReadDailyLimitViewModel>()
                };
                return View(emptyModel);
            }
        }

        private async Task<ScheduleOverviewViewModel?> GetOverviewAsync(string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{GetApiBaseUrl()}/api/ProviderSchedule/overview");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            
            // Log to console
            Console.WriteLine("\n=== ProviderSchedule Overview Response ===");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Response JSON: {json}");
            Console.WriteLine("=== End Response ===\n");
            
            try
            {
                var result = JsonSerializer.Deserialize<ApiResponse<ScheduleOverviewViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                Console.WriteLine($"Success: {result?.Success}");
                Console.WriteLine($"Data is null: {result?.Data == null}");
                
                if (result?.Data != null)
                {
                    Console.WriteLine($"ProviderId: {result.Data.ProviderId}");
                    Console.WriteLine($"ProviderName: {result.Data.ProviderName}");
                    Console.WriteLine($"Schedules count: {result.Data.Schedules?.Count ?? 0}");
                    Console.WriteLine($"TimeOffs count: {result.Data.UpcomingTimeOffs?.Count ?? 0}");
                    Console.WriteLine($"DailyLimits count: {result.Data.DailyLimits?.Count ?? 0}");
                }
                
                return result?.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR - Deserialization error: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                throw;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTimeOffs(DateOnly startDate, DateOnly endDate)
        {
            var jwt = GetJwtToken();
            if (string.IsNullOrEmpty(jwt))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var request = new HttpRequestMessage(HttpMethod.Get, 
                $"{GetApiBaseUrl()}/api/ProviderSchedule/time-offs?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"GetTimeOffs Response: {json}");

            if (!response.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = json });
            }

            var backendResult = JsonSerializer.Deserialize<ApiResponse<IEnumerable<ReadTimeOffViewModel>>>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Console.WriteLine($"Parsed TimeOffs count: {backendResult?.Data?.Count() ?? 0}");
            
            if (backendResult?.Success == true && backendResult.Data != null)
            {
                return Json(new { success = true, data = backendResult.Data });
            }
            
            return Json(new { success = false, data = new List<ReadTimeOffViewModel>() });
        }

        [HttpPost]
        public async Task<IActionResult> CreateSchedule([FromBody] CreateScheduleViewModel model)
        {
            var jwt = GetJwtToken();
            if (string.IsNullOrEmpty(jwt))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var dto = new
            {
                dayOfWeek = model.DayOfWeek,
                startTime = model.StartTime,
                endTime = model.EndTime,
                bookingLimit = model.BookingLimit
            };

            Console.WriteLine($"Creating Schedule - DayOfWeek: {dto.dayOfWeek}, StartTime: {dto.startTime}, EndTime: {dto.endTime}");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{GetApiBaseUrl()}/api/ProviderSchedule/schedules");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
            request.Content = new StringContent(JsonSerializer.Serialize(dto), System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"CreateSchedule Response: {response.StatusCode} - {json}");

            if (!response.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = json });
            }

            return Json(new { success = true, data = json });
        }

        [HttpPost]
        public async Task<IActionResult> CreateWeeklySchedule([FromBody] CreateWeeklyScheduleViewModel model)
        {
            var jwt = GetJwtToken();
            if (string.IsNullOrEmpty(jwt))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var request = new HttpRequestMessage(HttpMethod.Post, $"{GetApiBaseUrl()}/api/ProviderSchedule/schedules/weekly");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
            request.Content = new StringContent(JsonSerializer.Serialize(model), System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = json });
            }

            return Json(new { success = true, data = json });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSchedule(string id, [FromBody] UpdateScheduleViewModel model)
        {
            var jwt = GetJwtToken();
            if (string.IsNullOrEmpty(jwt))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var dto = new
            {
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                BookingLimit = model.BookingLimit
            };

            Console.WriteLine($"Updating Schedule {id} - StartTime: {dto.StartTime}, EndTime: {dto.EndTime}, BookingLimit: {dto.BookingLimit}");

            var request = new HttpRequestMessage(HttpMethod.Put, $"{GetApiBaseUrl()}/api/ProviderSchedule/schedules/{id}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
            request.Content = new StringContent(JsonSerializer.Serialize(dto), System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"UpdateSchedule Response: {response.StatusCode} - {json}");

            if (!response.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = json });
            }

            return Json(new { success = true, data = json });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteSchedule(string id)
        {
            var jwt = GetJwtToken();
            if (string.IsNullOrEmpty(jwt))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            Console.WriteLine($"Attempting to delete schedule: {id}");

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{GetApiBaseUrl()}/api/ProviderSchedule/schedules/{id}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Delete schedule response: Status={response.StatusCode}, Body={json}");

            if (!response.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = json });
            }

            return Json(new { success = true, data = json });
        }

        [HttpPost]
        public async Task<IActionResult> CreateTimeOff([FromBody] CreateTimeOffViewModel model)
        {
            var jwt = GetJwtToken();
            if (string.IsNullOrEmpty(jwt))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            // Pass data to backend
            var dto = new
            {
                date = model.Date,
                startTime = model.StartTime,
                endTime = model.EndTime,
                reason = model.Reason
            };

            Console.WriteLine($"Creating TimeOff - Date: {dto.date}, StartTime: {dto.startTime}, EndTime: {dto.endTime}, Reason: {dto.reason}");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{GetApiBaseUrl()}/api/ProviderSchedule/time-offs");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
            var serialized = JsonSerializer.Serialize(dto);
            Console.WriteLine($"Request body: {serialized}");
            request.Content = new StringContent(serialized, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Response status: {response.StatusCode}");
            Console.WriteLine($"Response body: {json}");

            if (!response.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = json });
            }

            return Json(new { success = true, data = json });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteTimeOff(string id)
        {
            var jwt = GetJwtToken();
            if (string.IsNullOrEmpty(jwt))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{GetApiBaseUrl()}/api/ProviderSchedule/time-offs/{id}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = json });
            }

            return Json(new { success = true, data = json });
        }

        private class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T? Data { get; set; }
        }
    }
}
