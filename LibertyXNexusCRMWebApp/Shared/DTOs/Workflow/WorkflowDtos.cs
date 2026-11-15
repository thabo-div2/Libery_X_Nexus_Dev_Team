using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;
namespace Shared.DTOs.Workflow;
public class TaskInput
{
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string? Note { get; set; }
    public DateTime DueDate { get; set; } = DateTime.Today;
    [Range(1, int.MaxValue)] public int? ClientId { get; set; }
    [Range(1, int.MaxValue)] public int? PolicyId { get; set; }
    public bool Completed { get; set; }
}
public record AdvisorTaskDto(int AdvisorTaskId, string Title, string? Note, DateTime DueDate,
    int? ClientId, string? ClientName, int? PolicyId, string? PolicyName, DateTime? CompletedAt, bool IsAutomatic = false);
public class DocumentReviewInput
{
    [Required, EnumDataType(typeof(DocumentReviewStatus))] public DocumentReviewStatus? Status { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
    public DateTime? ExpectedUpdatedAt { get; set; }
}
public record DocumentReviewDto(int DocumentId, string FileName, string Status, string? Note, DateTime? UpdatedAt, DateTime? ReviewedAt);
public class MeetingSummaryInput
{
    [MaxLength(2000)] public string? Text { get; set; }
    public bool CompleteMeeting { get; set; }
}
public record MeetingSummaryDto(int MeetingId, string? Text, DateTime? UpdatedAt, string Status);
public record OnboardingStep(string Title, bool Complete, string Detail, string? Link);
public record OnboardingDto(List<OnboardingStep> Steps);
