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

            var today = DateTime.UtcNow.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(7);
            var previousWeekStart = startOfWeek.AddDays(-7);
            var previousWeekEnd = startOfWeek;
            var deadlineDate = today.AddDays(30);

            // These queries are all independent of each other, so instead of
            // awaiting them one at a time — nine sequential round-trips to
            // Azure SQL — run them concurrently. Each gets its own DbContext
            // because a single DbContext can't run two queries at once.
            var casesTask = GetCasesAsync(advisorId);
            var totalClientsTask = GetTotalClientsAsync(advisorId);
            var meetingsTodayTask = GetMeetingsCountAsync(advisorId, today, today.AddDays(1));
            var meetingsThisWeekTask = GetMeetingsCountAsync(advisorId, startOfWeek, endOfWeek);
            var meetingsLastWeekTask = GetMeetingsCountAsync(advisorId, previousWeekStart, previousWeekEnd);
            var pipelineValueTask = GetPipelineSumAsync(advisorId, null, null);
            var pipelineThisWeekTask = GetPipelineSumAsync(advisorId, startOfWeek, endOfWeek);
            var pipelineLastWeekTask = GetPipelineSumAsync(advisorId, previousWeekStart, previousWeekEnd);
            var deadlinesTask = GetDeadlinesAsync(advisorId, today, deadlineDate);

            await Task.WhenAll(
                casesTask, totalClientsTask, meetingsTodayTask, meetingsThisWeekTask,
                meetingsLastWeekTask, pipelineValueTask, pipelineThisWeekTask,
                pipelineLastWeekTask, deadlinesTask);

            var cases = casesTask.Result;
            var totalClients = totalClientsTask.Result;
            var meetingsToday = meetingsTodayTask.Result;
            var meetingsThisWeek = meetingsThisWeekTask.Result;
            var meetingsLastWeek = meetingsLastWeekTask.Result;
            var pipelineValue = pipelineValueTask.Result;
            var pipelineThisWeek = pipelineThisWeekTask.Result;
            var pipelineLastWeek = pipelineLastWeekTask.Result;
            var deadlines = deadlinesTask.Result;

            var activeCases = cases.Count(c =>
                c.Status == CaseStatus.InProgress ||
                c.Status == CaseStatus.AwaitingApproval);

            var waitingOnClient = cases.Count(c =>
                c.Status == CaseStatus.OnHold);

            var awaitingDocuments = cases.Count(c =>
                c.Policy.Documents.Count == 0);

            var meetingsChange = CalculatePercentageChange(meetingsThisWeek, meetingsLastWeek);

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
                c.Policy.Documents.Count == 0 &&
                c.UpdatedAt >= startOfWeek &&
                c.UpdatedAt < endOfWeek);

            var awaitingDocumentsLastWeek = cases.Count(c =>
                c.Policy.Documents.Count == 0 &&
                c.UpdatedAt >= previousWeekStart &&
                c.UpdatedAt < previousWeekEnd);

            var awaitingDocumentsChange = CalculatePercentageChange(
                awaitingDocumentsThisWeek,
                awaitingDocumentsLastWeek);

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
        /// Loads this advisor's cases, with the client, policy and document
        /// data the rest of the dashboard calculations need.
        /// </summary>
        private async Task<List<Shared.Models.Case>> GetCasesAsync(int advisorId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Cases
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
        }

        /// <summary>
        /// Counts this advisor's total clients.
        /// </summary>
        private async Task<int> GetTotalClientsAsync(int advisorId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Clients
                .AsNoTracking()
                .CountAsync(c => c.AdvisorId == advisorId);
        }

        /// <summary>
        /// Counts this advisor's non-cancelled meetings within a date range.
        /// </summary>
        private async Task<int> GetMeetingsCountAsync(int advisorId, DateTime from, DateTime to)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Meetings
                .AsNoTracking()
                .CountAsync(m =>
                    m.Client.AdvisorId == advisorId &&
                    m.MeetingDate >= from &&
                    m.MeetingDate < to &&
                    m.Status != MeetingStatus.Cancelled);
        }

        /// <summary>
        /// Sums this advisor's active, non-catalogue policy premiums,
        /// optionally restricted to policies created within a date range.
        /// </summary>
        private async Task<double> GetPipelineSumAsync(int advisorId, DateTime? from, DateTime? to)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.Policies
                .AsNoTracking()
                .Where(p =>
                    p.Client != null &&
                    p.Client.AdvisorId == advisorId &&
                    !p.IsCatalogueItem &&
                    p.Status != PolicyStatus.Cancelled &&
                    p.Status != PolicyStatus.Matured);

            if (from.HasValue && to.HasValue)
            {
                query = query.Where(p => p.CreatedAt >= from.Value && p.CreatedAt < to.Value);
            }

            return await query.SumAsync(p => p.PremiumAmount ?? 0);
        }

        /// <summary>
        /// Gets this advisor's upcoming policy-expiry deadlines.
        /// </summary>
        private async Task<List<DeadlineItemDto>> GetDeadlinesAsync(int advisorId, DateTime today, DateTime deadlineDate)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Policies
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