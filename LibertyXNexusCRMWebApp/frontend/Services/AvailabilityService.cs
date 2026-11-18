using System.Net.Http.Json;

namespace frontend.Services
{
    /// <summary>
    /// The hours for one day of the week. Times are "HH:mm".
    /// </summary>
    public class DayHours
    {
        public DayOfWeek Day { get; set; }

        public bool IsWorkingDay { get; set; }

        public string? StartTime { get; set; } = "08:00";

        public string? EndTime { get; set; } = "17:00";
    }

    /// <summary>
    /// Custom hours for one date. Date is "yyyy-MM-dd" and times are "HH:mm".
    /// </summary>
    public class HoursOverride
    {
        public string Date { get; set; } = string.Empty;

        public bool IsWorkingDay { get; set; }

        public string? StartTime { get; set; }

        public string? EndTime { get; set; }

        public string? Note { get; set; }
    }

    /// <summary>
    /// The advisor's default week and upcoming custom dates.
    /// </summary>
    public class AdvisorSchedule
    {
        public bool IsConfigured { get; set; }

        public List<DayHours> Days { get; set; } = new();

        public List<HoursOverride> Overrides { get; set; } = new();
    }

    /// <summary>
    /// What a client sees about their advisor's availability right now.
    /// </summary>
    public class AdvisorAvailability
    {
        public bool IsConfigured { get; set; }

        public bool IsAvailableNow { get; set; } = true;

        public string Message { get; set; } = string.Empty;

        public List<DayHours> Days { get; set; } = new();
    }

    /// <summary>
    /// One open meeting time. StartUtc is what gets booked; Label is the South African time to show.
    /// </summary>
    public class MeetingSlot
    {
        public DateTime StartUtc { get; set; }

        public string Label { get; set; } = string.Empty;
    }

    /// <summary>
    /// The open meeting times on one date.
    /// </summary>
    public class MeetingSlots
    {
        public bool IsConfigured { get; set; }

        public bool IsWorkingDay { get; set; } = true;

        public string Message { get; set; } = string.Empty;

        public List<MeetingSlot> Slots { get; set; } = new();
    }

    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
    /// <summary>
    /// Calls the API for the advisor's working hours.
    /// </summary>
    public class AvailabilityService
    {
        private readonly HttpClient _http;

        public AvailabilityService(HttpClient http)
        {
            _http = http;
        }

        /// <summary>
        /// Gets the signed-in advisor's default week and upcoming custom dates.
        /// </summary>
        public Task<(AdvisorSchedule? Schedule, string? Error)> GetScheduleAsync()
        {
            return SendAsync<AdvisorSchedule>(() => _http.GetAsync("availability/advisor/hours"));
        }

        /// <summary>
        /// Saves the advisor's default week. All seven days are sent.
        /// </summary>
        public Task<(AdvisorSchedule? Schedule, string? Error)> SaveWeeklyHoursAsync(List<DayHours> days)
        {
            return SendAsync<AdvisorSchedule>(() => _http.PutAsJsonAsync("availability/advisor/hours", new { Days = days }));
        }

        /// <summary>
        /// Adds or replaces the custom hours for one date.
        /// </summary>
        public Task<(AdvisorSchedule? Schedule, string? Error)> SaveOverrideAsync(HoursOverride hoursOverride)
        {
            return SendAsync<AdvisorSchedule>(() => _http.PutAsJsonAsync("availability/advisor/overrides", hoursOverride));
        }

        /// <summary>
        /// Removes the custom hours for a date (yyyy-MM-dd). Returns an error message, or null if it worked.
        /// </summary>
        public async Task<string?> DeleteOverrideAsync(string date)
        {
            try
            {
                var response = await _http.DeleteAsync($"availability/advisor/overrides/{Uri.EscapeDataString(date)}");

                // Already gone counts as removed.
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                return await ReadErrorAsync(response);
            }
            catch (HttpRequestException)
            {
                return "Can't reach the server. Please try again.";
            }
            catch (Exception)
            {
                return "Something went wrong. Please try again.";
            }
        }

        /// <summary>
        /// For a client: whether their advisor is available right now.
        /// </summary>
        public Task<(AdvisorAvailability? Status, string? Error)> GetStatusAsync()
        {
            return SendAsync<AdvisorAvailability>(() => _http.GetAsync("availability/status"));
        }

        /// <summary>
        /// For a client: the open meeting times with their advisor on a date (yyyy-MM-dd).
        /// </summary>
        public Task<(MeetingSlots? Slots, string? Error)> GetSlotsAsync(string date, int durationMinutes = 60)
        {
            return SendAsync<MeetingSlots>(() =>
                _http.GetAsync($"availability/slots?date={Uri.EscapeDataString(date)}&durationMinutes={durationMinutes}"));
        }

        /// <summary>
        /// Sends a request and reads the reply, turning every kind of failure into a message for the page.
        /// </summary>
        private static async Task<(T? Value, string? Error)> SendAsync<T>(Func<Task<HttpResponseMessage>> send) where T : class
        {
            try
            {
                var response = await send();

                if (!response.IsSuccessStatusCode)
                {
                    return (null, await ReadErrorAsync(response));
                }

                var value = await response.Content.ReadFromJsonAsync<T>();

                return value is null
                    ? (null, "The server sent back an empty reply. Please try again.")
                    : (value, null);
            }
            catch (HttpRequestException)
            {
                return (null, "Can't reach the server. Please try again.");
            }
            catch (Exception)
            {
                return (null, "Something went wrong. Please try again.");
            }
        }

        /// <summary>
        /// Pulls the API's own message out of an error reply, e.g. { "message": "Choose at least one working day." }.
        /// </summary>
        private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
        {
            var status = (int)response.StatusCode;

            if (status == 401 || status == 403)
            {
                return "You don't have permission to do that. Please sign in again.";
            }

            if (status == 429)
            {
                return "Too many requests. Please wait a moment and try again.";
            }

            try
            {
                var body = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrWhiteSpace(body))
                {
                    using var json = System.Text.Json.JsonDocument.Parse(body);
                    var root = json.RootElement;

                    if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        if (root.TryGetProperty("message", out var message)
                            && message.ValueKind == System.Text.Json.JsonValueKind.String
                            && !string.IsNullOrWhiteSpace(message.GetString()))
                        {
                            return message.GetString()!;
                        }

                        // Validation errors look like { "errors": { "Date": [ "..." ] } }
                        if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            foreach (var field in errors.EnumerateObject())
                            {
                                if (field.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    foreach (var item in field.Value.EnumerateArray())
                                    {
                                        if (item.ValueKind == System.Text.Json.JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                                        {
                                            return item.GetString()!;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Not JSON, so fall through to the plain message below.
            }

            return $"The server reported an error (status {status}).";
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
