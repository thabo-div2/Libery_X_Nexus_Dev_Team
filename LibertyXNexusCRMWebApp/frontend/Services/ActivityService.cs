namespace frontend.Services
{
    /// <summary>
    /// One line of the activity log.
    /// </summary>
    public record ActivityItem(DateTime Timestamp, string Action, string Entity, int? EntityId, string? Details);

    /// <summary>
    /// Calls the API for the advisor's activity log.
    /// </summary>
    public class ActivityService
    {
        private readonly HttpClient _http;

        public ActivityService(HttpClient http)
        {
            _http = http;
        }

        /// <summary>
        /// Gets the signed-in advisor's most recent activity, newest first.
        /// </summary>
        public Task<(List<ActivityItem>? Items, string? Error)> GetRecentAsync(int count = 200)
        {
            return ApiErrors.SendAsync<List<ActivityItem>>(() => _http.GetAsync($"activity?count={count}"));
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
