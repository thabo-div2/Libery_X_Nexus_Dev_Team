using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace frontend.Services
{
    public class TokenStorageService
    {
        private string? _accessToken;

        public Task SetTokenAsync(string token)
        {
            _accessToken = token;

            return Task.CompletedTask;
        }

        public Task<string?> GetTokenAsync()
        {
            return Task.FromResult(_accessToken);
        }

        public Task ClearTokenAsync()
        {
            _accessToken = null;

            return Task.CompletedTask;
        }
    }
}
