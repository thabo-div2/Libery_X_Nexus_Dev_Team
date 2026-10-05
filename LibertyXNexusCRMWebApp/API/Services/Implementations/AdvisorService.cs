using API.Data;
using API.DTOs.Advisor;
using API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models.Enums;

namespace API.Services.Implementations
{
    /// <summary>
    /// Gets the data for the advisor dashboard.
    /// </summary>
    public class AdvisorService : IAdvisorService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        /// <summary>
        /// Sets up the service.
        /// </summary>
        public AdvisorService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        /// <summary>
        /// Builds the advisor's dashboard stats.
        /// </summary>
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
                .Where(c =>
                    c.Policy.Client != null &&
                    c.Policy.Client.AdvisorId == advisorId)
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => new
                {
                    c.Status,
                    c.UpdatedAt,

                    ClientFirstName = c.Policy.Client!.FirstName,
                    ClientLastName = c.Policy.Client.LastName,

                    PolicyName = c.Policy.PolicyName,
                    Provider = c.Policy.Provider,

                    HasDocuments = c.Policy.Documents.Any()
                })
                .ToListAsync();

            var activeCases = cases.Count(c =>
                c.Status == Shared.Models.Enums.CaseStatus.InProgress ||
                c.Status == Shared.Models.Enums.CaseStatus.AwaitingApproval);

            var waitingOnClient = cases.Count(c =>
                c.Status == Shared.Models.Enums.CaseStatus.OnHold);

            var awaitingDocuments = cases.Count(c => !c.HasDocuments);

            var today = DateTime.UtcNow.Date;

            var totalClients = await context.Clients
                .AsNoTracking()
                .CountAsync(c => c.AdvisorId == advisorId);

            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);

            var endOfWeek = startOfWeek.AddDays(7);

            var previousWeekStart = startOfWeek.AddDays(-7);
            var previousWeekEnd = startOfWeek;

            var meetings = await context.Meetings
                .AsNoTracking()
                .Where(m =>
                    m.Client.AdvisorId == advisorId &&
                    m.MeetingDate >= previousWeekStart &&
                    m.MeetingDate < endOfWeek &&
                    m.Status != MeetingStatus.Cancelled)
                .Select(m => m.MeetingDate)
                .ToListAsync();

            var meetingsToday = meetings.Count(d =>
                d >= today &&
                d < today.AddDays(1));

            var meetingsThisWeek = meetings.Count(d =>
                d >= startOfWeek &&
                d < endOfWeek);

            var meetingsLastWeek = meetings.Count(d =>
                d >= previousWeekStart &&
                d < previousWeekEnd);

            var meetingsChange = CalculatePercentageChange(
                meetingsThisWeek,
                meetingsLastWeek);

            var pipelineValue = await context.Policies
                .AsNoTracking()
                .Where(p =>
                    p.Client != null &&
                    p.Client.AdvisorId == advisorId &&
                    !p.IsCatalogueItem &&
                    p.Status != PolicyStatus.Cancelled &&
                    p.Status != PolicyStatus.Matured)
                .SumAsync(p => p.PremiumAmount ?? 0);

            var pipelineThisWeek = await context.Policies
                .AsNoTracking()
                .Where(p =>
                    p.Client != null &&
                    p.Client.AdvisorId == advisorId &&
                    !p.IsCatalogueItem &&
                    p.Status != PolicyStatus.Cancelled &&
                    p.Status != PolicyStatus.Matured &&
                    p.CreatedAt >= startOfWeek &&
                    p.CreatedAt < endOfWeek)
                .SumAsync(p => p.PremiumAmount ?? 0);

            var pipelineLastWeek = await context.Policies
                .AsNoTracking()
                .Where(p =>
                    p.Client != null &&
                    p.Client.AdvisorId == advisorId &&
                    !p.IsCatalogueItem &&
                    p.Status != PolicyStatus.Cancelled &&
                    p.Status != PolicyStatus.Matured &&
                    p.CreatedAt >= previousWeekStart &&
                    p.CreatedAt < previousWeekEnd)
                .SumAsync(p => p.PremiumAmount ?? 0);

            var pipelineValueChange = CalculatePercentageChange(
                pipelineThisWeek,
                pipelineLastWeek);

            var activeCasesThisWeek = cases.Count(c =>
                (c.Status == CaseStatus.InProgress ||
                 c.Status == CaseStatus.AwaitingApproval) &&
                c.UpdatedAt >= startOfWeek &&
                c.UpdatedAt < endOfWeek);

            var activeCasesLastWeek = cases.Count(c =>
                (c.Status == CaseStatus.InProgress ||
                    c.Status == CaseStatus.AwaitingApproval) &&
                c.UpdatedAt >= previousWeekStart &&
                c.UpdatedAt < previousWeekEnd);

            var activeCasesChange = CalculatePercentageChange(
                activeCasesThisWeek,
                activeCasesLastWeek);

            var awaitingDocumentsThisWeek = cases.Count(c =>
                !c.HasDocuments &&
                c.UpdatedAt >= startOfWeek &&
                c.UpdatedAt < endOfWeek);

            var awaitingDocumentsLastWeek = cases.Count(c =>
                !c.HasDocuments &&
                c.UpdatedAt >= previousWeekStart &&
                c.UpdatedAt < previousWeekEnd);

            var awaitingDocumentsChange = CalculatePercentageChange(
                awaitingDocumentsThisWeek,
                awaitingDocumentsLastWeek);

            var caseItems = cases
                .Take(10)
                .Select(c => new CasePipelineItemDto
                {
                    ClientName = string.IsNullOrWhiteSpace(c.ClientFirstName) &&
                                 string.IsNullOrWhiteSpace(c.ClientLastName)
                        ? "Unknown client"
                        : $"{c.ClientFirstName} {c.ClientLastName}".Trim(),

                    Product = c.PolicyName,
                    Institution = c.Provider,
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
                .GroupBy(c => c.Provider)
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
                AdvisorLastName = advisor.LastName,
                ActiveCases = activeCases,
                WaitingOnClient = waitingOnClient,
                AwaitingDocuments = awaitingDocuments,
                MeetingsThisWeek = meetingsThisWeek,
                PipelineValue = pipelineValue,
                ActiveCasesChange = activeCasesChange,
                AwaitingDocumentsChange = awaitingDocumentsChange,
                MeetingsChange = meetingsChange,
                PipelineValueChange = pipelineValueChange,
                Cases = caseItems,
                Deadlines = deadlines,
                Institutions = institutions,
                TotalClients = totalClients,
                MeetingsToday = meetingsToday
            };

            return dashboard;
        }

        /// <summary>
        /// Works out the change from last week.
        /// </summary>
        private static string CalculatePercentageChange(double current, double previous)
        {
            if (previous == 0)
            {
                return current == 0 ? "No change" : "+100%";
            }

            var change = ((current - previous) / previous) * 100;

            return $"{(change >= 0 ? "+" : "")}{change:F0}%";
        }
    }


}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
