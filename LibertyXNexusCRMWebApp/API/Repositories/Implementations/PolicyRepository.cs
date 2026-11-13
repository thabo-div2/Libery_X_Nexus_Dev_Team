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

        public async Task<Policy> UpdateStatusAsync(int policyId, PolicyStatus status)
        {
            using var strategyDb = await _dbContextFactory.CreateDbContextAsync();
            return await strategyDb.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                using var db = await _dbContextFactory.CreateDbContextAsync();
                await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                var policy = await db.Policies.Include(p => p.Case).FirstOrDefaultAsync(p => p.PolicyId == policyId)
                    ?? throw new KeyNotFoundException($"Policy {policyId} was not found.");
                PolicyLifecycle.ValidateTransition(policy, status, DateTime.UtcNow.Date);
                if (policy.Status != status)
                {
                    policy.Status = status;
                    policy.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
                await transaction.CommitAsync();
                return policy;
            });
        }

        // Editing descriptive details must never overwrite a concurrent lifecycle change.
        public override async Task UpdateAsync(Policy entity)
        {
            using var db = await _dbContextFactory.CreateDbContextAsync();
            var policy = await db.Policies.FindAsync(entity.PolicyId)
                ?? throw new KeyNotFoundException($"Policy {entity.PolicyId} was not found.");
            policy.PolicyName = entity.PolicyName;
            policy.Provider = entity.Provider;
            policy.Description = entity.Description;
            policy.PremiumAmount = entity.PremiumAmount;
            policy.CoverAmount = entity.CoverAmount;
            policy.StartDate = entity.StartDate;
            policy.EndDate = entity.EndDate;
            policy.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        public override async Task DeleteAsync(int id)
        {
            using var db = await _dbContextFactory.CreateDbContextAsync();
            var policy = await db.Policies.FindAsync(id)
                ?? throw new KeyNotFoundException($"Policy {id} was not found.");
            if (await db.Documents.AnyAsync(d => d.PolicyId == id))
                throw new InvalidOperationException("This policy still has documents. Delete its documents first.");
            // Remove policy-specific checklist items with the policy, never convert them to general requests.
            db.DocumentRequests.RemoveRange(await db.DocumentRequests.Where(r => r.PolicyId == id).ToListAsync());
            db.Policies.Remove(policy);
            await db.SaveChangesAsync();
        }

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

        public async Task<IEnumerable<Policy>> GetByStatusAsync(PolicyStatus status, int advisorId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            return await context.Policies
                .Where(p => p.Status == status
                            && !p.IsCatalogueItem
                            && p.Client != null
                            && p.Client.AdvisorId == advisorId)
                .AsNoTracking()
                .ToListAsync();
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

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
