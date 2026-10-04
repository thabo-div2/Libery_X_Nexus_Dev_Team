using API.Data;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Models.Enums;
using System.Net.NetworkInformation;

namespace API.Repositories.Implementations
{
    public class CaseRepository : GenericRepository<Case>, ICaseRepository
    {
        public CaseRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(dbContextFactory) { }

        public async Task<Case?> GetByPolicyIdAsync(int policyId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var cas = await context.Cases.AsNoTracking().FirstOrDefaultAsync(c => c.PolicyId == policyId);

            return cas;
        }

        public async Task<IEnumerable<Case>> GetByStatusAsync(CaseStatus status) 
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var cases = await context.Cases
                                .Include(c => c.Policy)
                                .Where(c => c.Status == status)
                                .OrderByDescending(c => c.UpdatedAt)
                                .AsNoTracking()
                                .ToListAsync();

            return cases;
        }

        public async Task<IEnumerable<Case>> GetByClientIdAsync(int clientId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var cases = await context.Cases
                                .Include(c => c.Policy)
                                .Where(c => c.Policy.ClientId == clientId)
                                .OrderByDescending(c => c.UpdatedAt)
                                .AsNoTracking()
                                .ToListAsync();

            return cases;
        }

        public async Task UpdateStatusAsync(int caseId, CaseStatus status, string? notes = null)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var caseRecaord = await context.Cases.FindAsync(caseId);
            if (caseRecaord is null)
                throw new KeyNotFoundException($"Case with id {caseId} was not found.");

            caseRecaord.Status = status;

            if (notes is not null) caseRecaord.Notes = notes;
            caseRecaord.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
        }

        public async Task<Case> MarkStepCompleteAsync(int caseId, CaseStep step)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var caseRecord = await context.Cases.Include(c => c.Policy).FirstOrDefaultAsync(c => c.CaseId == caseId);

            if (caseRecord is null)
             throw new KeyNotFoundException($"Case with id {caseId} was not found.");

            var now = DateTime.UtcNow;

            switch (step)
            {
                case CaseStep.DetailsSubmitted:
                    caseRecord.DetailsSubmittedAt = now;
                    break;
                case CaseStep.AdviserReview:
                    caseRecord.AdviserReviewAt = now;
                    break;
                case CaseStep.FicaVerification:
                    caseRecord.FicaVerifiedAt = now;
                    break;
                case CaseStep.SubmittedToLiberty:
                    caseRecord.SubmittedToLibertyAt = now;
                    caseRecord.Status = CaseStatus.AwaitingApproval;
                    break;
                case CaseStep.PolicyIssued:
                    caseRecord.PolicyIssuedAt = now;
                    caseRecord.Status = CaseStatus.Completed;
                    break;
            }
            caseRecord.UpdatedAt = now;

            await context.SaveChangesAsync();

            return caseRecord;
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
