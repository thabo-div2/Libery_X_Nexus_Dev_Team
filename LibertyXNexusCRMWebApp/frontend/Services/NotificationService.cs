using System.Net.Http.Json;

namespace frontend.Services
{
    public record NotificationItem(
        int NotificationId,
        string Type,
        string Message,
        string? LinkUrl,
        bool IsRead,
        DateTime CreatedAt,
        bool IsReminder);

    public record NotificationFeed(int UnreadCount, List<NotificationItem> Items);

    public class NotificationService
    {
        private readonly HttpClient _http;

        public NotificationService(HttpClient http)
        {
            _http = http;
        }

        public async Task<NotificationFeed> GetFeedForAdvisorAsync(int advisorId)
        {
            try
            {
                var response = await _http.GetAsync($"notifications/advisor/{advisorId}");

                if (!response.IsSuccessStatusCode)
                {
                    return new NotificationFeed(0, new List<NotificationItem>());
                }

                var feed = await response.Content.ReadFromJsonAsync<NotificationFeed>();
                return feed ?? new NotificationFeed(0, new List<NotificationItem>());
            }
            catch (HttpRequestException)
            {
                return new NotificationFeed(0, new List<NotificationItem>());
            }
        }

        public async Task<int> GetUnreadCountForAdvisorAsync(int advisorId)
        {
            try
            {
                var response = await _http.GetAsync($"notifications/advisor/{advisorId}/unread-count");
                
                if (!response.IsSuccessStatusCode)
                {
                    return 0;
                }

                return await response.Content.ReadFromJsonAsync<int>();
            }
            catch (HttpRequestException)
            {
                return 0;
            }
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            try
            {
                var response = await _http.PutAsync($"notifications/{notificationId}/read", new StringContent(string.Empty));
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }

        public async Task<bool> MarkAllAsReadAsync(int advisorId)
        {
            try
            {
                var response = await _http.PutAsync($"notifications/advisor/{advisorId}/read-all", new StringContent(string.Empty));
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }
    }
}
