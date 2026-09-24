using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace frontend.Services
{
    public record CreateInvitationRequest(string Email);
    public record InvitationResult(bool Success, string Message, string? Token, DateTime? ExpiresAt);
    public record InvitationDetails(bool Valid, string Message, string Email, int AdvisorId, string AdvisorName);

    public class InvitationService
    {
        private readonly HttpClient _http;
        private readonly NavigationManager _navigation;

        public InvitationService(HttpClient http, NavigationManager navigation)
        {
            _http = http;
            _navigation = navigation;
        }

        public async Task<InvitationResult> CreateAsync(string email)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("Invitation/create", new CreateInvitationRequest(email));
                var result = await response.Content.ReadFromJsonAsync<InvitationResult>();
                return result ?? new InvitationResult(false, "The server sent back an unexpected response.", null, null);
            }
            catch (HttpRequestException)
            {
                return new InvitationResult(false, "Can't reach the server. Make sure the Backend project is running.", null, null);
            }
            catch (Exception ex)
            {
                return new InvitationResult(false, $"Something went wrong: {ex.Message}", null, null);
            }
        }

        public async Task<InvitationDetails> ValidateAsync(string token)
        {
            try
            {
                var response = await _http.GetAsync($"Invitation/validate/{token}");
                var result = await response.Content.ReadFromJsonAsync<InvitationDetails>();
                return result ?? new InvitationDetails(false, "The server sent back an unexpected response.", string.Empty, 0, string.Empty);
            }
            catch (HttpRequestException)
            {
                return new InvitationDetails(false, "Can't reach the server. Make sure the Backend project is running.", string.Empty, 0, string.Empty);
            }
            catch (Exception ex)
            {
                return new InvitationDetails(false, $"Something went wrong: {ex.Message}", string.Empty, 0, string.Empty);
            }
        }

        public string BuildLink(string token)
        {
            return $"{_navigation.BaseUri}register?token={token}";
        }
    }
}
