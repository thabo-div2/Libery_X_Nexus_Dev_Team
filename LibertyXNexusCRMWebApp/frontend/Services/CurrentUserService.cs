using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace frontend.Services
{
    /// <summary>
    /// Service for managing the current user's session state, including sign-in, sign-out, and restoring user information from browser storage.
    /// </summary>
    public class CurrentUserService
    {
        private const string StorageKey = "nexus-current-user";
        private readonly ProtectedSessionStorage _storage;
        private bool _restoreAttempted;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentUserService"/> class with the specified <see cref="ProtectedSessionStorage"/> for managing user session data.
        /// </summary>
        /// <param name="storage"></param>
        public CurrentUserService(ProtectedSessionStorage storage)
        {
            _storage = storage;
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
        public async Task SignInAsync(AuthResult result)
        {
            Id = result.Id;
            Role = result.Role;
            FirstName = result.FirstName;
            LastName = result.LastName;
            Email = result.Email;
            _restoreAttempted = true;

            try
            {
                await _storage.SetAsync(StorageKey, new StoredUser(Id, Role, FirstName, LastName, Email));
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
            if (_restoreAttempted || Id is not null)
            {
                return;
            }

            _restoreAttempted = true;

            try
            {
                var result = await _storage.GetAsync<StoredUser>(StorageKey);
                if (result.Success && result.Value is not null)
                {
                    Id = result.Value.Id;
                    Role = result.Value.Role;
                    FirstName = result.Value.FirstName;
                    LastName = result.Value.LastName;
                    Email = result.Value.Email;
                }
            }
            catch
            {
                // Browser storage isn't available; the user will just need to log in again.
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
