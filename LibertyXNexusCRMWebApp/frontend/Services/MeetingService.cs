using System.Net.Http.Json;

namespace frontend.Services
{
    public record MeetingSummary(int MeetingId, int ClientId, int AdvisorId, string ClientName, DateTime MeetingDate, string Status, string? Notes, DateTime CreatedAt);

    public class MeetingService
    {
        private readonly HttpClient _http;
        private readonly MessageNotifier _notifier;

        public MeetingService(HttpClient http, MessageNotifier notifier)
        {
            _http = http;
            _notifier = notifier;
        }

        public async Task<(List<MeetingSummary> Meetings, string? Error)> GetForClientAsync(int clientId)
        {
            try
            {
                var response = await _http.GetAsync($"Meeting/client/{clientId}");
                if (!response.IsSuccessStatusCode)
                {
                    return (new List<MeetingSummary>(), $"The server reported an error (status {(int)response.StatusCode}).");
                }

                var result = await response.Content.ReadFromJsonAsync<List<MeetingSummary>>();
                return (result ?? new List<MeetingSummary>(), null);
            }
            catch (HttpRequestException)
            {
                return (new List<MeetingSummary>(), "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (new List<MeetingSummary>(), $"Something went wrong: {ex.Message}");
            }
        }

        public async Task<(List<MeetingSummary> Meetings, string? Error)> GetForAdvisorAsync(int advisorId)
        {
            try
            {
                var response = await _http.GetAsync($"Meeting/advisor/{advisorId}");
                if (!response.IsSuccessStatusCode)
                {
                    return (new List<MeetingSummary>(), $"The server reported an error (status {(int)response.StatusCode}).");
                }

                var result = await response.Content.ReadFromJsonAsync<List<MeetingSummary>>();
                return (result ?? new List<MeetingSummary>(), null);
            }
            catch (HttpRequestException)
            {
                return (new List<MeetingSummary>(), "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (new List<MeetingSummary>(), $"Something went wrong: {ex.Message}");
            }
        }

        public async Task<string?> RequestAsync(int clientId, int advisorId, bool fromAdvisor, DateTime meetingDate, string? notes)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("Meeting/request", new { ClientId = clientId, AdvisorId = advisorId, FromAdvisor = fromAdvisor, MeetingDate = meetingDate, Notes = notes });
                if (!response.IsSuccessStatusCode)
                {
                    return $"The server reported an error (status {(int)response.StatusCode}).";
                }

                _notifier.NotifyMessageSent(clientId);
                return null;
            }
            catch (HttpRequestException)
            {
                return "Can't reach the server. Make sure the Backend project is running.";
            }
            catch (Exception ex)
            {
                return $"Something went wrong: {ex.Message}";
            }
        }

        public async Task<string?> RespondAsync(int meetingId, int clientId, bool accept)
        {
            try
            {
                var response = await _http.PostAsJsonAsync($"Meeting/{meetingId}/respond", new { Accept = accept });
                if (!response.IsSuccessStatusCode)
                {
                    return $"The server reported an error (status {(int)response.StatusCode}).";
                }

                _notifier.NotifyMessageSent(clientId);
                return null;
            }
            catch (HttpRequestException)
            {
                return "Can't reach the server. Make sure the Backend project is running.";
            }
            catch (Exception ex)
            {
                return $"Something went wrong: {ex.Message}";
            }
        }
    }
}
