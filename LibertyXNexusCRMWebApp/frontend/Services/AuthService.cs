using System.Data;
using System.Net.Http.Json;

namespace frontend.Services
{
    public record LoginRequest(string Email, string Password);
    public record RegisterRequest(string InvitationToken, string FirstName, string LastName, string Email, string? Phone, string Password);
    public record ApiAuthResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc, string Email, string Role);
    public record CurrentUserResponse(string? UserId, string? Email, string? AdvisorId, string? ClientId, List<string> Roles);
    public record AuthResult(bool Success, string Message, string? Role, int? Id, string? FirstName, string? LastName, string? Email);
    public record ForgotPasswordApiRequest(string Email);
    public record ForgotPasswordApiResponse(bool Exists, string? ResetToken);
    public record ResetPasswordApiRequest(string Email, string ResetToken, string NewPassword);
    public record ForgotPasswordResult(bool Exists, string? ResetToken, string? Error);
    public record ResetPasswordResult(bool Success, string? Error);

    public class AuthService
    {
        private readonly HttpClient _http;
        private readonly TokenStorageService _tokenStorage;
        private readonly CurrentUserService _currentUser;

        public AuthService(HttpClient http, TokenStorageService tokenStorage, CurrentUserService currentUser)
        {
            _http = http;
            _tokenStorage = tokenStorage;
            _currentUser = currentUser;
        }

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("auth/login", new LoginRequest(email, password));

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return new AuthResult(
                                false,
                                "Invalid email or password.",
                                null,
                                null,
                                null,
                                null,
                                null
                            );
                    }

                    var errorMessage = await ReadErrorMessageAsync(response);

                    return new AuthResult(
                            false,
                            errorMessage,
                            null,
                            null,
                            null,
                            null,
                            null
                        );
                }

                var authResponse = await response.Content.ReadFromJsonAsync<ApiAuthResponse>();

                if (authResponse is null || string.IsNullOrWhiteSpace(authResponse.AccessToken))
                {
                    return new AuthResult(
                            false,
                            "The server did not return a valid response.",
                            null,
                            null,
                            null,
                            null,
                            null
                        );
                }

                await _tokenStorage.SetTokenAsync(authResponse.AccessToken);

                var meResponse = await _http.GetAsync("auth/me");

                if (!meResponse.IsSuccessStatusCode)
                {
                    var status = (int)meResponse.StatusCode;

                    var errorBody =
                        await meResponse.Content.ReadAsStringAsync();

                    // Remove the token because authentication has
                    // not been successfully established.
                    await _tokenStorage.ClearTokenAsync();

                    return new AuthResult(
                        false,
                        $"Login succeeded, but /api/auth/me returned HTTP {status}. " +
                        $"Response: {errorBody}",
                        null,
                        null,
                        null,
                        null,
                        null);
                }


                // 6. Read current user information.
                var currentUser =
                    await meResponse.Content
                        .ReadFromJsonAsync<CurrentUserResponse>();

                if (currentUser is null)
                {
                    await _tokenStorage.ClearTokenAsync();

                    return new AuthResult(
                            false,
                            "Login succeded, but user information could not be retrieved.",
                            null,
                            null,
                            null,
                            null,
                            null
                        );
                }

                var role = currentUser.Roles.FirstOrDefault() ?? authResponse.Role;

                int? domainId = null;

                if (int.TryParse(currentUser.ClientId, out var clientId))
                {
                    domainId = clientId;
                }
                else if (int.TryParse(currentUser.AdvisorId, out var advisorId))
                {
                    domainId = advisorId;
                }

                return new AuthResult(
                            true,
                            "Login successful.",
                            role,
                            domainId,
                            null,
                            null,
                            currentUser.Email ?? authResponse.Email
                    );
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
                var request = new RegisterRequest(
                    InvitationToken: token,
                    FirstName: firstName,
                    LastName: lastName,
                    Email: email,
                    Phone: phone,
                    Password: password
                    );

                var response = await _http.PostAsJsonAsync("auth/register", request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await ReadErrorMessageAsync(response);

                    return new AuthResult(
                            false,
                            errorMessage,
                            null,
                            null,
                            null,
                            null,
                            null
                        );
                }

                var authResponse = await response.Content.ReadFromJsonAsync<ApiAuthResponse>();

                if (authResponse is null || string.IsNullOrWhiteSpace(authResponse.AccessToken))
                {
                    return new AuthResult(
                            false,
                            "The server did not return a valid response.",
                            null,
                            null,
                            null,
                            null,
                            null
                        );
                }

                await _tokenStorage.SetTokenAsync(authResponse.AccessToken);

                var currentUser = await GetCurrentUserAsync();

                int? clientId = null;

                if (currentUser is not null && int.TryParse(currentUser.ClientId, out var parsedClientId))
                {
                    clientId = parsedClientId;
                }

                return new AuthResult(
                            true,
                            "Registration successful.",
                            authResponse.Role,
                            clientId,
                            firstName,
                            lastName,
                            authResponse.Email
                    );
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

        private async Task<CurrentUserResponse?> GetCurrentUserAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<CurrentUserResponse>("auth/me");
            }
            catch
            {
                return null;
            }
        }

        public async Task<ForgotPasswordResult> ForgotPasswordAsync(string email)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("auth/forgot-password", new ForgotPasswordApiRequest(email));

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await ReadErrorMessageAsync(response);
                    return new ForgotPasswordResult(false, null, errorMessage);
                }

                var result = await response.Content.ReadFromJsonAsync<ForgotPasswordApiResponse>();

                if (result is null)
                {
                    return new ForgotPasswordResult(false, null, "The server did not return a valid response.");
                }

                return new ForgotPasswordResult(result.Exists, result.ResetToken, null);
            }
            catch (HttpRequestException)
            {
                return new ForgotPasswordResult(false, null, "Can't reach the server. Make sure the API project is running.");
            }
            catch (Exception ex)
            {
                return new ForgotPasswordResult(false, null, $"Something went wrong: {ex.Message}");
            }
        }

        public async Task<ResetPasswordResult> ResetPasswordAsync(string email, string resetToken, string newPassword)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("auth/reset-password", new ResetPasswordApiRequest(email, resetToken, newPassword));

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await ReadErrorMessageAsync(response);
                    return new ResetPasswordResult(false, errorMessage);
                }

                return new ResetPasswordResult(true, null);
            }
            catch (HttpRequestException)
            {
                return new ResetPasswordResult(false, "Can't reach the server. Make sure the API project is running.");
            }
            catch (Exception ex)
            {
                return new ResetPasswordResult(false, $"Something went wrong: {ex.Message}");
            }
        }

        public async Task LogoutAsync()
        {
            await _tokenStorage.ClearTokenAsync();

            await _currentUser.SignOutAsync();
        }

        private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
        {
            try
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

                if (!string.IsNullOrWhiteSpace(error?.Message))
                {
                    return error.Message;
                }
            }
            catch
            {

            }

            return response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => "Invalid email or password.",
                System.Net.HttpStatusCode.Forbidden => "You do not have permission to perform this action.",
                System.Net.HttpStatusCode.BadRequest => "The server rejected the request.",
                _ => $"The server reported an error (status {(int)response.StatusCode}."
            };
        }

        private record ErrorResponse(string? Message);
    }
}
