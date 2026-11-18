using API.Data;
using API.Identity;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.Workflow;
using Shared.Models;
using Shared.Models.Enums;
using System.Security.Claims;

namespace API.Controllers;

[ApiController, Route("api/workflow"), Authorize]
public class WorkflowController(IDbContextFactory<ApplicationDbContext> factory,
    IAuditLogRepository audit, INotificationService notifications,
    ITaskAutomationService automation, ILogger<WorkflowController> logger) : ControllerBase
{
    private bool IsAdvisor => User.IsInRole(AppRoles.Advisor);
    private int AdvisorId => int.TryParse(User.FindFirstValue("advisorId"), out var id) ? id : -1;
    private int ClientId => int.TryParse(User.FindFirstValue("clientId"), out var id) ? id : -1;
    private IQueryable<Client> AccessibleClients(ApplicationDbContext db) => IsAdvisor
        ? db.Clients.Where(c => c.AdvisorId == AdvisorId) : db.Clients.Where(c => c.ClientId == ClientId);
    private async Task LogAsync(string entity, int id, string detail, AuditActionType action = AuditActionType.Update)
    {
        try { await audit.LogAsync(AdvisorId, UserRole.FinancialAdviser, action, entity, id, detail); }
        catch { /* Persisted work must not fail because activity logging is unavailable. */ }
    }
    private async Task NotifyAsync(int clientId, string text, string link)
    {
        try { await notifications.NotifyClientAsync(clientId, NotificationType.PendingAction, text, link); }
        catch { /* The in-app record remains available even when notification delivery fails. */ }
    }
    private static AdvisorTaskDto TaskDto(AdvisorTask t) => new(t.AdvisorTaskId, t.Title, t.Note, t.DueDate,
        t.ClientId, t.Client is null ? null : t.Client.FullName, t.PolicyId, t.Policy?.PolicyName, t.CompletedAt, t.SourceKey != null);

    [HttpGet("tasks"), Authorize(Roles = AppRoles.Advisor)]
    public async Task<ActionResult<List<AdvisorTaskDto>>> Tasks([FromQuery] bool? completed, [FromQuery] int? clientId,
        [FromQuery] int? skip = null, [FromQuery] int? take = null)
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.AdvisorTasks.AsNoTracking().Include(t => t.Client).Include(t => t.Policy).Where(t => t.AdvisorId == AdvisorId);
        // A client profile asks only for that client's tasks so other clients' work never reaches the page.
        if (clientId.HasValue) query = query.Where(t => t.ClientId == clientId.Value);
        if (completed.HasValue) query = query.Where(t => (t.CompletedAt != null) == completed.Value);
        // Completed tasks pile up over time, so they are listed newest first and can be paged.
        var ordered = completed == true
            ? query.OrderByDescending(t => t.CompletedAt).ThenByDescending(t => t.AdvisorTaskId)
            : query.OrderBy(t => t.CompletedAt != null).ThenBy(t => t.DueDate).ThenBy(t => t.AdvisorTaskId);
        if (take.HasValue)
        {
            Response.Headers["X-Total-Count"] = (await query.CountAsync()).ToString();
            return Ok((await ordered.Skip(Math.Max(0, skip ?? 0)).Take(Math.Clamp(take.Value, 1, 200)).ToListAsync()).Select(TaskDto));
        }
        return Ok((await ordered.ToListAsync()).Select(TaskDto));
    }
    private async Task<string?> ValidateTaskAsync(ApplicationDbContext db, TaskInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title)) return "Enter a task title.";
        if (input.DueDate == default) return "Choose a due date.";
        if (input.ClientId is int client && !await AccessibleClients(db).AnyAsync(c => c.ClientId == client))
            return "Choose one of your assigned clients.";
        if (input.PolicyId is int policy && (input.ClientId is null || !await db.Policies.AnyAsync(p => p.PolicyId == policy && p.ClientId == input.ClientId && !p.IsCatalogueItem)))
            return "Choose a policy belonging to the selected client.";
        return null;
    }
    [HttpPost("tasks"), Authorize(Roles = AppRoles.Advisor)]
    public async Task<IActionResult> CreateTask(TaskInput input)
    {
        await using var db = await factory.CreateDbContextAsync();
        var error = await ValidateTaskAsync(db, input);
        if (error is not null) return BadRequest(new { message = error });
        var task = new AdvisorTask { AdvisorId = AdvisorId, Title = input.Title.Trim(), Note = input.Note?.Trim(),
            DueDate = input.DueDate.Date, ClientId = input.ClientId, PolicyId = input.PolicyId,
            CompletedAt = input.Completed ? DateTime.UtcNow : null };
        db.AdvisorTasks.Add(task);
        await db.SaveChangesAsync();
        await LogAsync("Task", task.AdvisorTaskId, $"Created task: {task.Title}.", AuditActionType.Create);
        return Ok(TaskDto(task));
    }
    [HttpPut("tasks/{id:int}"), Authorize(Roles = AppRoles.Advisor)]
    public async Task<IActionResult> UpdateTask(int id, TaskInput input)
    {
        await using var db = await factory.CreateDbContextAsync();
        var task = await db.AdvisorTasks.FirstOrDefaultAsync(t => t.AdvisorTaskId == id && t.AdvisorId == AdvisorId);
        if (task is null) return NotFound(new { message = "Task was not found." });
        var error = await ValidateTaskAsync(db, input);
        if (error is not null) return BadRequest(new { message = error });
        task.Title = input.Title.Trim(); task.Note = input.Note?.Trim(); task.DueDate = input.DueDate.Date;
        task.ClientId = input.ClientId; task.PolicyId = input.PolicyId;
        task.CompletedAt = input.Completed ? task.CompletedAt ?? DateTime.UtcNow : null;
        await db.SaveChangesAsync();
        await LogAsync("Task", id, $"Updated task: {task.Title}.");
        return Ok(TaskDto(task));
    }
    [HttpDelete("tasks/{id:int}"), Authorize(Roles = AppRoles.Advisor)]
    public async Task<IActionResult> DeleteTask(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var task = await db.AdvisorTasks.FirstOrDefaultAsync(t => t.AdvisorTaskId == id && t.AdvisorId == AdvisorId);
        if (task is null) return NotFound();
        db.AdvisorTasks.Remove(task); await db.SaveChangesAsync();
        await LogAsync("Task", id, $"Deleted task: {task.Title}.", AuditActionType.Delete);
        return NoContent();
    }

    [HttpGet("documents/{id:int}/review"), Authorize(Roles = AppRoles.Advisor)]
    public async Task<IActionResult> GetReview(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var doc = await db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.DocumentId == id && d.Client.AdvisorId == AdvisorId);
        return doc is null ? NotFound() : Ok(new DocumentReviewDto(id, doc.FileName, doc.ReviewStatus.ToString(), doc.ReviewNote, doc.UpdateAt, doc.ReviewedAt));
    }
    [HttpPut("documents/{id:int}/review"), Authorize(Roles = AppRoles.Advisor)]
    public async Task<IActionResult> Review(int id, DocumentReviewInput input)
    {
        var error = WorkflowRules.ValidateReview(input.Status, input.Note);
        if (error is not null) return BadRequest(new { message = error });
        await using var strategyDb = await factory.CreateDbContextAsync();
        var reviewClientId = 0;
        var notifyClient = false;
        var result = await strategyDb.Database.CreateExecutionStrategy().ExecuteAsync<IActionResult>(async () =>
        {
            await using var db = await factory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var doc = await db.Documents.FirstOrDefaultAsync(d => d.DocumentId == id && d.Client.AdvisorId == AdvisorId);
            if (doc is null) return NotFound();
            if (doc.UpdateAt != input.ExpectedUpdatedAt) return Conflict(new { message = "This document changed. Close and reopen the review before saving." });
            reviewClientId = doc.ClientId;
            notifyClient = doc.VisibleToClient;
            var requests = await db.DocumentRequests.Where(r => r.ClientId == doc.ClientId && r.PolicyId == doc.PolicyId && r.DocumentType == doc.DocumentType).ToListAsync();
            WorkflowRules.ApplyReview(doc, requests, input.Status!.Value, input.Note, DateTime.UtcNow);
            var replacementRequest = WorkflowRules.EnsureReplacementRequest(doc, requests, DateTime.UtcNow);
            if (replacementRequest is not null && replacementRequest.DocumentRequestId == 0) db.DocumentRequests.Add(replacementRequest);
            await db.SaveChangesAsync(); await tx.CommitAsync();
            return Ok(new DocumentReviewDto(id, doc.FileName, doc.ReviewStatus.ToString(), doc.ReviewNote, doc.UpdateAt, doc.ReviewedAt));
        });
        if (result is OkObjectResult)
        {
            await LogAsync("Document", id, $"Document review: {input.Status}.");
            var reviewed = (DocumentReviewDto)((OkObjectResult)result).Value!;
            if (notifyClient) await NotifyAsync(reviewClientId, input.Status == DocumentReviewStatus.Accepted
                ? $"Your document {reviewed.FileName} was accepted."
                : $"Please replace {reviewed.FileName}: {input.Note?.Trim()}", "/client-documents");
            if (input.Status == DocumentReviewStatus.NeedsReplacement)
            {
                try { await automation.DocumentNeedsReplacementAsync(id); }
                catch (Exception ex) { logger.LogWarning(ex, "Could not create the follow-up task for document {DocumentId}.", id); }
            }
        }
        return result;
    }

    [HttpGet("meetings/{id:int}/summary")]
    public async Task<IActionResult> GetSummary(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var clients = AccessibleClients(db);
        var meeting = await db.Meetings.AsNoTracking().FirstOrDefaultAsync(m => m.MeetingId == id && clients.Any(c => c.ClientId == m.ClientId));
        return meeting is null ? NotFound() : Ok(new MeetingSummaryDto(id, meeting.SharedSummary, meeting.SummaryUpdatedAt, meeting.Status.ToString()));
    }
    [HttpPut("meetings/{id:int}/summary"), Authorize(Roles = AppRoles.Advisor)]
    public async Task<IActionResult> ShareSummary(int id, MeetingSummaryInput input)
    {
        await using var db = await factory.CreateDbContextAsync();
        var meeting = await db.Meetings.FirstOrDefaultAsync(m => m.MeetingId == id && m.Client.AdvisorId == AdvisorId);
        if (meeting is null) return NotFound();
        var error = WorkflowRules.ValidateMeetingSummary(meeting, DateTime.UtcNow);
        if (error is not null) return Conflict(new { message = error });
        meeting.SharedSummary = string.IsNullOrWhiteSpace(input.Text) ? null : input.Text.Trim();
        meeting.SummaryUpdatedAt = DateTime.UtcNow;
        if (input.CompleteMeeting) meeting.Status = MeetingStatus.Completed;
        meeting.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await LogAsync("Meeting", id, input.CompleteMeeting ? "Completed meeting and updated shared summary." : "Updated shared meeting summary.");
        if (meeting.SharedSummary is not null) await NotifyAsync(meeting.ClientId, "Your adviser shared a meeting summary.", "/client-calendar");
        return Ok(new MeetingSummaryDto(id, meeting.SharedSummary, meeting.SummaryUpdatedAt, meeting.Status.ToString()));
    }

    [HttpGet("clients/{id:int}/onboarding")]
    public async Task<IActionResult> Onboarding(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var client = await AccessibleClients(db).AsNoTracking().FirstOrDefaultAsync(c => c.ClientId == id);
        if (client is null) return NotFound();
        var requests = await db.DocumentRequests.AsNoTracking().Where(r => r.ClientId == id).ToListAsync();
        var cases = await db.Cases.AsNoTracking().Where(c => c.Policy.ClientId == id && c.Policy.Status != PolicyStatus.Cancelled && c.Policy.Status != PolicyStatus.Matured).ToListAsync();
        var accepted = requests.Count(r => r.FulfilledAt != null && r.ReviewStatus == DocumentReviewStatus.Accepted);
        var meeting = await db.Meetings.AnyAsync(m => m.ClientId == id && (m.Status == MeetingStatus.Confirmed || m.Status == MeetingStatus.Completed));
        return Ok(new OnboardingDto(new() {
            new("Profile details", !string.IsNullOrWhiteSpace(client.Phone) && !string.IsNullOrWhiteSpace(client.IdentificationNumber) && client.PopiaConsent,
                "Contact number, ID number and consent recorded. Ask your adviser to update missing details.", null),
            new("Requested documents", accepted == requests.Count,
                requests.Count == 0 ? "No documents currently requested." : $"{accepted} of {requests.Count} requested documents accepted.", IsAdvisor ? "/clients" : "/client-documents"),
            new("Identity verification", cases.Count > 0 && cases.All(c => c.FicaVerifiedAt != null),
                "Your adviser records verification separately from document acceptance.", null),
            new("First meeting arranged", meeting, "A confirmed or completed meeting with your adviser.", IsAdvisor ? "/calendar" : "/client-calendar")
        }));
    }
}
