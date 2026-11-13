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
        /// Gets the signed-in user's notifications. Clients and advisors have separate
        /// API routes, so pass whether this is a client.
        /// </summary>
        public async Task<NotificationFeed> GetFeedAsync(int userId, bool isClient)
        {
            try
            {
                var response = await _http.GetAsync($"notifications/{Owner(isClient)}/{userId}");

                if (!response.IsSuccessStatusCode)
                {
                    return new NotificationFeed(0, new List<NotificationItem>());
                }

                var feed = await response.Content.ReadFromJsonAsync<NotificationFeed>();
                return feed ?? new NotificationFeed(0, new List<NotificationItem>());
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or System.Text.Json.JsonException)
            {
                return new NotificationFeed(0, new List<NotificationItem>());
            }
        }

        /// <summary>
        /// Gets the advisor's notifications.
        /// </summary>
        public Task<NotificationFeed> GetFeedForAdvisorAsync(int advisorId) => GetFeedAsync(advisorId, isClient: false);

        /// <summary>
        /// Gets the signed-in user's unread count.
        /// </summary>
        public async Task<int> GetUnreadCountAsync(int userId, bool isClient)
        {
            try
            {
                var response = await _http.GetAsync($"notifications/{Owner(isClient)}/{userId}/unread-count");
                
                if (!response.IsSuccessStatusCode)
                {
                    return 0;
                }

                return await response.Content.ReadFromJsonAsync<int>();
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or System.Text.Json.JsonException)
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the advisor's unread count.
        /// </summary>
        public Task<int> GetUnreadCountForAdvisorAsync(int advisorId) => GetUnreadCountAsync(advisorId, isClient: false);

        private static string Owner(bool isClient) => isClient ? "client" : "advisor";

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
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
            {
                return false;
            }
        }

        /// <summary>
        /// Marks all of the signed-in user's notifications as read.
        /// </summary>
        public async Task<bool> MarkAllAsReadAsync(int userId, bool isClient = false)
        {
            try
            {
                var response = await _http.PutAsync($"notifications/{Owner(isClient)}/{userId}/read-all", new StringContent(string.Empty));
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
            {
                return false;
            }
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
