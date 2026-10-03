using API.Data;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Models.Enums;

namespace API.Repositories.Implementations
{
    public class ClientRepository : GenericRepository<Client>, IClientRepository
    {
        public ClientRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(dbContextFactory) { }

        public async Task<Client?> GetWithDetailsAsync(int clientId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var client = await context.Clients
                            .Include(c => c.Policies)
                            .Include(c => c.Meetings)
                            .Include(c => c.Documents)
                            .AsSplitQuery()
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.ClientId == clientId);
            
            return client;
        }

        public async Task<Client?> GetByEmailAsync(string email) 
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var client = await context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Email == email);

            return client;
        }

        public async Task<Client?> GetByIdentitySubjectIdAsync(string subjectId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var client = await context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.IdentityProviderSubjectId == subjectId);

            return client;
        }

        public async Task<IEnumerable<Client>> GetByAdvisorAsync(int advisorId) 
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var clients = await context.Clients.Where(c => c.AdvisorId == advisorId).OrderBy(c => c.LastName).ThenBy(c => c.FirstName).AsNoTracking().ToListAsync();

            return clients;
        }

        public async Task<IEnumerable<Client>> SearchAsync(string? searchTerm = null, ClientStatus? status = null, int? advisorId = null)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var query = context.Clients.AsNoTracking().AsQueryable();

            // Advisor should on be allowed to see their own clients, so filter by advisorId if provided
            if (advisorId.HasValue)
            {
                query = query.Where(c => c.AdvisorId == advisorId);
            }

            // Filter by status if provided
            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }

            // Filter by search term if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();

                query = query.Where(c =>
                EF.Functions.Like(c.FirstName, $"%{term}%") ||
                EF.Functions.Like(c.LastName, $"%{term}%") ||
                EF.Functions.Like(c.Email, $"%{term}%") ||
                EF.Functions.Like(c.FirstName + " " + c.LastName, $"%{term}%"));
            }

            return await query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync();
        }

        public async Task<IEnumerable<Client>> GetDueForReviewAsync(DateTime reviewCutoff)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            return await context.Clients.Where(c => c.Status == ClientStatus.Active)
                                        .Where(c => !c.Meetings.Any(m => m.Status == MeetingStatus.Completed) ||
                                            c.Meetings.Where(m => m.Status == MeetingStatus.Completed).Max(m => m.MeetingDate < reviewCutoff))
                                        .OrderBy(c => c.LastName)
                                        .AsNoTracking()
                                        .ToListAsync();
        }
    }
}
