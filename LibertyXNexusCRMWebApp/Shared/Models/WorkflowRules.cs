using Shared.DTOs.Workflow;
using Shared.Models.Enums;
namespace Shared.Models;
public static class WorkflowRules
{
    public static string? ValidateReview(DocumentReviewStatus? status, string? note) =>
        status is null || !Enum.IsDefined(status.Value) || status == DocumentReviewStatus.Received
        ? "Choose Accepted or Needs replacement."
        : status == DocumentReviewStatus.NeedsReplacement && string.IsNullOrWhiteSpace(note)
            ? "Explain why a replacement is needed so the client knows what to fix." : null;

    public static void ApplyReview(Document document, IEnumerable<DocumentRequest> requests, DocumentReviewStatus status, string? note, DateTime now)
    {
        var error = ValidateReview(status, note);
        if (error is not null) throw new ArgumentException(error);
        document.ReviewStatus = status;
        document.ReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        document.ReviewedAt = now;
        document.UpdateAt = now;
        foreach (var request in requests.Where(r => r.FulfilledDocumentId == document.DocumentId))
        {
            request.ReviewStatus = status;
            request.ReviewNote = document.ReviewNote;
            if (status == DocumentReviewStatus.NeedsReplacement)
            {
                request.FulfilledAt = null;
                request.FulfilledDocumentId = null;
            }
        }
    }
    // Every visible rejected upload needs an actionable checklist entry, even if unsolicited.
    // Reuse an outstanding entry in the exact scope rather than creating duplicates.
    public static DocumentRequest? EnsureReplacementRequest(Document document, IList<DocumentRequest> requests, DateTime now)
    {
        if (!document.VisibleToClient || document.ReviewStatus != DocumentReviewStatus.NeedsReplacement) return null;
        var request = requests.FirstOrDefault(r => r.ClientId == document.ClientId && r.PolicyId == document.PolicyId
            && r.DocumentType == document.DocumentType && r.FulfilledAt == null);
        if (request is null)
        {
            request = new DocumentRequest { ClientId = document.ClientId, PolicyId = document.PolicyId,
                DocumentType = document.DocumentType, CreatedAt = now,
                Note = "Please upload a corrected replacement document." };
            requests.Add(request);
        }
        request.ReviewStatus = DocumentReviewStatus.NeedsReplacement;
        request.ReviewNote = document.ReviewNote;
        return request;
    }

    public static void RecordUpload(Document document, DocumentRequest request, DateTime now)
    {
        if (document.ClientId != request.ClientId || document.PolicyId != request.PolicyId || document.DocumentType != request.DocumentType)
            throw new InvalidOperationException("The upload does not match this request's client, policy and document type.");
        request.FulfilledAt = now;
        request.FulfilledDocumentId = document.DocumentId;
        request.ReviewStatus = DocumentReviewStatus.Received;
        request.ReviewNote = null;
    }
    public const string SummaryAlreadySaved = "A summary has already been saved for this meeting.";

    // A meeting gets one shared summary: after it is saved it can't be saved again.
    public static string? ValidateMeetingSummary(Meeting meeting, DateTime now) =>
        meeting.Status is not (MeetingStatus.Confirmed or MeetingStatus.Completed)
            ? "Only confirmed or completed meetings can have a shared summary."
            : meeting.MeetingDate.AddMinutes(meeting.DurationMinutes) > now
                ? "A meeting summary can be shared after the meeting has ended."
                : !string.IsNullOrWhiteSpace(meeting.SharedSummary)
                    ? SummaryAlreadySaved : null;
}
