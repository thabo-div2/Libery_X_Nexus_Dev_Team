using Microsoft.AspNetCore.Components.Forms;
using Shared.Models.Enums;
using System.Net.Http.Headers;

namespace frontend.Services
{
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

    public class DocumentService
    {
        private readonly HttpClient _http;

        public DocumentService(HttpClient http)
        {
            _http = http;
        }

        public async Task<(DocumentSummary? Document, string? Error)> GetByIdAsync(int documentId)
        {
            try
            {
                var response = await _http.GetAsync($"documents/{documentId}");

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
                return (null, "Can't reach the API.");
            }
            catch (Exception ex)
            {
                return (null, $"Something went wrong: {ex.Message}");
            }
        }

        public async Task<(List<DocumentSummary>? Documents, string? Error)> GetForClientAsync(int clientId)
        {
            try
            {
                var response = await _http.GetAsync($"documents/client/{clientId}");

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

        public async Task<(List<DocumentSummary>? Documents, string? Error)> GetVisibleToClientAsync(int clientId)
        {
            try
            {
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

        public async Task<(DocumentSummary? Document, string? Error)> UploadAsync(int clientId, int policyId, DocumentType documentType, string uploadedby, IBrowserFile file, bool visibleToClient = false)
        {
            try
            {
                const long maxFileSize = 30_000_000;

                await using var stream = file.OpenReadStream(maxFileSize);

                using var content = new MultipartFormDataContent();

                content.Add(
                    new StringContent(clientId.ToString()),
                    "ClientId");

                content.Add(
                    new StringContent(policyId.ToString()),
                    "PolicyId");

                content.Add(
                    new StringContent(documentType.ToString()),
                    "DocumentType");

                content.Add(
                    new StringContent(visibleToClient.ToString()),
                    "VisibleToClient");

                content.Add(
                    new StringContent(uploadedby),
                    "UploadedBy");

                var fileContent = new StreamContent(stream);

                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                        string.IsNullOrWhiteSpace(file.ContentType) 
                            ? "application/octet-stream"
                            : file.ContentType
                    );

                content.Add(
                    fileContent,
                    "File",
                    file.Name
                    );

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
                return (null, "The file could not be read.");
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

        public async Task<(DocumentSummary? Document, string? Error)> SetVisibilityAsync(int documentId, bool visibleToClient)
        {
            try
            {
                var request = new { 
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

        public async Task<string?> DeleteAsync(int documentId)
        {
            try
            {

                var response = await _http.DeleteAsync($"documents/{documentId}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return "That document could not be found.";
                }

                if (!response.IsSuccessStatusCode)
                {
                    return await ReadErrorAsync(response);
                }

                var document = await response.Content.ReadFromJsonAsync<DocumentSummary>();

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
