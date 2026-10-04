using System.Net.Http.Json;

namespace frontend.Services
{
    /// <summary>
    /// One notification.
    /// </summary>
    public record NotificationItem(
        int NotificationId,
        string Type,
        string Message,
        string? LinkUrl,
        bool IsRead,
        DateTime CreatedAt,
        bool IsReminder);

    /// <summary>
    /// The notifications and unread count.
    /// </summary>
    public record NotificationFeed(int UnreadCount, List<NotificationItem> Items);

    /// <summary>
    /// Gets and updates notifications.
    /// </summary>
    public class NotificationService
    {
        private readonly HttpClient _http;

        /// <summary>
        /// Sets up the service.
        /// </summary>
        public NotificationService(HttpClient http)
        {
            _http = http;
        }

        /// <summary>
        /// Gets the advisor's notifications.
        /// </summary>
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

        /// <summary>
        /// Gets the unread count.
        /// </summary>
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

        /// <summary>
        /// Marks one as read.
        /// </summary>
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

        /// <summary>
        /// Marks all as read.
        /// </summary>
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

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
