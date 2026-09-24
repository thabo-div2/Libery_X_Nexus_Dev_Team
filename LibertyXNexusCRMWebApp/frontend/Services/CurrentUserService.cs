using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace frontend.Services
{
    public class CurrentUserService
    {
        private const string StorageKey = "nexus-current-user";
        private readonly ProtectedSessionStorage _storage;
        private bool _restoreAttempted;

        public CurrentUserService(ProtectedSessionStorage storage)
        {
            _storage = storage;
        }

        public int? Id { get; private set; }
        public string? Role { get; private set; }
        public string? FirstName { get; private set; }
        public string? LastName { get; private set; }
        public string? Email { get; private set; }

        public bool IsClient => Role == "RegisteredClient";
        public bool IsAdvisor => Role == "FinancialAdviser";

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

        private record StoredUser(int? Id, string? Role, string? FirstName, string? LastName, string? Email);
    }
}
