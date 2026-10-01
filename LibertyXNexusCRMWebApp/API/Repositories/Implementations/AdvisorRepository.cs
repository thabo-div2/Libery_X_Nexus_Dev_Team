using API.Data;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace API.Repositories.Implementations
{
    public class AdvisorRepository : GenericRepository<Advisor>, IAdvisorRepository
    {
        public AdvisorRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(dbContextFactory) { }

        public async Task<Advisor?> GetEmailAsync(string email)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var advisor = await context.Advisors.AsNoTracking().FirstOrDefaultAsync(a => a.Email == email);

            return advisor;
        }

        public async Task<Advisor?> GetByIdentitySubjectIdAsync(string subjectId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var advisor = await context.Advisors.AsNoTracking().FirstOrDefaultAsync(a => a.IdentityProviderSubjectId == subjectId);

            return advisor;
        }

        public async Task<Advisor?> GetWithClientAsync(int advisorId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var advisor = await context.Advisors.AsNoTracking().FirstOrDefaultAsync(a => a.AdvisorId == advisorId);

            return advisor;
        }
    }
}
