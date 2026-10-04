using System.Net.Http.Json;

namespace frontend.Services
{
    /// <summary>
    /// One notification shown in the bell dropdown.
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
    /// The advisor's notifications plus how many are unread.
    /// </summary>
    public record NotificationFeed(int UnreadCount, List<NotificationItem> Items);

    /// <summary>
    /// This service gets the advisor's notifications from the API and marks them as read.
    /// </summary>
    public class NotificationService
    {
        private readonly HttpClient _http;

        /// <summary>
        /// Sets up the service with the HttpClient.
        /// </summary>
        public NotificationService(HttpClient http)
        {
            _http = http;
        }

        /// <summary>
        /// Gets all the notifications for an advisor.
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
        /// Gets how many unread notifications the advisor has.
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
        /// Marks one notification as read.
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
        /// Marks all of the advisor's notifications as read.
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
