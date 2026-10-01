using Shared.Models.Enums;
using System.Net;

namespace frontend.Services
{
    public record PolicySummary(
            int PolicyId,
            int? ClientId,
            string PolicyName,
            string Provider,
            string? Description,
            string Status,
            double? PremiumAmount,
            double? CoverAmount,
            DateTime? StartDate,
            DateTime? EndDate,
            bool IsCatalogueItem,
            DateTime CreatedAt,
            DateTime? UpdatedAt
        );

    public record CreateCataloguePolicyRequest(
            string PolicyName,
            string Provider,
            string? Description,
            double? PremiumAmount,
            double? CoverAmount
        );

    public record CreateClientPolicyRequest(
            string PolicyName,
            string Provider,
            string? Description,
            double? PremiumAmount,
            double? CoverAmount,
            DateTime? StartDate,
            DateTime? EndDate
        );

    public record UpdatePolicyRequest(
            string PolicyName,
            string Provider,
            string? Description,
            double? PremiumAmount,
            double? CoverAmount,
            DateTime? StartDate,
            DateTime? EndDate
        );

    public class PolicyService
    {
        private readonly HttpClient _http;

        public PolicyService(HttpClient http)
        {
            _http = http;
        }

        public async Task<(PolicySummary? Policy, string? Error)>
            GetByIdAsync(int policyId)
        {
            try
            {
                var response =
                    await _http.GetAsync($"Policies/{policyId}");

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return (
                        null,
                        "That policy could not be found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return (
                        null,
                        await ReadErrorAsync(response));
                }

                var policy =
                    await response.Content
                        .ReadFromJsonAsync<PolicySummary>();

                return policy is null
                    ? (null, "The API returned an empty policy.")
                    : (policy, null);
            }
            catch (HttpRequestException)
            {
                return (
                    null,
                    "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (
                    null,
                    $"Something went wrong: {ex.Message}");
            }
        }

        public async Task<(PolicySummary? Policy, string? Error)>
            GetWithDocumentsAsync(int policyId)
        {
            try
            {
                var response =
                    await _http.GetAsync(
                        $"Policies/{policyId}/with-documents");

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return (
                        null,
                        "That policy could not be found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return (
                        null,
                        await ReadErrorAsync(response));
                }

                var policy =
                    await response.Content
                        .ReadFromJsonAsync<PolicySummary>();

                return policy is null
                    ? (null, "The API returned an empty policy.")
                    : (policy, null);
            }
            catch (HttpRequestException)
            {
                return (
                    null,
                    "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (
                    null,
                    $"Something went wrong: {ex.Message}");
            }
        }

        public async Task<(List<PolicySummary> Policies, string? Error)>
            GetCatalogueAsync()
        {
            try
            {
                var response =
                    await _http.GetAsync("Policies/catalogue");

                if (!response.IsSuccessStatusCode)
                {
                    return (
                        new List<PolicySummary>(),
                        await ReadErrorAsync(response));
                }

                var policies =
                    await response.Content
                        .ReadFromJsonAsync<List<PolicySummary>>();

                return (
                    policies ?? new List<PolicySummary>(),
                    null);
            }
            catch (HttpRequestException)
            {
                return (
                    new List<PolicySummary>(),
                    "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (
                    new List<PolicySummary>(),
                    $"Something went wrong: {ex.Message}");
            }
        }

        public async Task<(List<PolicySummary> Policies, string? Error)>
            GetForClientAsync(int clientId)
        {
            try
            {
                var response =
                    await _http.GetAsync(
                        $"Policies/client/{clientId}");

                if (!response.IsSuccessStatusCode)
                {
                    return (
                        new List<PolicySummary>(),
                        await ReadErrorAsync(response));
                }

                var policies =
                    await response.Content
                        .ReadFromJsonAsync<List<PolicySummary>>();

                return (
                    policies ?? new List<PolicySummary>(),
                    null);
            }
            catch (HttpRequestException)
            {
                return (
                    new List<PolicySummary>(),
                    "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (
                    new List<PolicySummary>(),
                    $"Something went wrong: {ex.Message}");
            }
        }

        public async Task<(List<PolicySummary> Policies, string? Error)>
            GetByStatusAsync(PolicyStatus status)
        {
            try
            {
                var response =
                    await _http.GetAsync(
                        $"Policies/status/{status}");

                if (!response.IsSuccessStatusCode)
                {
                    return (
                        new List<PolicySummary>(),
                        await ReadErrorAsync(response));
                }

                var policies =
                    await response.Content
                        .ReadFromJsonAsync<List<PolicySummary>>();

                return (
                    policies ?? new List<PolicySummary>(),
                    null);
            }
            catch (HttpRequestException)
            {
                return (
                    new List<PolicySummary>(),
                    "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (
                    new List<PolicySummary>(),
                    $"Something went wrong: {ex.Message}");
            }
        }

        public async Task<(PolicySummary? Policy, string? Error)>
            CreateCatalogueItemAsync(
                CreateCataloguePolicyRequest request)
        {
            try
            {
                var response =
                    await _http.PostAsJsonAsync(
                        "Policies/catalogue",
                        request);

                if (!response.IsSuccessStatusCode)
                {
                    return (
                        null,
                        await ReadErrorAsync(response));
                }

                var policy =
                    await response.Content
                        .ReadFromJsonAsync<PolicySummary>();

                return policy is null
                    ? (null, "The API returned an empty policy.")
                    : (policy, null);
            }
            catch (HttpRequestException)
            {
                return (
                    null,
                    "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (
                    null,
                    $"Something went wrong: {ex.Message}");
            }
        }

        public async Task<(PolicySummary? Policy, string? Error)>
            CreateClientPolicyAsync(
                int clientId,
                CreateClientPolicyRequest request)
        {
            try
            {
                var response =
                    await _http.PostAsJsonAsync(
                        $"Policies/client/{clientId}",
                        request);

                if (!response.IsSuccessStatusCode)
                {
                    return (
                        null,
                        await ReadErrorAsync(response));
                }

                var policy =
                    await response.Content
                        .ReadFromJsonAsync<PolicySummary>();

                return policy is null
                    ? (null, "The API returned an empty policy.")
                    : (policy, null);
            }
            catch (HttpRequestException)
            {
                return (
                    null,
                    "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (
                    null,
                    $"Something went wrong: {ex.Message}");
            }
        }

        public async Task<(PolicySummary? Policy, string? Error)>
            UpdateAsync(
                int policyId,
                UpdatePolicyRequest request)
        {
            try
            {
                var response =
                    await _http.PutAsJsonAsync(
                        $"Policies/{policyId}",
                        request);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return (
                        null,
                        "That policy could not be found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return (
                        null,
                        await ReadErrorAsync(response));
                }

                var policy =
                    await response.Content
                        .ReadFromJsonAsync<PolicySummary>();

                return policy is null
                    ? (null, "The API returned an empty policy.")
                    : (policy, null);
            }
            catch (HttpRequestException)
            {
                return (
                    null,
                    "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (
                    null,
                    $"Something went wrong: {ex.Message}");
            }
        }

        public async Task<(PolicySummary? Policy, string? Error)>
            UpdateStatusAsync(
                int policyId,
                PolicyStatus newStatus)
        {
            try
            {
                var request = new
                {
                    NewStatus = newStatus
                };

                var response =
                    await _http.PutAsJsonAsync(
                        $"Policies/{policyId}/status",
                        request);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return (
                        null,
                        "That policy could not be found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return (
                        null,
                        await ReadErrorAsync(response));
                }

                var policy =
                    await response.Content
                        .ReadFromJsonAsync<PolicySummary>();

                return policy is null
                    ? (null, "The API returned an empty policy.")
                    : (policy, null);
            }
            catch (HttpRequestException)
            {
                return (
                    null,
                    "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (
                    null,
                    $"Something went wrong: {ex.Message}");
            }
        }

        public async Task<string?> DeleteAsync(int policyId)
        {
            try
            {
                var response =
                    await _http.DeleteAsync(
                        $"Policies/{policyId}");

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return "That policy could not be found.";
                }

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

        private static async Task<string> ReadErrorAsync(
            HttpResponseMessage response)
        {
            var body =
                await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(body))
            {
                return
                    $"API returned HTTP {(int)response.StatusCode}: {body}";
            }

            return
                $"The server reported an error " +
                $"(status {(int)response.StatusCode}).";
        }
    }
}
