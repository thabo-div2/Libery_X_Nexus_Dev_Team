using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;
using System.Security.Cryptography;
using System.Text.Json;

namespace frontend.Services
{
    /// <summary>
    /// Service for managing the current user's session state, including sign-in, sign-out, and restoring user information from browser storage.
    /// </summary>
    public class CurrentUserService
    {
        private const string StorageKey = "nexus-current-user";
        private readonly ProtectedSessionStorage _storage;
        private readonly ProtectedLocalStorage _localStorage;
        private readonly TokenStorageService _tokens;
        private bool _restoreAttempted;
        private readonly SemaphoreSlim _restoreLock = new(1, 1);

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentUserService"/> class with the specified <see cref="ProtectedSessionStorage"/> for managing user session data.
        /// </summary>
        /// <param name="storage"></param>
        public CurrentUserService(ProtectedSessionStorage storage, ProtectedLocalStorage localStorage, TokenStorageService tokens)
        {
            _storage = storage;
            _localStorage = localStorage;
            _tokens = tokens;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the current user's ID, or null if the user is not signed in.
        /// </summary>
        public int? Id { get; private set; }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Get the current user's Role, or null if the user is not signed in.
        /// </summary>
        public string? Role { get; private set; }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the current user's First Name, or null if the user is not signed in.
        /// </summary>
        public string? FirstName { get; private set; }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the current user's Last Name, or null if the user is not signed in.
        /// </summary>
        public string? LastName { get; private set; }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the current user's Email, or null if the user is not signed in.
        /// </summary>
        public string? Email { get; private set; }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Check if the current user is a client
        /// </summary>
        public bool IsClient => Role == "Client";

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Check if the current user is a advisor
        /// </summary>
        public bool IsAdvisor => Role == "Advisor";

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Signs in the current user by copying values from the provided <see cref="AuthResult"/> into the service state
        /// and attempting to persist them to browser storage.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public async Task SignInAsync(AuthResult result, bool rememberMe = false)
        {
            Id = result.Id;
            Role = result.Role;
            FirstName = result.FirstName;
            LastName = result.LastName;
            Email = result.Email;
            _restoreAttempted = true;

            try
            {
                var user = new StoredUser(Id, Role, FirstName, LastName, Email);

                // "Keep me signed in" keeps the user in local storage so it survives closing the browser.
                if (rememberMe)
                {
                    await _localStorage.SetAsync(StorageKey, user);
                    await _storage.DeleteAsync(StorageKey);
                }
                else
                {
                    await _storage.SetAsync(StorageKey, user);
                    await _localStorage.DeleteAsync(StorageKey);
                }
            }
            catch
            {
                // Browser storage isn't available; the session just won't survive a reload.
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Signs the current user out
        /// </summary>
        /// <returns></returns>
        public async Task SignOutAsync()
        {
            Id = null;
            Role = null;
            FirstName = null;
            LastName = null;
            Email = null;
            _restoreAttempted = true;

            try
            {
                await _storage.DeleteAsync(StorageKey);
                await _localStorage.DeleteAsync(StorageKey);
            }
            catch
            {
                // Nothing to clean up if storage isn't available.
            }
        }
        
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// To keep the user logged in
        /// </summary>
        /// <returns></returns>
        public async Task EnsureRestoredAsync()
        {
            // Navigation and sidebar counters can restore the same session simultaneously.
            await _restoreLock.WaitAsync();
            try
            {
                await RestoreCoreAsync();
            }
            finally
            {
                _restoreLock.Release();
            }
        }

        private async Task RestoreCoreAsync()
        {
            if (Id is not null)
            {
                // Already signed in: make sure the login hasn't run out. If it has, sign out
                // so the layout sends the user back to the login page instead of showing API errors.
                if (await _tokens.GetTokenAsync() is null)
                {
                    await SignOutAsync();
                }

                return;
            }

            if (_restoreAttempted)
            {
                return;
            }

            _restoreAttempted = true;

            try
            {
                // This tab's login first, then a "Keep me signed in" one.
                var result = await _storage.GetAsync<StoredUser>(StorageKey);
                if (!result.Success || result.Value is null)
                {
                    result = await _localStorage.GetAsync<StoredUser>(StorageKey);
                }

                if (result.Success && result.Value is not null)
                {
                    // Only restore the user if their login token is still valid.
                    if (await _tokens.GetTokenAsync() is null)
                    {
                        await SignOutAsync();
                        return;
                    }

                    Id = result.Value.Id;
                    Role = result.Value.Role;
                    FirstName = result.Value.FirstName;
                    LastName = result.Value.LastName;
                    Email = result.Value.Email;
                }

                // Mark restoration complete only after storage was accessible.
                _restoreAttempted = true;
            }
            catch (Exception ex) when (ex is CryptographicException or JsonException)
            {
                // A stale/corrupt saved session must not terminate the interactive circuit.
                await _tokens.ClearTokenAsync();
                await SignOutAsync();
            }
            catch (Exception ex) when (ex is InvalidOperationException or JSException or OperationCanceledException)
            {
                // A reconnect or initial render may temporarily make browser storage unavailable.
                _restoreAttempted = false;
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// DTO
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Role"></param>
        /// <param name="FirstName"></param>
        /// <param name="LastName"></param>
        /// <param name="Email"></param>
        private record StoredUser(int? Id, string? Role, string? FirstName, string? LastName, string? Email);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
