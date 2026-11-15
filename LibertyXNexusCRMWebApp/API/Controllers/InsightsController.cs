using API.Data;
using API.Identity;
using API.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.Insights;
using Shared.Models;
using Shared.Models.Enums;
using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace API.Controllers;

/// <summary>
/// Read-only views across an advisor's book: dashboard numbers, upcoming renewals,
/// a client's activity timeline, and CSV exports for record keeping.
/// Everything is limited to the signed-in advisor's own clients.
/// </summary>
[ApiController, Route("api/insights"), Authorize(Roles = AppRoles.Advisor)]
public class InsightsController(IDbContextFactory<ApplicationDbContext> factory) : ControllerBase
{
    private int AdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id) ? id : -1;

    private static DateTime TodaySa() => DisplayTime.ToSouthAfrica(DateTime.UtcNow).Date;

    private static IQueryable<Policy> ActivePolicies(ApplicationDbContext db, int advisorId) => db.Policies
        .Where(p => p.Client != null && p.Client.AdvisorId == advisorId && !p.IsCatalogueItem
            && p.Status != PolicyStatus.Cancelled && p.Status != PolicyStatus.Matured);

    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
    /// <summary>
    /// Headline numbers: tasks, documents waiting on either side, onboarding progress and renewals.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<PracticeInsightsDto>> Dashboard()
    {
        await using var db = await factory.CreateDbContextAsync();
        var advisorId = AdvisorId;
        var today = TodaySa();
        var now = DateTime.UtcNow;

        var openTasks = db.AdvisorTasks.Where(t => t.AdvisorId == advisorId && t.CompletedAt == null);
        var open = await openTasks.CountAsync();
        var overdue = await openTasks.CountAsync(t => t.DueDate < today);
        var dueToday = await openTasks.CountAsync(t => t.DueDate >= today && t.DueDate < today.AddDays(1));

        var waitingOnClients = await db.DocumentRequests.CountAsync(r => r.Client.AdvisorId == advisorId
            && (r.FulfilledAt == null || r.ReviewStatus == DocumentReviewStatus.NeedsReplacement));
        var toReview = await db.Documents.CountAsync(d => d.Client.AdvisorId == advisorId && d.ReviewStatus == DocumentReviewStatus.Received);

        var (onboarded, total) = await CountOnboardedAsync(db, advisorId);

        var policies = ActivePolicies(db, advisorId).Where(p => p.EndDate != null && p.EndDate >= now);
        var renewals30 = await policies.CountAsync(p => p.EndDate <= now.AddDays(30));
        var renewals90 = await policies.CountAsync(p => p.EndDate <= now.AddDays(90));

        return Ok(new PracticeInsightsDto(open, overdue, dueToday, waitingOnClients, toReview, onboarded, total, renewals30, renewals90));
    }

    /// <summary>
    /// A client counts as onboarded when every step of their onboarding checklist is done:
    /// profile details, all requested documents accepted, FICA verified on every case, and a meeting held or confirmed.
    /// Only Registered and Active clients are counted: Invited clients haven't signed up yet and
    /// Dormant ones aren't being onboarded, so including them would only drag the figure down.
    /// </summary>
    private static async Task<(int Onboarded, int Total)> CountOnboardedAsync(ApplicationDbContext db, int advisorId)
    {
        var clients = await db.Clients.AsNoTracking()
            .Where(c => c.AdvisorId == advisorId && (c.Status == ClientStatus.Registered || c.Status == ClientStatus.Active))
            .Select(c => new
            {
                c.ClientId,
                c.Phone,
                c.IdentificationNumber,
                c.PopiaConsent,
                Requests = db.DocumentRequests.Count(r => r.ClientId == c.ClientId),
                Accepted = db.DocumentRequests.Count(r => r.ClientId == c.ClientId && r.FulfilledAt != null && r.ReviewStatus == DocumentReviewStatus.Accepted),
                Cases = db.Cases.Count(k => k.Policy.ClientId == c.ClientId && k.Policy.Status != PolicyStatus.Cancelled && k.Policy.Status != PolicyStatus.Matured),
                Verified = db.Cases.Count(k => k.Policy.ClientId == c.ClientId && k.Policy.Status != PolicyStatus.Cancelled && k.Policy.Status != PolicyStatus.Matured && k.FicaVerifiedAt != null),
                Meeting = db.Meetings.Any(m => m.ClientId == c.ClientId && (m.Status == MeetingStatus.Confirmed || m.Status == MeetingStatus.Completed))
            })
            .ToListAsync();

        // Same profile rule as the client's own checklist (WorkflowController.Onboarding), so the two always agree.
        var onboarded = clients.Count(c =>
            !string.IsNullOrWhiteSpace(c.Phone) && !string.IsNullOrWhiteSpace(c.IdentificationNumber) && c.PopiaConsent
            && c.Accepted == c.Requests && c.Cases > 0 && c.Verified == c.Cases && c.Meeting);
        return (onboarded, clients.Count);
    }

    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
    /// <summary>
    /// The dashboard's "Needs your attention" list: everything waiting on the advisor, most urgent first.
    /// Order: overdue tasks, meeting requests, today's meetings, unread client messages, documents to review,
    /// tasks due today, tomorrow's meetings, cases ready for FICA, then renewals with no follow-up yet.
    /// </summary>
    [HttpGet("attention")]
    public async Task<ActionResult<AttentionListDto>> Attention([FromQuery] int take = 8)
    {
        take = Math.Clamp(take, 1, 50);
        await using var db = await factory.CreateDbContextAsync();
        var advisorId = AdvisorId;
        var now = DateTime.UtcNow;
        var today = TodaySa();
        var items = new List<(int Priority, DateTime Order, AttentionItemDto Item)>();

        // Tasks: overdue first (oldest first), then due today.
        var tasks = await db.AdvisorTasks.AsNoTracking().Include(t => t.Client)
            .Where(t => t.AdvisorId == advisorId && t.CompletedAt == null && t.DueDate < today.AddDays(1))
            .ToListAsync();
        foreach (var t in tasks)
        {
            var overdueDays = (today - t.DueDate.Date).Days;
            var detail = Join("Task", t.Client?.FullName, overdueDays > 0 ? $"due {t.DueDate:d MMM}" : null) ?? "Task";
            items.Add(overdueDays > 0
                ? (1, t.DueDate, new AttentionItemDto("task", t.Title, detail, overdueDays == 1 ? "1 day overdue" : $"{overdueDays} days overdue", "red", "/tasks"))
                : (6, t.DueDate, new AttentionItemDto("task", t.Title, detail, "Due today", "amber", "/tasks")));
        }

        // Meetings: requests waiting for the advisor, then today's and tomorrow's confirmed meetings.
        var meetings = await db.Meetings.AsNoTracking().Include(m => m.Client)
            .Where(m => m.Client.AdvisorId == advisorId && m.MeetingDate >= now && m.MeetingDate < now.AddDays(14)
                && (m.Status == MeetingStatus.Requested || m.Status == MeetingStatus.Confirmed))
            .ToListAsync();
        foreach (var m in meetings)
        {
            var day = DisplayTime.ToSouthAfrica(m.MeetingDate).Date;
            var when = Join(DisplayTime.Format(m.MeetingDate), m.MeetingType) ?? DisplayTime.Format(m.MeetingDate);
            if (m.Status == MeetingStatus.Requested)
                items.Add((2, m.MeetingDate, new AttentionItemDto("meeting", $"Respond to {m.Client.FullName}'s meeting request", when, "Request", "amber", "/calendar")));
            else if (day == today)
                items.Add((3, m.MeetingDate, new AttentionItemDto("meeting", $"Meeting with {m.Client.FullName}", when, "Today", "blue", $"/meeting-prep?clientId={m.ClientId}")));
            else if (day == today.AddDays(1))
                items.Add((7, m.MeetingDate, new AttentionItemDto("meeting", $"Prepare for meeting with {m.Client.FullName}", when, "Tomorrow", "blue", $"/meeting-prep?clientId={m.ClientId}")));
        }

        // Unread messages, one row per client.
        var unread = await db.Messages.AsNoTracking()
            .Where(m => m.AdvisorId == advisorId && !m.FromAdvisor && m.ReadAt == null)
            .GroupBy(m => new { m.ClientId, m.Client.FirstName, m.Client.LastName })
            .Select(g => new { g.Key.ClientId, Name = g.Key.FirstName + " " + g.Key.LastName, Count = g.Count(), Latest = g.Max(x => x.SentAt) })
            .ToListAsync();
        foreach (var u in unread)
        {
            var lastText = await db.Messages.AsNoTracking()
                .Where(m => m.AdvisorId == advisorId && m.ClientId == u.ClientId && !m.FromAdvisor && m.ReadAt == null)
                .OrderByDescending(m => m.SentAt).Select(m => m.Text).FirstOrDefaultAsync();
            items.Add((4, u.Latest, new AttentionItemDto("message", $"Reply to {u.Name}", Shorten(lastText ?? "New message", 90),
                u.Count == 1 ? "Unread" : $"{u.Count} unread", "blue", "/messages")));
        }

        // Uploaded documents waiting for review, as one row.
        var toReview = await db.Documents.AsNoTracking()
            .Where(d => d.Client.AdvisorId == advisorId && d.ReviewStatus == DocumentReviewStatus.Received)
            .Select(d => new { d.Client.FirstName, d.Client.LastName, d.UpdateAt })
            .ToListAsync();
        if (toReview.Count > 0)
        {
            var names = toReview.Select(d => $"{d.FirstName} {d.LastName}").Distinct().ToList();
            var who = names.Count <= 2 ? string.Join(" and ", names) : $"{names[0]}, {names[1]} and {names.Count - 2} more";
            items.Add((5, toReview.Min(d => d.UpdateAt) ?? now, new AttentionItemDto("document",
                toReview.Count == 1 ? "Review 1 uploaded document" : $"Review {toReview.Count} uploaded documents", who, "Review", "amber", "/documents")));
        }

        // Cases waiting on FICA where every requested document has been accepted: ready to tick off.
        var ficaReady = await db.Cases.AsNoTracking()
            .Where(c => c.Policy.Client != null && c.Policy.Client.AdvisorId == advisorId
                && c.Policy.Status != PolicyStatus.Cancelled && c.Policy.Status != PolicyStatus.Matured
                && c.AdviserReviewAt != null && c.FicaVerifiedAt == null
                && db.DocumentRequests.Any(r => r.ClientId == c.Policy.ClientId)
                && !db.DocumentRequests.Any(r => r.ClientId == c.Policy.ClientId
                    && (r.FulfilledAt == null || r.ReviewStatus != DocumentReviewStatus.Accepted)))
            .Select(c => new { c.Policy.PolicyName, c.Policy.Client!.FirstName, c.Policy.Client.LastName, c.UpdatedAt })
            .ToListAsync();
        foreach (var c in ficaReady)
        {
            var name = $"{c.FirstName} {c.LastName}";
            items.Add((8, c.UpdatedAt, new AttentionItemDto("case", $"Mark FICA verification done for {name}",
                $"{c.PolicyName} · all requested documents accepted", "Ready", "green", $"/clients?search={Uri.EscapeDataString(name)}")));
        }

        // Renewals in the next 30 days that have no open follow-up task.
        var renewals = await ActivePolicies(db, advisorId).AsNoTracking().Include(p => p.Client)
            .Where(p => p.EndDate != null && p.EndDate >= now && p.EndDate <= now.AddDays(30)
                && !db.AdvisorTasks.Any(t => t.AdvisorId == advisorId && t.CompletedAt == null && t.PolicyId == p.PolicyId))
            .ToListAsync();
        foreach (var p in renewals)
        {
            var days = Math.Max(0, (DisplayTime.ToSouthAfrica(p.EndDate!.Value).Date - today).Days);
            items.Add((9, p.EndDate.Value, new AttentionItemDto("renewal", $"{p.PolicyName} renews for {p.Client!.FullName}",
                $"Ends {DisplayTime.ToSouthAfrica(p.EndDate.Value):d MMM} · no follow-up yet", days == 1 ? "1 day" : $"{days} days",
                days <= 14 ? "red" : "amber", "advisor#renewals")));
        }

        var ordered = items.OrderBy(i => i.Priority).ThenBy(i => i.Order).Select(i => i.Item).ToList();
        return Ok(new AttentionListDto(ordered.Count, ordered.Take(take).ToList()));
    }

    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
    /// <summary>
    /// Policies ending within the next <paramref name="days"/> days (30, 60 or 90), soonest first.
    /// </summary>
    [HttpGet("renewals")]
    public async Task<ActionResult<List<RenewalDto>>> Renewals([FromQuery] int days = 90)
    {
        days = Math.Clamp(days, 1, 365);
        await using var db = await factory.CreateDbContextAsync();
        var advisorId = AdvisorId;
        var now = DateTime.UtcNow;
        var today = TodaySa();

        var policies = await ActivePolicies(db, advisorId).AsNoTracking().Include(p => p.Client)
            .Where(p => p.EndDate != null && p.EndDate >= now && p.EndDate <= now.AddDays(days))
            .OrderBy(p => p.EndDate)
            .ToListAsync();

        var policyIds = policies.Select(p => p.PolicyId).ToList();
        var withTask = await db.AdvisorTasks
            .Where(t => t.AdvisorId == advisorId && t.CompletedAt == null && t.PolicyId != null && policyIds.Contains(t.PolicyId.Value))
            .Select(t => t.PolicyId!.Value).Distinct().ToListAsync();

        return Ok(policies.Select(p => new RenewalDto(p.PolicyId, p.PolicyName, p.Provider, p.Client!.ClientId, p.Client.FullName,
            p.EndDate!.Value, Math.Max(0, (DisplayTime.ToSouthAfrica(p.EndDate.Value).Date - today).Days), p.PremiumAmount,
            withTask.Contains(p.PolicyId))).ToList());
    }

    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
    /// <summary>
    /// Everything that happened for one client, newest first: meetings, documents, messages, notes, tasks and policies.
    /// </summary>
    [HttpGet("clients/{clientId:int}/timeline")]
    public async Task<ActionResult<TimelinePageDto>> Timeline(int clientId, [FromQuery] int take = 30, [FromQuery] string? kind = null)
    {
        take = Math.Clamp(take, 1, 300);
        await using var db = await factory.CreateDbContextAsync();
        var advisorId = AdvisorId;
        if (!await db.Clients.AnyAsync(c => c.ClientId == clientId && c.AdvisorId == advisorId)) return NotFound();

        bool Wants(string k) => string.IsNullOrEmpty(kind) || string.Equals(kind, k, StringComparison.OrdinalIgnoreCase);
        // Each source fetches one more than needed so "has more" is known without counting everything.
        var per = take + 1;
        var items = new List<TimelineItemDto>();

        if (Wants("meeting"))
        {
            var meetings = await db.Meetings.AsNoTracking().Where(m => m.ClientId == clientId)
                .OrderByDescending(m => m.CreatedAt).Take(per).ToListAsync();
            items.AddRange(meetings.Select(m => new TimelineItemDto("meeting",
                $"Meeting {MeetingVerb(m.Status)}: {DisplayTime.Format(m.MeetingDate)}",
                Join(m.MeetingType, m.Location, m.SharedSummary is null ? null : "Summary shared"), m.CreatedAt, "/calendar")));
        }

        if (Wants("document"))
        {
            var documents = await db.Documents.AsNoTracking().Where(d => d.ClientId == clientId)
                .OrderByDescending(d => d.ReviewedAt ?? d.UpdateAt).Take(per).ToListAsync();
            foreach (var d in documents)
            {
                var type = DocumentTypeLabel(d.DocumentType);
                if (d.UpdateAt is DateTime uploaded)
                    items.Add(new TimelineItemDto("document", $"{type} uploaded", d.FileName, uploaded, "/documents"));
                if (d.ReviewedAt is DateTime reviewed && d.ReviewStatus != DocumentReviewStatus.Received)
                    items.Add(new TimelineItemDto("document",
                        d.ReviewStatus == DocumentReviewStatus.Accepted ? $"{type} accepted" : $"{type} needs replacing",
                        d.ReviewNote ?? d.FileName, reviewed, "/documents"));
            }
        }

        if (Wants("message"))
        {
            var messages = await db.Messages.AsNoTracking().Where(m => m.ClientId == clientId && m.AdvisorId == advisorId)
                .OrderByDescending(m => m.SentAt).Take(per).ToListAsync();
            items.AddRange(messages.Select(m => new TimelineItemDto("message",
                m.FromAdvisor ? "You sent a message" : "Client sent a message", Shorten(m.Text, 140), m.SentAt, "/messages")));
        }

        if (Wants("note"))
        {
            var notes = await db.ClientNotes.AsNoTracking().Where(n => n.ClientId == clientId && n.AdvisorId == advisorId)
                .OrderByDescending(n => n.CreatedAt).Take(per).ToListAsync();
            items.AddRange(notes.Select(n => new TimelineItemDto("note", "Note added", Shorten(n.Text, 140), n.CreatedAt, null)));
        }

        if (Wants("task"))
        {
            var tasks = await db.AdvisorTasks.AsNoTracking().Where(t => t.ClientId == clientId && t.AdvisorId == advisorId)
                .OrderByDescending(t => t.CompletedAt ?? t.CreatedAt).Take(per).ToListAsync();
            foreach (var t in tasks)
            {
                items.Add(new TimelineItemDto("task", $"Task created: {t.Title}", $"Due {t.DueDate:d MMM yyyy}", t.CreatedAt, "/tasks"));
                if (t.CompletedAt is DateTime done)
                    items.Add(new TimelineItemDto("task", $"Task completed: {t.Title}", null, done, "/tasks"));
            }
        }

        if (Wants("policy"))
        {
            var policies = await db.Policies.AsNoTracking().Where(p => p.ClientId == clientId && !p.IsCatalogueItem)
                .OrderByDescending(p => p.CreatedAt).Take(per).ToListAsync();
            items.AddRange(policies.Select(p => new TimelineItemDto("policy", $"Policy added: {p.PolicyName}",
                Join(p.Provider, p.Status.ToString()), p.CreatedAt, null)));
        }

        var ordered = items.OrderByDescending(i => i.At).ToList();
        return Ok(new TimelinePageDto(ordered.Take(take).ToList(), ordered.Count > take));
    }

    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
    /// <summary>
    /// A CSV summary of one client: profile, policies, documents and meetings.
    /// </summary>
    [HttpGet("clients/{clientId:int}/export")]
    public async Task<IActionResult> ExportClient(int clientId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var client = await db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.ClientId == clientId && c.AdvisorId == AdvisorId);
        if (client is null) return NotFound();

        var policies = await db.Policies.AsNoTracking().Where(p => p.ClientId == clientId && !p.IsCatalogueItem).OrderBy(p => p.PolicyName).ToListAsync();
        var documents = await db.Documents.AsNoTracking().Include(d => d.Policy).Where(d => d.ClientId == clientId).OrderByDescending(d => d.UpdateAt).ToListAsync();
        var meetings = await db.Meetings.AsNoTracking().Where(m => m.ClientId == clientId).OrderByDescending(m => m.MeetingDate).ToListAsync();

        var csv = new CsvWriter();
        csv.Row("Client summary");
        csv.Row("Name", "Email", "Phone", "ID number", "Status", "POPIA consent", "Client since");
        csv.Row(client.FullName, client.Email, client.Phone, client.IdentificationNumber, client.Status.ToString(),
            client.PopiaConsent ? "Yes" : "No", Date(client.CreatedAt));
        csv.Blank();

        csv.Row("Policies");
        csv.Row("Policy", "Provider", "Status", "Premium", "Cover", "Start date", "End date");
        foreach (var p in policies)
            csv.Row(p.PolicyName, p.Provider, p.Status.ToString(), Money(p.PremiumAmount), Money(p.CoverAmount), Date(p.StartDate), Date(p.EndDate));
        csv.Blank();

        csv.Row("Documents");
        csv.Row("File", "Type", "Policy", "Review status", "Signature", "Uploaded", "Reviewed");
        foreach (var d in documents)
            csv.Row(d.FileName, DocumentTypeLabel(d.DocumentType), d.Policy?.PolicyName, d.ReviewStatus.ToString(), d.SignatureStatus.ToString(), Date(d.UpdateAt), Date(d.ReviewedAt));
        csv.Blank();

        csv.Row("Meetings");
        csv.Row("Date", "Type", "Location", "Status", "Duration (min)");
        foreach (var m in meetings)
            csv.Row(DisplayTime.Format(m.MeetingDate, "yyyy-MM-dd HH:mm"), m.MeetingType, m.Location, m.Status.ToString(), m.DurationMinutes.ToString(CultureInfo.InvariantCulture));

        return File(csv.ToBytes(), "text/csv", $"client-{Slug(client.FullName)}-{TodaySa():yyyy-MM-dd}.csv");
    }

    /// <summary>
    /// A CSV list of every document across the advisor's clients.
    /// </summary>
    [HttpGet("documents/export")]
    public async Task<IActionResult> ExportDocuments()
    {
        await using var db = await factory.CreateDbContextAsync();
        var documents = await db.Documents.AsNoTracking().Include(d => d.Client).Include(d => d.Policy)
            .Where(d => d.Client.AdvisorId == AdvisorId)
            .OrderBy(d => d.Client.LastName).ThenBy(d => d.Client.FirstName).ThenByDescending(d => d.UpdateAt)
            .ToListAsync();

        var csv = new CsvWriter();
        csv.Row("Client", "Client email", "File", "Type", "Policy", "Review status", "Signature", "Visible to client", "Uploaded", "Reviewed");
        foreach (var d in documents)
            csv.Row(d.Client.FullName, d.Client.Email, d.FileName, DocumentTypeLabel(d.DocumentType), d.Policy?.PolicyName,
                d.ReviewStatus.ToString(), d.SignatureStatus.ToString(), d.VisibleToClient ? "Yes" : "No", Date(d.UpdateAt), Date(d.ReviewedAt));

        return File(csv.ToBytes(), "text/csv", $"documents-{TodaySa():yyyy-MM-dd}.csv");
    }

    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

    private static string MeetingVerb(MeetingStatus status) => status switch
    {
        MeetingStatus.Requested => "requested",
        MeetingStatus.Confirmed => "confirmed",
        MeetingStatus.Completed => "held",
        MeetingStatus.Cancelled => "cancelled",
        _ => status.ToString().ToLowerInvariant()
    };

    private static string DocumentTypeLabel(DocumentType type)
    {
        if (type == DocumentType.CertifiedIdCopy) return "Certified ID copy";
        var spaced = System.Text.RegularExpressions.Regex.Replace(type.ToString(), "(?<!^)([A-Z])", " $1").ToLowerInvariant();
        return char.ToUpperInvariant(spaced[0]) + spaced[1..];
    }

    private static string? Join(params string?[] parts)
    {
        var kept = parts.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        return kept.Count == 0 ? null : string.Join(" · ", kept);
    }

    private static string Shorten(string text, int max) => text.Length <= max ? text : text[..(max - 1)].TrimEnd() + "…";
    private static string Date(DateTime? value) => value is DateTime d ? DisplayTime.ToSouthAfrica(d).ToString("yyyy-MM-dd") : "";
    private static string Money(double? value) => value?.ToString("0.00", CultureInfo.InvariantCulture) ?? "";

    private static string Slug(string name)
    {
        var slug = new string(name.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray());
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        slug = slug.Trim('-');
        return slug.Length == 0 ? "client" : slug;
    }

    /// <summary>
    /// Builds CSV text that Excel opens cleanly. Cells starting with = + - @ are prefixed with an
    /// apostrophe so a spreadsheet never runs them as formulas (CSV injection).
    /// </summary>
    private sealed class CsvWriter
    {
        private readonly StringBuilder _text = new();

        public void Row(params string?[] cells) => _text.AppendLine(string.Join(",", cells.Select(Cell)));
        public void Blank() => _text.AppendLine();

        // UTF-8 with a byte-order mark so Excel shows accented names correctly.
        public byte[] ToBytes() => Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(_text.ToString())).ToArray();

        private static string Cell(string? value)
        {
            var text = value ?? "";
            if (text.Length > 0 && "=+-@\t\r".Contains(text[0])) text = "'" + text;
            return text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? $"\"{text.Replace("\"", "\"\"")}\"" : text;
        }
    }
}
