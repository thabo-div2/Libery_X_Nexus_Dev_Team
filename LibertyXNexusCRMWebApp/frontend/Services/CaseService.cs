using Shared.Models.Enums;
using System.Net;

namespace frontend.Services
{
    public record CaseSummary(
            int CaseId,
            int PolicyId,
            string? PolicyName,
            string Status,
            string? Notes,
            DateTime CreatedAt,
            DateTime? UpdatedAt,
            DateTime? DetailsSubmittedAt,
            DateTime? AdviserReviewAt,
            DateTime? FicaVerifiedAt,
            DateTime? SubmittedToLibertyAt,
            DateTime? PolicyIssuedAt
        );

    public class CaseService
    {
        private readonly HttpClient _http;

        public CaseService(HttpClient http)
        {
            _http = http;
        }

        public async Task<(List<CaseSummary> Cases, string? Error)>
            GetForClientAsync(int clientId)
        {
            try
            {
                var response = await _http.GetAsync($"Cases/client/{clientId}");

                if (!response.IsSuccessStatusCode)
                {
                    return (new List<CaseSummary>(),await ReadErrorAsync(response));
                }

                var cases =await response.Content.ReadFromJsonAsync<List<CaseSummary>>();

                return (cases ?? new List<CaseSummary>(),null);
            }
            catch (HttpRequestException)
            {
                return (new List<CaseSummary>(), "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (new List<CaseSummary>(), $"Something went wrong: {ex.Message}");
            }
        }

        public async Task<(CaseSummary? Case, string? Error)>
            MarkStepCompleteAsync(int caseId, CaseStep step)
        {
            try
            {
                var response =await _http.PutAsJsonAsync($"Cases/{caseId}/steps/{step}",new {});

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return (null,"That case could not be found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return (null,await ReadErrorAsync(response));
                }
                var updated = await response.Content.ReadFromJsonAsync<CaseSummary>();
                return updated is null ? (null, "The API returned an empty case."): (updated, null);
            }
            catch (HttpRequestException)
            {
               return ( null, "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (null, $"Something went wrong: {ex.Message}");
            }
        }

        private static async Task<string> ReadErrorAsync(
            HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(body))
            {
                return $"API returned HTTP {(int)response.StatusCode}: {body}";
            }

            return $"The server reported an error (status {(int)response.StatusCode}).";
        }
    
    }
}
