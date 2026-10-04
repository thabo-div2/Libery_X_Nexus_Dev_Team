using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace frontend.Services
{
    /// <summary>
    /// This service saves the user's login token in the browser's session storage so they stay logged in.
    /// </summary>
    public class TokenStorageService
    {
        private const string StorageKey = "nexus-access-token";
        private readonly ProtectedSessionStorage _storage;
        private string? _accessToken;
        private bool _loaded;

        /// <summary>
        /// Sets up the service with protected session storage.
        /// </summary>
        public TokenStorageService(ProtectedSessionStorage storage)
        {
            _storage = storage;
        }

        /// <summary>
        /// Saves the login token.
        /// </summary>
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

        /// <summary>
        /// Gets the saved login token, or null if there isn't one.
        /// </summary>
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

        /// <summary>
        /// Deletes the saved login token when the user logs out.
        /// </summary>
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

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
