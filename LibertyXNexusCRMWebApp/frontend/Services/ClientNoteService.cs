using System.Net.Http.Json;

namespace frontend.Services
{
    /// <summary>
    /// A private note the advisor keeps about a client.
    /// </summary>
    public record ClientNoteSummary(int ClientNoteId, int ClientId, string Text, DateTime CreatedAt);

    /// <summary>
    /// Calls the API for the advisor's private client notes.
    /// </summary>
    public class ClientNoteService
    {
        private readonly HttpClient _http;

        public ClientNoteService(HttpClient http)
        {
            _http = http;
        }

        public Task<(List<ClientNoteSummary>? Notes, string? Error)> GetForClientAsync(int clientId)
        {
            return ApiErrors.SendAsync<List<ClientNoteSummary>>(() => _http.GetAsync($"clientnotes/client/{clientId}"));
        }

        public Task<(ClientNoteSummary? Note, string? Error)> CreateAsync(int clientId, string text)
        {
            return ApiErrors.SendAsync<ClientNoteSummary>(() => _http.PostAsJsonAsync($"clientnotes/client/{clientId}", new { Text = text }));
        }

        /// <summary>
        /// Deletes a note. Returns an error message, or null if it worked.
        /// </summary>
        public Task<string?> DeleteAsync(int clientNoteId)
        {
            return ApiErrors.SendAsync(() => _http.DeleteAsync($"clientnotes/{clientNoteId}"));
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
