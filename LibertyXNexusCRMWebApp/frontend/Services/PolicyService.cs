using Shared.Models.Enums;
using System.Net;

namespace frontend.Services
{
    /// <summary>
    /// The info about a policy that we show on the frontend.
    /// </summary>
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

    /// <summary>
    /// What we send to the API to add a new policy to the catalogue.
    /// </summary>
    public record CreateCataloguePolicyRequest(
            string PolicyName,
            string Provider,
            string? Description,
            double? PremiumAmount,
            double? CoverAmount
        );

    /// <summary>
    /// What we send to the API to give a client a policy.
    /// </summary>
    public record CreateClientPolicyRequest(
            string PolicyName,
            string Provider,
            string? Description,
            double? PremiumAmount,
            double? CoverAmount,
            DateTime? StartDate,
            DateTime? EndDate,
            DateTime? TargetSubmissionDate = null
        );

    /// <summary>
    /// What we send to the API to update a policy.
    /// </summary>
    public record UpdatePolicyRequest(
            string PolicyName,
            string Provider,
            string? Description,
            double? PremiumAmount,
            double? CoverAmount,
            DateTime? StartDate,
            DateTime? EndDate
        );

    /// <summary>
    /// This service handles everything to do with policies, like getting, adding, updating and deleting them.
    /// </summary>
    public class PolicyService
    {
        private readonly HttpClient _http;

        /// <summary>
        /// Sets up the service with the HttpClient.
        /// </summary>
        public PolicyService(HttpClient http)
        {
            _http = http;
        }

        /// <summary>
        /// Gets one policy using its id.
        /// </summary>
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

        /// <summary>
        /// Gets a policy along with its documents.
        /// </summary>
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

        /// <summary>
        /// Gets all the policies in the catalogue.
        /// </summary>
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

        /// <summary>
        /// Gets all the policies a client has.
        /// </summary>
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

        /// <summary>
        /// Gets all the policies with a certain status.
        /// </summary>
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

        /// <summary>
        /// Adds a new policy to the catalogue.
        /// </summary>
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

        /// <summary>
        /// Gives a client a new policy.
        /// </summary>
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

        /// <summary>
        /// Updates a policy's details.
        /// </summary>
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

        /// <summary>
        /// Changes a policy's status.
        /// </summary>
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

        /// <summary>
        /// Deletes a policy. Returns an error message if it fails, or null if it worked.
        /// </summary>
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

        /// <summary>
        /// Reads the error message the API sent back.
        /// </summary>
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

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
