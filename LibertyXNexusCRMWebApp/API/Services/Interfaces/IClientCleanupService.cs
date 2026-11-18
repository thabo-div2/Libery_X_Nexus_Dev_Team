namespace API.Services.Interfaces
{
    /// <summary>
    /// What has to be removed outside the database when a client is deleted.
    /// </summary>
    public class ClientCleanupPlan
    {
        /// <summary>The client's stored files.</summary>
        public List<string> BlobReferences { get; set; } = new();

        /// <summary>The client's login, if they registered.</summary>
        public string? IdentityUserId { get; set; }
    }

    /// <summary>
    /// Deleting a client removes their database records, but their uploaded files and their login
    /// live elsewhere. This service removes those too, so nothing personal is left behind.
    /// </summary>
    public interface IClientCleanupService
    {
        /// <summary>Works out what to remove. Call this before the client is deleted, while their records still exist.</summary>
        Task<ClientCleanupPlan> CollectAsync(int clientId);

        /// <summary>Removes files and login before deleting client records; failures must leave references available for retry.</summary>
        Task RunAsync(ClientCleanupPlan plan);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
