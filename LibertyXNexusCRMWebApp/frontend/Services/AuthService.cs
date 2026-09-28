using System.Net.Http.Json;

namespace frontend.Services
{
    public record LoginRequest(string Email, string Password);
    public record RegisterRequest(string FirstName, string LastName, string Email, string? Phone, string? IdentityNumber, string Password, string Token);
    public record AuthResult(bool Success, string Message, string? Role, int? Id, string? FirstName, string? LastName, string? Email);

    public class AuthService
    {
        private readonly HttpClient _http;

        public AuthService(HttpClient http)
        {
            _http = http;
        }

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("Auth/login", new LoginRequest(email, password));
                return await ReadResultAsync(response);
            }
            catch (HttpRequestException)
            {
                return new AuthResult(false, "Can't reach the server. Make sure the Backend project is running.", null, null, null, null, null);
            }
            catch (Exception ex)
            {
                return new AuthResult(false, $"Something went wrong: {ex.Message}", null, null, null, null, null);
            }
        }

        public async Task<AuthResult> RegisterAsync(string firstName, string lastName, string email, string? phone, string? identityNumber, string password, string token)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("Auth/register", new RegisterRequest(firstName, lastName, email, phone, identityNumber, password, token));
                return await ReadResultAsync(response);
            }
            catch (HttpRequestException)
            {
                return new AuthResult(false, "Can't reach the server. Make sure the Backend project is running.", null, null, null, null, null);
            }
            catch (Exception ex)
            {
                return new AuthResult(false, $"Something went wrong: {ex.Message}", null, null, null, null, null);
            }
        }

        private static async Task<AuthResult> ReadResultAsync(HttpResponseMessage response)
        {
            var result = await response.Content.ReadFromJsonAsync<AuthResult>();
            return result ?? new AuthResult(false, "The server sent back an unexpected response.", null, null, null, null, null);
        }
    }
}
