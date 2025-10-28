using System.Text;
using System.Text.Json;

namespace VHS_frontend.Services.Customer
{
    /// <summary>
    /// Service để gọi API ChatboxAI từ Backend
    /// </summary>
    public class ChatboxService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ChatboxService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        /// <summary>
        /// Tạo hoặc lấy conversation đang active
        /// </summary>
        public async Task<ConversationResponse?> GetOrCreateConversationAsync(string jwt, string? sessionId = null, string language = "vi")
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/ChatboxAI/conversation");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
                
                var dto = new
                {
                    sessionId = sessionId,
                    language = language
                };
                
                var json = JsonSerializer.Serialize(dto);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ConversationResponse>(jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting conversation: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lấy conversation đang active
        /// </summary>
        public async Task<ConversationResponse?> GetActiveConversationAsync(string jwt, string? sessionId = null)
        {
            try
            {
                var url = "/api/ChatboxAI/conversation/active";
                if (!string.IsNullOrEmpty(sessionId))
                {
                    url += $"?sessionId={sessionId}";
                }

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ConversationResponse>(jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting active conversation: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gửi tin nhắn và nhận phản hồi từ AI
        /// </summary>
        public async Task<MessageResponse?> SendMessageAsync(string jwt, string? sessionId, string content, string language = "vi")
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/ChatboxAI/message");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
                
                var dto = new
                {
                    content = content,
                    sessionId = sessionId,
                    language = language
                };
                
                var json = JsonSerializer.Serialize(dto);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var jsonString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error sending message: {jsonString}");
                    return null;
                }

                return JsonSerializer.Deserialize<MessageResponse>(jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lấy lịch sử conversation
        /// </summary>
        public async Task<ConversationHistoryResponse?> GetConversationHistoryAsync(string jwt, Guid conversationId, int page = 1, int pageSize = 50)
        {
            try
            {
                var url = $"/api/ChatboxAI/conversation/{conversationId}/history?page={page}&pageSize={pageSize}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ConversationHistoryResponse>(jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting conversation history: {ex.Message}");
                return null;
            }
        }
    }

    // DTOs cho Chatbox
    public class ConversationResponse
    {
        public Guid ConversationId { get; set; }
        public Guid? UserId { get; set; }
        public string SessionId { get; set; } = "";
        public string Language { get; set; } = "vi";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MessageResponse
    {
        public Guid MessageId { get; set; }
        public Guid ConversationId { get; set; }
        public string Content { get; set; } = "";
        public string SenderType { get; set; } = "";
        public string MessageType { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string? Metadata { get; set; }
        public List<QuickActionDTO>? QuickActions { get; set; }
    }

    public class QuickActionDTO
    {
        public string Label { get; set; } = "";
        public string Action { get; set; } = "";
        public string? Data { get; set; }
    }

    public class ConversationHistoryResponse
    {
        public Guid ConversationId { get; set; }
        public List<MessageDTO> Messages { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class MessageDTO
    {
        public Guid MessageId { get; set; }
        public Guid ConversationId { get; set; }
        public string Content { get; set; } = "";
        public string SenderType { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}




