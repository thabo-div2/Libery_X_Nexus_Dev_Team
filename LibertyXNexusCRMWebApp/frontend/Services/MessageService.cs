using System.Net.Http.Json;

namespace frontend.Services
{
    /// <summary>
    /// One chat message.
    /// </summary>
    public record ChatMessage(int Id, int ClientId, int AdvisorId, bool FromAdvisor, string Text, DateTime SentAt);

    /// <summary>
    /// A conversation in the advisor's list.
    /// </summary>
    public record ConversationSummary(int ClientId, string ClientName, string? LastMessage, DateTime? LastMessageAt);

    /// <summary>
    /// Sends and gets chat messages.
    /// </summary>
    public class MessageService
    {
        private readonly HttpClient _http;
        private readonly MessageNotifier _notifier;

        /// <summary>
        /// Sets up the service.
        /// </summary>
        public MessageService(HttpClient http, MessageNotifier notifier)
        {
            _http = http;
            _notifier = notifier;
        }

        /// <summary>
        /// Gets a client's messages.
        /// </summary>
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
                return (new List<ChatMessage>(), "Can't reach the API. Make sure the API project is running.");
            }
            catch (Exception ex)
            {
                return (new List<ChatMessage>(), $"Something went wrong: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all of an advisor's chats.
        /// </summary>
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
                return (new List<ConversationSummary>(), "Can't reach the API. Make sure the API project is running.");
            }
            catch (Exception ex)
            {
                return (new List<ConversationSummary>(), $"Something went wrong: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a message.
        /// </summary>
        public async Task<string?> SendAsync(int clientId, int advisorId, bool fromAdvisor, string text)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("Message", new { ClientId = clientId, AdvisorId = advisorId, Text = text });
                if (!response.IsSuccessStatusCode)
                {
                    return $"The server reported an error (status {(int)response.StatusCode}).";
                }

                _notifier.NotifyMessageSent(clientId);
                return null;
            }
            catch (HttpRequestException)
            {
                return "Can't reach the API. Make sure the API project is running.";
            }
            catch (Exception ex)
            {
                return $"Something went wrong: {ex.Message}";
            }
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
