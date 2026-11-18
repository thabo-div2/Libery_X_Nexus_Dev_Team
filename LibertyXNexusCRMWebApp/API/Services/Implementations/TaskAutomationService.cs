using API.Data;
using API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Models.Enums;

namespace API.Services.Implementations
{
    /// <summary>
    /// Creates advisor follow-up tasks from events. Tasks it creates carry a SourceKey naming the event,
    /// which a unique index enforces, so retries and double clicks never create duplicates.
    /// </summary>
    public class TaskAutomationService : ITaskAutomationService
    {
        public const int RenewalWindowDays = 30;

        private readonly IDbContextFactory<ApplicationDbContext> _factory;
        private readonly ILogger<TaskAutomationService> _logger;

        public TaskAutomationService(IDbContextFactory<ApplicationDbContext> factory, ILogger<TaskAutomationService> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        public static string DocumentKey(int documentId) => $"document:{documentId}:replace";
        public static string MeetingKey(int meetingId) => $"meeting:{meetingId}:prepare";
        public static string RenewalKey(int policyId, DateTime endDate) => $"policy:{policyId}:renewal:{endDate:yyyyMMdd}";

        public async Task DocumentNeedsReplacementAsync(int documentId)
        {
            await using var db = await _factory.CreateDbContextAsync();
            var doc = await db.Documents.AsNoTracking().Include(d => d.Client)
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);
            if (doc?.Client?.AdvisorId is not int advisorId) return;

            var type = SplitWords(doc.DocumentType.ToString());
            await EnsureAsync(db, new AdvisorTask
            {
                AdvisorId = advisorId,
                ClientId = doc.ClientId,
                PolicyId = doc.PolicyId,
                Title = Trim($"Follow up: {doc.Client.FullName} to replace {type}", 200),
                Note = Trim($"{doc.FileName} was marked as needing a replacement. Check the new upload has arrived.", 1000),
                DueDate = Today().AddDays(3),
                SourceKey = DocumentKey(documentId)
            }, moveDueDate: false);
        }

        public async Task SyncMeetingTaskAsync(int meetingId)
        {
            await using var db = await _factory.CreateDbContextAsync();
            var meeting = await db.Meetings.AsNoTracking().Include(m => m.Client)
                .FirstOrDefaultAsync(m => m.MeetingId == meetingId);
            if (meeting?.Client?.AdvisorId is not int advisorId) return;

            var key = MeetingKey(meetingId);
            if (meeting.Status == MeetingStatus.Cancelled)
            {
                // The meeting is off, so an open "prepare" task is no longer needed. Completed ones stay as history.
                var open = await db.AdvisorTasks.FirstOrDefaultAsync(t => t.AdvisorId == advisorId && t.SourceKey == key && t.CompletedAt == null);
                if (open is not null)
                {
                    db.AdvisorTasks.Remove(open);
                    await db.SaveChangesAsync();
                }
                return;
            }

            if (meeting.Status != MeetingStatus.Confirmed || meeting.MeetingDate <= DateTime.UtcNow) return;

            // Due the day before the meeting (South African date), or today if the meeting is tomorrow or sooner.
            var meetingDay = DisplayTime.ToSouthAfrica(meeting.MeetingDate).Date;
            var due = meetingDay.AddDays(-1) < Today() ? Today() : meetingDay.AddDays(-1);

            await EnsureAsync(db, new AdvisorTask
            {
                AdvisorId = advisorId,
                ClientId = meeting.ClientId,
                Title = Trim($"Prepare for meeting with {meeting.Client.FullName}", 200),
                Note = $"Meeting on {DisplayTime.Format(meeting.MeetingDate)}. Review their policies, documents and recent messages.",
                DueDate = due,
                SourceKey = key
            }, moveDueDate: true);
        }

        public async Task<int> EnsureRenewalTasksAsync(CancellationToken cancellationToken = default)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellationToken);
            var now = DateTime.UtcNow;
            var deadline = now.AddDays(RenewalWindowDays);

            var policies = await db.Policies.AsNoTracking().Include(p => p.Client)
                .Where(p => p.Client != null && p.Client.AdvisorId != null
                    && !p.IsCatalogueItem
                    && p.Status != PolicyStatus.Cancelled && p.Status != PolicyStatus.Matured
                    && p.EndDate != null && p.EndDate >= now && p.EndDate <= deadline)
                .ToListAsync(cancellationToken);

            var created = 0;
            foreach (var policy in policies)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var end = policy.EndDate!.Value;
                if (await EnsureAsync(db, new AdvisorTask
                {
                    AdvisorId = policy.Client!.AdvisorId!.Value,
                    ClientId = policy.ClientId,
                    PolicyId = policy.PolicyId,
                    Title = Trim($"Review renewal: {policy.PolicyName} for {policy.Client.FullName}", 200),
                    Note = $"This policy ends on {end:d MMM yyyy}. Contact the client about renewing or replacing it.",
                    DueDate = Today(),
                    SourceKey = RenewalKey(policy.PolicyId, end)
                }, moveDueDate: false)) created++;
            }

            return created;
        }

        /// <summary>
        /// Adds the task unless one with the same SourceKey exists. With moveDueDate, an open existing
        /// task gets the new due date (e.g. a rescheduled meeting). Returns true when a task was added.
        /// </summary>
        private async Task<bool> EnsureAsync(ApplicationDbContext db, AdvisorTask task, bool moveDueDate)
        {
            var existing = await db.AdvisorTasks.FirstOrDefaultAsync(t => t.AdvisorId == task.AdvisorId && t.SourceKey == task.SourceKey);
            if (existing is not null)
            {
                if (moveDueDate && existing.CompletedAt is null && (existing.DueDate != task.DueDate || existing.Note != task.Note))
                {
                    existing.DueDate = task.DueDate;
                    existing.Note = task.Note;
                    await db.SaveChangesAsync();
                }
                return false;
            }

            db.AdvisorTasks.Add(task);
            try
            {
                await db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                // Another request created the same task a moment ago; the unique index kept it to one.
                _logger.LogInformation(ex, "Automatic task {SourceKey} already exists.", task.SourceKey);
                db.Entry(task).State = EntityState.Detached;
                return false;
            }
        }

        private static DateTime Today() => DisplayTime.ToSouthAfrica(DateTime.UtcNow).Date;

        private static string Trim(string text, int max) => text.Length <= max ? text : text[..max];

        private static string SplitWords(string value)
        {
            if (value == "CertifiedIdCopy") return "certified ID copy";
            return System.Text.RegularExpressions.Regex.Replace(value, "(?<!^)([A-Z])", " $1").ToLowerInvariant();
        }
    }

    /// <summary>
    /// Runs the renewal check shortly after start-up and then every six hours.
    /// </summary>
    public class RenewalTaskBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<RenewalTaskBackgroundService> _logger;

        public RenewalTaskBackgroundService(IServiceScopeFactory scopes, ILogger<RenewalTaskBackgroundService> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Give start-up migrations a moment to finish first.
            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); } catch (OperationCanceledException) { return; }

            using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
            do
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var created = await scope.ServiceProvider.GetRequiredService<ITaskAutomationService>().EnsureRenewalTasksAsync(stoppingToken);
                    if (created > 0) _logger.LogInformation("Created {Count} policy renewal task(s).", created);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception ex)
                {
                    // A failed run must not stop the app; the next run tries again.
                    _logger.LogWarning(ex, "Policy renewal task check failed.");
                }
            }
            while (await WaitAsync(timer, stoppingToken));
        }

        private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken token)
        {
            try { return await timer.WaitForNextTickAsync(token); }
            catch (OperationCanceledException) { return false; }
        }
    }
}
