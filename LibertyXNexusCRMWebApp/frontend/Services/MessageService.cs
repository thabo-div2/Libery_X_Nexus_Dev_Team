using System.Net.Http.Json;

namespace frontend.Services
{
    public record ChatMessage(int Id, int ClientId, int AdvisorId, bool FromAdvisor, string Text, DateTime SentAt);
    public record ConversationSummary(int ClientId, string ClientName, string? LastMessage, DateTime? LastMessageAt);

    public class MessageService
    {
        private readonly HttpClient _http;

        public MessageService(HttpClient http)
        {
            _http = http;
        }

        public async Task<(List<ChatMessage> Messages, string? Error)> GetConversationAsync(int clientId)
        {
            try
            {
                var response = await _http.GetAsync($"Message/client/{clientId}");
                if (!response.IsSuccessStatusCode)
                {
                    return (new List<ChatMessage>(), $"The server reported an error (status {(int)response.StatusCode}).");
                }

                var result = await response.Content.ReadFromJsonAsync<List<ChatMessage>>();
                return (result ?? new List<ChatMessage>(), null);
            }
            catch (HttpRequestException)
            {
                return (new List<ChatMessage>(), "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (new List<ChatMessage>(), $"Something went wrong: {ex.Message}");
            }
        }

        public async Task<(List<ConversationSummary> Conversations, string? Error)> GetConversationsForAdvisorAsync(int advisorId)
        {
            try
            {
                var response = await _http.GetAsync($"Message/advisor/{advisorId}");
                if (!response.IsSuccessStatusCode)
                {
                    return (new List<ConversationSummary>(), $"The server reported an error (status {(int)response.StatusCode}).");
                }

                var result = await response.Content.ReadFromJsonAsync<List<ConversationSummary>>();
                return (result ?? new List<ConversationSummary>(), null);
            }
            catch (HttpRequestException)
            {
                return (new List<ConversationSummary>(), "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (new List<ConversationSummary>(), $"Something went wrong: {ex.Message}");
            }
        }

        public async Task<string?> SendAsync(int clientId, int advisorId, bool fromAdvisor, string text)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("Message", new { ClientId = clientId, AdvisorId = advisorId, FromAdvisor = fromAdvisor, Text = text });
                return response.IsSuccessStatusCode ? null : $"The server reported an error (status {(int)response.StatusCode}).";
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
