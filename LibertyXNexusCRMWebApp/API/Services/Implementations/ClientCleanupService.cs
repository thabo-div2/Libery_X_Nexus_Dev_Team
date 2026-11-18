using API.Data;
using API.Identity;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Implementations
{
    /// <summary>
    /// Removes a deleted client's stored files and login.
    /// </summary>
    public class ClientCleanupService : IClientCleanupService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IBlobStorageService _blobStorageService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ClientCleanupService> _logger;

        public ClientCleanupService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IBlobStorageService blobStorageService,
            UserManager<ApplicationUser> userManager,
            ILogger<ClientCleanupService> logger)
        {
            _contextFactory = contextFactory;
            _blobStorageService = blobStorageService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<ClientCleanupPlan> CollectAsync(int clientId)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();

            var blobReferences = await db.Documents
                .AsNoTracking()
                .Where(d => d.ClientId == clientId)
                .Select(d => d.BlobReference)
                .ToListAsync();

            var identityUserId = await db.Clients
                .AsNoTracking()
                .Where(c => c.ClientId == clientId)
                .Select(c => c.IdentityProviderSubjectId)
                .FirstOrDefaultAsync();

            return new ClientCleanupPlan
            {
                BlobReferences = blobReferences.Where(r => !string.IsNullOrWhiteSpace(r)).ToList(),
                IdentityUserId = identityUserId
            };
        }

        /// <summary>
        /// Remove stored files before the login and client records. Propagate failures so
        /// the caller retains the references needed to retry cleanup.
        /// </summary>
        public async Task RunAsync(ClientCleanupPlan plan)
        {
            ArgumentNullException.ThrowIfNull(plan);

            foreach (var blobReference in plan.BlobReferences)
            {
                try
                {
                    await _blobStorageService.DeleteAsync(blobReference);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "A deleted client's stored file could not be removed.");
                    throw;
                }
            }

            if (string.IsNullOrWhiteSpace(plan.IdentityUserId))
            {
                return;
            }

            try
            {
                var user = await _userManager.FindByIdAsync(plan.IdentityUserId);

                if (user is not null)
                {
                    var result = await _userManager.DeleteAsync(user);

                    if (!result.Succeeded)
                    {
                        _logger.LogWarning("A deleted client's login could not be removed: {Errors}",
                            string.Join("; ", result.Errors.Select(e => e.Code)));
                        throw new InvalidOperationException("The client's login could not be removed.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "A deleted client's login could not be removed.");
                throw;
            }
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
