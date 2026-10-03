using System.Net.Http.Json;

namespace frontend.Services
{
    // DTOs
    public record MeetingSummary(
        int MeetingId,
        int ClientId,
        string ClientName,
        DateTime MeetingDate,
        int DurationMinutes,
        string? MeetingType,
        string? Location,
        string Status,
        string? Notes,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
    /// <summary>
    /// Handles the advisors meetings
    /// </summary>
    public class MeetingService
    {
        private readonly HttpClient _http;
        private readonly MessageNotifier _notifier;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        ///  Initializes a new instance of the <see cref="MeetingService"/> class with the specified <see cref="HttpClient"/> <see cref="MessageNotifier" />.
        /// </summary>
        /// <param name="http"></param>
        /// <param name="notifier"></param>
        public MeetingService(HttpClient http, MessageNotifier notifier)
        {
            _http = http;
            _notifier = notifier;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets all the meetings that are assigned to a client.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<(List<MeetingSummary> Meetings, string? Error)> GetForClientAsync(int clientId)
        {
            try
            {
                var response = await _http.GetAsync($"meetings/client/{clientId}");
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets all the meetings that are assigned to an advisor.
        /// </summary>
        /// <param name="advisorId"></param>
        /// <returns></returns>
        public async Task<(List<MeetingSummary> Meetings, string? Error)> GetForAdvisorAsync(int advisorId)
        {
            try
            {
                var response = await _http.GetAsync($"meetings/upcoming");
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Requests a meeting with an advisor.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="advisorId"></param>
        /// <param name="fromAdvisor"></param>
        /// <param name="meetingDate"></param>
        /// <param name="notes"></param>
        /// <returns></returns>
        public async Task<string?> RequestAsync(int clientId, int advisorId, bool fromAdvisor, DateTime meetingDate, string? notes)
        {
            try
            {
                var request = new
                {
                    ClientId = clientId,
                    MeetingDate =  meetingDate,
                    DurationMinutes = 60,
                    MeetingType = "Consulation",
                    Location = (string?)null,
                    Notes = notes
                };
                var response = await _http.PostAsJsonAsync("meetings", request);

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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Advisor responds to meeting requests
        /// </summary>
        /// <param name="meetingId"></param>
        /// <param name="clientId"></param>
        /// <param name="accept"></param>
        /// <returns></returns>
        public async Task<string?> RespondAsync(int meetingId, int clientId, bool accept)
        {
            try
            {
                var action = accept ? "confirm" : "cancel"; 
                var response = await _http.PutAsync($"meetings/{meetingId}/{action}", null);
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Inspect the details of an individual meeting.
        /// </summary>
        /// <param name="meetingId"></param>
        /// <returns></returns>
        public async Task<(MeetingSummary? Meeting, string? Error)> GetByIdAsync(int meetingId)
        {
            try
            {
                var response = await _http.GetAsync($"meetings/{meetingId}");

                if (!response.IsSuccessStatusCode)
                {
                    return (null, $"The server reported an error (status {(int)response.StatusCode}).");
                }

                var result = await response.Content.ReadFromJsonAsync<MeetingSummary>();

                return result is null
                    ? (null, "The API returned an empty meeting.")
                    : (result, null);
            }
            catch (HttpRequestException)
            {
                return (null, "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (null, $"Something went wrong: {ex.Message}");
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Cancel an upcoming meeting that was accepted.
        /// </summary>
        /// <param name="meetingId"></param>
        /// <returns></returns>
        public async Task<string?> CancelAsync(int meetingId) 
        { 
            try 
            { 
                var response = await _http.PutAsync( $"Meetings/{meetingId}/cancel", null); 
                if (!response.IsSuccessStatusCode) 
                {
                    return await ReadErrorAsync(response); 
                } 
                return null; 
            } 
            catch (HttpRequestException) 
            { 
                return "Can't reach the server. Make sure the Backend project is running."; 
            } 
            catch (Exception ex) 
            { return $"Something went wrong: {ex.Message}"; 
            } 
        } 
        
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Confirm a meeting request.
        /// </summary>
        /// <param name="meetingId"></param>
        /// <returns></returns>
        public async Task<string?> ConfirmAsync(int meetingId) 
        { 
            try 
            { 
                var response = await _http.PutAsync( $"Meetings/{meetingId}/confirm", null); 
                if (!response.IsSuccessStatusCode) 
                { 
                    return await ReadErrorAsync(response); 
                } 
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Reads the error message from the HTTP response. If the response contains a JSON error message, it returns that message. Otherwise, it returns a default error message based on the HTTP status code.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private static async Task<string> ReadErrorAsync(HttpResponseMessage response) 
        { 
            var body = await response.Content.ReadAsStringAsync();
            
            if (!string.IsNullOrWhiteSpace(body)) 
            { 
                return $"API returned HTTP {(int)response.StatusCode}: {body}"; 
            } 
            return $"The server reported an error " + $"(status {(int)response.StatusCode})."; 
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
