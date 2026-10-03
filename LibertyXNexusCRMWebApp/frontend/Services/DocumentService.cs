using Microsoft.AspNetCore.Components.Forms;
using Shared.Models.Enums;
using System.Net.Http.Headers;

namespace frontend.Services
{
    /// <summary>
    /// A lightweight summary DTO representing a document returned by the API.
    /// </summary>
    /// <param name="DocumentId">Unique id for the document.</param>
    /// <param name="ClientId">The client this document belongs to.</param>
    /// <param name="PolicyId">Optional policy id associated with the document.</param>
    /// <param name="FileName">Original filename uploaded by the user.</param>
    /// <param name="ContentType">MIME type of the file.</param>
    /// <param name="FileSizeBytes">Size of the file in bytes.</param>
    /// <param name="DocumentType">Logical document type (string representation).</param>
    /// <param name="VisibleToClient">Whether the client can view/download the document.</param>
    /// <param name="UploadedBy">Username who uploaded the document.</param>
    /// <param name="UpdatedAt">Last update timestamp, if any.</param>
    public record DocumentSummary(
        int DocumentId,
        int ClientId,
        int PolicyId,
        string FileName,
        string ContentType,
        long FileSizeBytes,
        string DocumentType,
        bool VisibleToClient,
        string? UploadedBy,
        DateTime? UpdatedAt);

    /// <summary>
    /// Service used by the Blazor frontend to call the Documents API endpoints.
    /// Contains convenience methods for retrieving, uploading and deleting documents.
    /// </summary>
    public class DocumentService
    {
        private readonly HttpClient _http;

        // -------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of <see cref="DocumentService"/>.
        /// </summary>
        /// <param name="http">Configured HttpClient for calling the backend API.</param>
        public DocumentService(HttpClient http)
        {
            _http = http;
        }

        /// <summary>
        /// Retrieve a single document summary by id.
        /// </summary>
        /// <param name="documentId">Document id to fetch.</param>
        /// <returns>Tuple of document summary (or null) and error message (or null).</returns>
        public async Task<(DocumentSummary? Document, string? Error)> GetByIdAsync(int documentId)
        {
            try
            {
                // Send GET request to the API for the specified document id
                var response = await _http.GetAsync($"documents/{documentId}");

                // If the API returned 404, surface a friendly message
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return (null, "That document could not be found.");
                }

                // Any non-success status should be forwarded as an error message
                if (!response.IsSuccessStatusCode)
                {
                    return (null, await ReadErrorAsync(response));
                }

                // Try to deserialize the response body into the DocumentSummary
                var document = await response.Content.ReadFromJsonAsync<DocumentSummary>();

                return document is null
                    ? (null, "The API returned an empty document.")
                    : (document, null);
            }
            catch (HttpRequestException)
            {
                // Network-level issue communicating with the API
                return (null, "Can't reach the API.");
            }
            catch (Exception ex)
            {
                // Unexpected error - include message for debugging in UI
                return (null, $"Something went wrong: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all documents for a given client.
        /// </summary>
        /// <param name="clientId">Client id to fetch documents for.</param>
        public async Task<(List<DocumentSummary>? Documents, string? Error)> GetForClientAsync(int clientId)
        {
            try
            {
                // Request list of documents belonging to a client
                var response = await _http.GetAsync($"documents/client/{clientId}");

                if (!response.IsSuccessStatusCode)
                {
                    // Return empty list and the API error message
                    return (new List<DocumentSummary>(), await ReadErrorAsync(response));
                }

                var documents = await response.Content.ReadFromJsonAsync<List<DocumentSummary>>();

                return (documents ?? new List<DocumentSummary>(), null);
            }
            catch (HttpRequestException)
            {
                return (null, "Can't reach the server.");
            }
            catch (Exception ex)
            {
                return (null, $"Something went wrong: {ex.Message}");
            }
        }

        /// <summary>
        /// Get documents for a client that are visible to the client.
        /// </summary>
        /// <param name="clientId">Client id.</param>
        public async Task<(List<DocumentSummary>? Documents, string? Error)> GetVisibleToClientAsync(int clientId)
        {
            try
            {
                // Only return documents flagged as visible to the client
                var response = await _http.GetAsync($"documents/client/{clientId}/visible");

                if (!response.IsSuccessStatusCode)
                {
                    return (new List<DocumentSummary>(), await ReadErrorAsync(response));
                }

                var documents = await response.Content.ReadFromJsonAsync<List<DocumentSummary>>();

                return (documents ?? new List<DocumentSummary>(), null);
            }
            catch (HttpRequestException)
            {
                return (null, "Can't reach the server.");
            }
            catch (Exception ex)
            {
                return (null, $"Something went wrong: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all documents associated with a policy.
        /// </summary>
        /// <param name="policyId">Policy id to filter documents by.</param>
        public async Task<(List<DocumentSummary>? Documents, string? Error)> GetForPolicyAsync(int policyId)
        {
            try
            {
                var response = await _http.GetAsync($"documents/policy/{policyId}");

                if (!response.IsSuccessStatusCode)
                {
                    return (new List<DocumentSummary>(), await ReadErrorAsync(response));
                }

                var documents = await response.Content.ReadFromJsonAsync<List<DocumentSummary>>();

                return (documents ?? new List<DocumentSummary>(), null);
            }
            catch (HttpRequestException)
            {
                return (null, "Can't reach the server.");
            }
            catch (Exception ex)
            {
                return (null, $"Something went wrong: {ex.Message}");
            }
        }

        /// <summary>
        /// Get documents filtered by type.
        /// </summary>
        /// <param name="documentType">Document type enum value.</param>
        public async Task<(List<DocumentSummary>? Documents, string? Error)> GetByTypeAsync(DocumentType documentType)
        {
            try
            {
                var response = await _http.GetAsync($"documents/type/{documentType}");

                if (!response.IsSuccessStatusCode)
                {
                    return (new List<DocumentSummary>(), await ReadErrorAsync(response));
                }

                var documents = await response.Content.ReadFromJsonAsync<List<DocumentSummary>>();

                return (documents ?? new List<DocumentSummary>(), null);
            }
            catch (HttpRequestException)
            {
                return (null, "Can't reach the server.");
            }
            catch (Exception ex)
            {
                return (null, $"Something went wrong: {ex.Message}");
            }
        }

        /// <summary>
        /// Upload a document to the API for the specified client and policy.
        /// </summary>
        /// <param name="clientId">Client id to associate the document with.</param>
        /// <param name="policyId">Policy id to associate the document with.</param>
        /// <param name="documentType">Document type enum value.</param>
        /// <param name="uploadedby">Uploader username.</param>
        /// <param name="fileName">Name of the file being uploaded.</param>
        /// <param name="contentType">MIME type of the file.</param>
        /// <param name="fileBytes">Raw file bytes.</param>
        /// <param name="visibleToClient">Whether the client can see the uploaded document.</param>
        public async Task<(DocumentSummary? Document, string? Error)> UploadAsync(int clientId, int policyId, DocumentType documentType, string uploadedby, string fileName, string contentType, byte[] fileBytes, bool visibleToClient = false)
        {
            try
            {
                // Build multipart form data content for file upload
                using var content = new MultipartFormDataContent();

                content.Add(new StringContent(clientId.ToString()), "ClientId");
                content.Add(new StringContent(policyId.ToString()), "PolicyId");
                content.Add(new StringContent(documentType.ToString()), "DocumentType");
                content.Add(new StringContent(visibleToClient.ToString()), "VisibleToClient");
                content.Add(new StringContent(uploadedby), "UploadedBy");

                var fileContent = new ByteArrayContent(fileBytes);
                // Ensure a default content type if none provided
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

                content.Add(fileContent, "File", fileName);

                // Post the multipart content to the documents endpoint
                var response = await _http.PostAsync("documents", content);

                if (!response.IsSuccessStatusCode)
                {
                    return (null, await ReadErrorAsync(response));
                }

                var document = await response.Content.ReadFromJsonAsync<DocumentSummary>();

                return document is null
                    ? (null, "The API returned an empty document.")
                    : (document, null);
            }
            catch (IOException)
            {
                // File read failure
                return (null, "The file could not be read.");
            }
            catch (HttpRequestException)
            {
                // Network error
                return (null, "Can't reach the server.");
            }
            catch (Exception ex)
            {
                return (null, $"Something went wrong: {ex.Message}");
            }
        }

        /// <summary>
        /// Request a temporary download URL for a document.
        /// </summary>
        /// <param name="documentId">Document id to generate the URL for.</param>
        public async Task<(string? Url, string? Error)> GetDownloadUrlAsync(int documentId)
        {
            try
            {
                var response = await _http.GetAsync($"documents/{documentId}/download-url");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return (null, "That document could not be found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return (null, await ReadErrorAsync(response));
                }

                var result = await response.Content.ReadFromJsonAsync<DownloadUrlResponse>();

                return result is null ||
                        string.IsNullOrWhiteSpace(result.Url)
                    ? (null, "The API did not return a download url.")
                    : (result.Url, null);
            }
            catch (HttpRequestException)
            {
                return (null, "Can't reach the server.");
            }
            catch (Exception ex)
            {
                return (null, $"Something went wrong: {ex.Message}");
            }
        }

        /// <summary>
        /// Update the visibility flag for a document.
        /// </summary>
        /// <param name="documentId">Document id to update.</param>
        /// <param name="visibleToClient">New visibility value.</param>
        public async Task<(DocumentSummary? Document, string? Error)> SetVisibilityAsync(int documentId, bool visibleToClient)
        {
            try
            {
                var request = new
                {
                    VisibleToClient = visibleToClient
                };

                var response = await _http.PutAsJsonAsync($"documents/{documentId}/visibility", request);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return (null, "That document could not be found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return (null, await ReadErrorAsync(response));
                }

                var document = await response.Content.ReadFromJsonAsync<DocumentSummary>();

                return document is null
                    ? (null, "The API returned an empty document.")
                    : (document, null);
            }
            catch (HttpRequestException)
            {
                return (null, "Can't reach the server.");
            }
            catch (Exception ex)
            {
                return (null, $"Something went wrong: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete a document by id.
        /// </summary>
        /// <param name="documentId">Id of the document to delete.</param>
        /// <returns>Null on success or an error message.</returns>
        public async Task<string?> DeleteAsync(int documentId)
        {
            try
            {
                // Send DELETE request
                var response = await _http.DeleteAsync($"documents/{documentId}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return "That document could not be found.";
                }

                if (!response.IsSuccessStatusCode)
                {
                    return await ReadErrorAsync(response);
                }

                return null;
            }
            catch (HttpRequestException)
            {
                return "Can't reach the server.";
            }
            catch (Exception ex)
            {
                return $"Something went wrong: {ex.Message}";
            }
        }

        /// <summary>
        /// Read the error response body and return a readable message.
        /// </summary>
        private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(body))
            {
                return $"API returned HTTP {(int)response.StatusCode}: {body}.";
            }

            return $"The server reported an error (status {(int)response.StatusCode}).";
        }

        private sealed class DownloadUrlResponse
        {
            public string Url { get; set; } = string.Empty;
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
