using Shared.Models.Enums;
using System.Net;
using System.Net.Http.Json;

namespace frontend.Services
{
    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
    /// <summary>
    /// DTO representing a client profile, including personal, financial, and compliance information.
    /// </summary>
    public sealed class ClientProfile
    {
        public int ClientId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? IdentificationNumber { get; set; }
        public string? RiskProfile { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int? AdvisorId { get; set; }
        public string? AdvisorName { get; set; }

        public DateTime? DateOfBirth { get; set; }
        public string? ResidentialAddress { get; set; }
        public string? MaritalStatus { get; set; }
        public int? Dependants { get; set; }

        public string? EmploymentStatus { get; set; }
        public string? Occupation { get; set; }
        public string? Employer { get; set; }
        public decimal? GrossMonthlyIncome { get; set; }
        public decimal? NetMonthlyIncome { get; set; }
        public decimal? MonthlyExpenses { get; set; }
        public string? SourceOfFunds { get; set; }
        public string? TaxNumber { get; set; }
        public decimal? PropertyValue { get; set; }
        public decimal? ExistingInvestments { get; set; }
        public decimal? RetirementSavings { get; set; }
        public decimal? OutstandingDebt { get; set; }
        public string? PrimaryGoal { get; set; }
        public int? InvestmentHorizonYears { get; set; }
        public bool PopiaConsent { get; set; }

        public string IdentityNumber => IdentificationNumber ?? string.Empty;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Normalizes the client's name by extracting first and last names from the full name if necessary.
        /// </summary>
        public void NormalizeName()
        {
            if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName) && !string.IsNullOrWhiteSpace(FullName))
            {
                var parts = FullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                FirstName = parts.ElementAtOrDefault(0) ?? string.Empty;
                LastName = parts.ElementAtOrDefault(1) ?? string.Empty;
            }
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
    /// <summary>
    /// DTO containing personal, financial, and compliance data required to register a client.
    /// </summary>
    public sealed class CreateClientRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? IdentificationNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? ResidentialAddress { get; set; }
        public string? MaritalStatus { get; set; }
        public int? Dependants { get; set; }

        public string? EmploymentStatus { get; set; }
        public string? Occupation { get; set; }
        public string? Employer { get; set; }
        public decimal? GrossMonthlyIncome { get; set; }
        public decimal? NetMonthlyIncome { get; set; }
        public decimal? MonthlyExpenses { get; set; }
        public string? SourceOfFunds { get; set; }
        public string? TaxNumber { get; set; }
        public decimal? PropertyValue { get; set; }
        public decimal? ExistingInvestments { get; set; }
        public decimal? RetirementSavings { get; set; }
        public decimal? OutstandingDebt { get; set; }
        public string? PrimaryGoal { get; set; }
        public int? InvestmentHorizonYears { get; set; }

        public string RiskProfile { get; set; } = string.Empty;
        public bool PopiaConsent { get; set; }
    }


    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
    /// <summary>
    /// Service class for managing client profiles, including searching, creating, and retrieving client data from the backend API.
    /// </summary>
    public class ClientService
    {
        private readonly HttpClient _http;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the ClientService class with the specified HttpClient.
        /// </summary>
        /// <param name="http"></param>
        public ClientService(HttpClient http)
        {
            _http = http;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Searches for client profiles by name or email. If the search term is null or empty, all clients are returned.
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public async Task<(List<ClientProfile> Clients, string? Error)> SearchAsync(string? search, ClientStatus? status)
        {
            try
            {
                var queryParams = new List<string>();

                if (!string.IsNullOrWhiteSpace(search)) 
                {
                    queryParams.Add($"searchTerm={Uri.EscapeDataString(search.Trim())}");
                }

                if (status.HasValue)
                {
                    queryParams.Add($"status={Uri.EscapeDataString(status.Value.ToString())}");
                }

                var url = queryParams.Count > 0 ? $"clients?{string.Join("&", queryParams)}" : "clients";

                var response = await _http.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return (new List<ClientProfile>(), $"The server reported an error (status {(int)response.StatusCode}).");
                }

                var result = await response.Content.ReadFromJsonAsync<List<ClientProfile>>() ?? new List<ClientProfile>();
                
                foreach (var client in result)
                    client.NormalizeName();
                
                return (result, null);
            }
            catch (HttpRequestException)
            {
                return (new List<ClientProfile>(), "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (new List<ClientProfile>(), $"Something went wrong: {ex.Message}");
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Creates a new client profile.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<(ClientProfile? Client, string? Error)> CreateAsync(CreateClientRequest request)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("clients", request);

                if (!response.IsSuccessStatusCode)
                {
                    return (null, await ReadErrorAsync(response));
                }

                var result = await response.Content.ReadFromJsonAsync<ClientProfile>();
                result?.NormalizeName();

                return (result, result is null ? "The server sent back an unexpected response." : null);
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
        /// Fetches a client profile by its ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<(ClientProfile? Client, string? Error)> GetByIdAsync(int id)
        {
            try
            {
                var response = await _http.GetAsync($"clients/{id}");

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return (null, "That client could not be found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    var error = await ReadErrorAsync(response);
                    return (null, error);
                }

                var result = await response.Content.ReadFromJsonAsync<ClientProfile>();
                result?.NormalizeName();

                return (result, result is null ? "The server sent back an unexpected response." : null);
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
        /// Reads the error message from an HTTP response and formats it for display.
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

            return $"The server reported an error (status {(int)response.StatusCode}.";
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
