using Shared.Models.Enums;
using System.Net.Http.Json;

namespace frontend.Services
{
    /// <summary>
    /// A document the advisor asked a client for.
    /// </summary>
    public record DocumentRequestSummary(
        int DocumentRequestId, int ClientId, string DocumentType, string? Note,
        DateTime CreatedAt, DateTime? FulfilledAt, bool IsFulfilled, int? PolicyId = null, string? PolicyName = null);

    /// <summary>
    /// Calls the API for document requests.
    /// </summary>
    public class DocumentRequestService
    {
        private readonly HttpClient _http;

        public DocumentRequestService(HttpClient http)
        {
            _http = http;
        }

        /// <summary>
        /// Gets the documents a client has been asked for.
        /// </summary>
        public Task<(List<DocumentRequestSummary>? Requests, string? Error)> GetForClientAsync(int clientId)
        {
            return ApiErrors.SendAsync<List<DocumentRequestSummary>>(() => _http.GetAsync($"documentrequests/client/{clientId}"));
        }

        /// <summary>
        /// Asks a client for a document. Advisor only.
        /// </summary>
        public Task<(DocumentRequestSummary? Request, string? Error)> CreateAsync(int clientId, DocumentType documentType, string? note, int? policyId = null)
        {
            return ApiErrors.SendAsync<DocumentRequestSummary>(() =>
                _http.PostAsJsonAsync($"documentrequests/client/{clientId}", new { DocumentType = documentType, Note = note, PolicyId = policyId }));
        }

        /// <summary>
        /// Removes a request. Returns an error message, or null if it worked.
        /// </summary>
        public Task<string?> CancelAsync(int documentRequestId)
        {
            return ApiErrors.SendAsync(() => _http.DeleteAsync($"documentrequests/{documentRequestId}"));
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
