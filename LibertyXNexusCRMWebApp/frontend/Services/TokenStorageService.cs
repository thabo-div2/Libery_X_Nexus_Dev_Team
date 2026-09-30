using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace frontend.Services
{
    public class TokenStorageService
    {
        private const string StorageKey = "nexus-access-token";
        private readonly ProtectedSessionStorage _storage;
        private string? _accessToken;
        private bool _loaded;

        public TokenStorageService(ProtectedSessionStorage storage)
        {
            _storage = storage;
        }

        public async Task SetTokenAsync(string token)
        {
            _accessToken = token;
            _loaded = true;

            try 
            { 
                await _storage.SetAsync(StorageKey, token); 
            }
            catch 
            {
                // Storage may not be available during prerendering.
            }
        }

        public async Task<string?> GetTokenAsync()
        {
            if (_loaded)
                return _accessToken;

            _loaded = true;

            try
            {
                var result = await _storage.GetAsync<string>(StorageKey);
                if (result.Success)
                    _accessToken = result.Value;
            }
            catch
            {
                // Storage may not be available during prerendering.
            }

            return _accessToken;
        }

        public async Task ClearTokenAsync()
        {
            _accessToken = null;
            _loaded = true;

            try
            {
                await _storage.DeleteAsync(StorageKey);
            }
            catch
            {
                // Nothing to clean up if storage isn't available.
            }
        }
    }
}
