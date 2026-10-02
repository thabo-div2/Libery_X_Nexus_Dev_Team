using API.DTOs.Auth;

namespace API.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse?> LoginAsync(LoginRequest request);

        Task<RegisterResult> RegisterAsync(RegisterRequest request);

        Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request);

        Task<ResetPasswordResult> ResetPasswordAsync(ResetPasswordRequest request);
    }
}
