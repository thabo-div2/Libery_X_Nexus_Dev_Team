namespace Shared.DTOs.Auth
{
    public class RegisterResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public AuthResponse? Response { get; init; }
    }
}
