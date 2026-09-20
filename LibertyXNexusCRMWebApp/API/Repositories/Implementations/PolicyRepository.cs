using API.Data;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Models.Enums;

namespace API.Repositories.Implementations
{
    public class PolicyRepository : GenericRepository<Policy>, IPolicyRepository
    {
        public PolicyRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(dbContextFactory) { }

        public async Task<IEnumerable<Policy>> GetByClientIdAsync(int clientId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var policies = await context.Policies
                                    .Where(p => p.ClientId == clientId)
                                    .OrderByDescending(p => p.StartDate)
                                    .AsNoTracking()
                                    .ToListAsync();

            return policies;
        }

        public async Task<IEnumerable<Policy>> GetCatalogoueAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var policies = await context.Policies
                                    .Where(p => p.IsCatalogueItem)
                                    .OrderBy(p => p.Provider).ThenBy(p => p.PolicyName)
                                    .AsNoTracking()
                                    .ToListAsync();

            return policies;
        }

        public async Task<IEnumerable<Policy>> GetByStatusAsync(PolicyStatus status)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var policies = await context.Policies
                                    .Where(p => p.Status == status && !p.IsCatalogueItem)
                                    .AsNoTracking()
                                    .ToListAsync();

            return policies;
        }

        public async Task<Policy?> GetWithDocumentAsync(int policyId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var policy = await context.Policies
                                .Include(p => p.Documents)
                                .Include(p => p.Case)
                                .AsNoTracking()
                                .FirstOrDefaultAsync(p => p.PolicyId == policyId);

            return policy;
        }

        public async Task<bool> BelongsToClientAsync(int policyId, int clientId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            return await context.Policies.AnyAsync(p => p.PolicyId == policyId && p.ClientId == clientId);
        }
    }
}
