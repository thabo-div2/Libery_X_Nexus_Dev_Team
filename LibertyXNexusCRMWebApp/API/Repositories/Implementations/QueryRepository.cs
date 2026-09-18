using API.Data;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Models.Enums;
using System.Net.NetworkInformation;

namespace API.Repositories.Implementations
{
    public class QueryRepository : GenericRepository<ClientQuery>, IQueryRepository
    {
        public QueryRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(dbContextFactory) { }

        public async Task<IEnumerable<ClientQuery>> GetByClientIdAsync(int clientId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var queries = await context.Queries
                             .Where(cq => cq.ClientId == clientId)
                             .OrderByDescending(cq => cq.CreatedAt)
                             .AsNoTracking()
                             .ToListAsync();

            return queries;
        }

        public async Task<IEnumerable<ClientQuery>> GetByStatusAsync(QueryStatus status)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var queries = await context.Queries
                            .Include(cq => cq.Client)
                            .Where(cq => cq.Status == status)
                            .OrderByDescending(cq => cq.CreatedAt)
                            .AsNoTracking()
                            .ToListAsync();

            return queries;
        }

        public async Task<IEnumerable<ClientQuery>> GetOpenForAdvisorAsync(int advisorId) 
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var queries = await context.Queries
                            .Include(cq => cq.Client)
                            .Where(cq => cq.Status == QueryStatus.Open && (cq.AdvisorId == advisorId || cq.Client.AdvisorId == advisorId))
                            .OrderByDescending(cq => cq.CreatedAt)
                            .AsNoTracking()
                            .ToListAsync();

            return queries;
        }

        public async Task<bool> BelongsToClientAsync(int queryId, int clientId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            return await context.Queries.AnyAsync(cq => cq.QueryId == queryId && cq.ClientId == clientId);
        }
    }
}
