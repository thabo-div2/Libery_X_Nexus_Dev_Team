using API.Data;
using API.DTOs.Advisor;
using API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Implementations
{
    public class AdvisorService : IAdvisorService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public AdvisorService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<AdvisorDashboardDto> GetDashboardAsync(int advisorId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var advisor = await context.Advisors.AsNoTracking().FirstOrDefaultAsync(a => a.AdvisorId == advisorId);

            if (advisor is null)
            {
                return new AdvisorDashboardDto();
            }

            var cases = await context.Cases
                .AsNoTracking()
                .Include(c => c.Policy)
                .ThenInclude(p => p.Client)
                .Include(c => c.Policy)
                .ThenInclude(p => p.Documents)
                .Where(c =>
                    c.Policy.Client != null && 
                    c.Policy.Client.AdvisorId == advisorId)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();

            var activeCases = cases.Count(c =>
                c.Status == Shared.Models.Enums.CaseStatus.InProgress ||
                c.Status == Shared.Models.Enums.CaseStatus.AwaitingApproval);

            var waitingOnClient = cases.Count(c =>
                c.Status == Shared.Models.Enums.CaseStatus.OnHold);

            var awaitingDocuments = cases.Count(c =>
                c.Policy.Documents.Count == 0);

            var today = DateTime.UtcNow.Date;

            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);

            var endOfWeek = startOfWeek.AddDays(7);

            var meetingsThisWeek = await context.Meetings
                .AsNoTracking()
                .Where(m =>
                m.Client.AdvisorId == advisorId &&
                m.MeetingDate >= startOfWeek &&
                m.MeetingDate < endOfWeek &&
                m.Status != Shared.Models.Enums.MeetingStatus.Cancelled)
                .CountAsync();

            var pipelineValue = await context.Policies
                .AsNoTracking()
                .Where(p =>
                p.Client != null &&
                p.Client.AdvisorId == advisorId &&
                !p.IsCatalogueItem &&
                p.Status != Shared.Models.Enums.PolicyStatus.Cancelled &&
                p.Status != Shared.Models.Enums.PolicyStatus.Matured)
                .SumAsync(p => p.PremiumAmount ?? 0);

            var caseItems = cases
                .Take(10)
                .Select(c => new CasePipelineItemDto
                {
                    ClientName = c.Policy.Client?.FullName ?? "Unknown client",
                    Product = c.Policy.PolicyName,
                    Institution = c.Policy.Provider,
                    Status = c.Status.ToString(),
                    UpdatedAt = c.UpdatedAt
                })
                .ToList();

            var deadlineDate = today.AddDays(30);

            var deadlines = await context.Policies
                .AsNoTracking()
                .Where(p =>
                p.Client != null &&
                p.Client.AdvisorId == advisorId &&
                p.EndDate.HasValue &&
                p.EndDate.Value >= today &&
                p.EndDate.Value <= deadlineDate)
                .OrderBy(p => p.EndDate)
                .Take(10)
                .Select(p => new DeadlineItemDto
                {
                    Title = "Policy expiry",
                    ClientName = p.Client!.FullName,
                    Date = p.EndDate!.Value,
                    Urgency = p.EndDate.Value <= today.AddDays(7)
                        ? "Urgent"
                        : "Soon"
                })
                .ToListAsync();

            var institutionsGroups = cases
                .GroupBy(c => c.Policy.Provider)
                .Select(g => new
                {
                    Name = g.Key,
                    CaseCount = g.Count()
                })
                .OrderByDescending(x => x.CaseCount)
                .ToList();

            var totalInstitutionsCases = institutionsGroups.Sum(x => x.CaseCount);

            var institutions = institutionsGroups
                .Select(x => new InstitutionItemDto
                {
                    Name = x.Name,
                    CaseCount = x.CaseCount,
                    Percentage = totalInstitutionsCases == 0 ? 0
                        : (int)Math.Round(x.CaseCount * 100.0 / totalInstitutionsCases)
                })
                .ToList();

            var dashboard = new AdvisorDashboardDto
            {
                AdvisorFirstName = advisor.FirstName,
                ActiveCases = activeCases,
                WaitingOnClient = waitingOnClient,
                AwaitingDocuments = awaitingDocuments,
                MeetingsThisWeek = meetingsThisWeek,
                PipelineValue = pipelineValue,
                Cases = caseItems,
                Deadlines = deadlines,
                Institutions = institutions
            };

            return dashboard;
        }
    }
}
