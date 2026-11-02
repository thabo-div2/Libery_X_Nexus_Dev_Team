using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace frontend.Services
{
    /// <summary>
    /// Keeps the API login token for this browser.
    /// Normally it lives in session storage, so it's gone when the browser closes.
    /// With "Keep me signed in" it lives in local storage, so it survives a restart until it expires.
    /// An expired token is thrown away, so the user is sent back to log in instead of getting errors.
    /// </summary>
    public class TokenStorageService
    {
        private const string StorageKey = "nexus-access-token";

        // Treat a token as expired a minute early so a request doesn't fail half way through.
        private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromMinutes(1);

        private readonly ProtectedSessionStorage _session;
        private readonly ProtectedLocalStorage _local;
        private string? _accessToken;
        private DateTime? _expiresAtUtc;
        private bool _loaded;

        public TokenStorageService(ProtectedSessionStorage session, ProtectedLocalStorage local)
        {
            _session = session;
            _local = local;
        }

        /// <summary>
        /// True when the current login was made with "Keep me signed in".
        /// </summary>
        public bool RememberMe { get; private set; }

        public async Task SetTokenAsync(string token, DateTime? expiresAtUtc = null, bool rememberMe = false)
        {
            _accessToken = token;
            _expiresAtUtc = expiresAtUtc == default(DateTime) ? null : expiresAtUtc;
            RememberMe = rememberMe;
            _loaded = true;

            var stored = new StoredToken(token, _expiresAtUtc);

            try
            {
                if (rememberMe)
                {
                    await _local.SetAsync(StorageKey, stored);
                    await _session.DeleteAsync(StorageKey);
                }
                else
                {
                    await _session.SetAsync(StorageKey, stored);
                    await _local.DeleteAsync(StorageKey);
                }
            }
            catch
            {
                // Storage may not be available during prerendering.
            }
        }

        public async Task<string?> GetTokenAsync()
        {
            if (!_loaded)
            {
                _loaded = true;

                // This tab's login first, then a remembered one.
                var stored = await ReadAsync(_session);
                RememberMe = false;

                if (stored is null)
                {
                    stored = await ReadAsync(_local);
                    RememberMe = stored is not null;
                }

                _accessToken = stored?.Token;
                _expiresAtUtc = stored?.ExpiresAtUtc;
            }

            if (_accessToken is not null && IsExpired)
            {
                await ClearTokenAsync();
            }

            return _accessToken;
        }

        public async Task ClearTokenAsync()
        {
            _accessToken = null;
            _expiresAtUtc = null;
            RememberMe = false;
            _loaded = true;

            try
            {
                await _session.DeleteAsync(StorageKey);
                await _local.DeleteAsync(StorageKey);
            }
            catch
            {
                // Nothing to clean up if storage isn't available.
            }
        }

        private bool IsExpired => _expiresAtUtc is DateTime expires && DateTime.UtcNow >= expires - ExpiryBuffer;

        private static async Task<StoredToken?> ReadAsync(ProtectedBrowserStorage storage)
        {
            try
            {
                var result = await storage.GetAsync<StoredToken>(StorageKey);
                return result.Success && !string.IsNullOrWhiteSpace(result.Value?.Token) ? result.Value : null;
            }
            catch
            {
                // Storage not available yet, or an old value saved in a different shape.
                return null;
            }
        }

        private record StoredToken(string Token, DateTime? ExpiresAtUtc);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
