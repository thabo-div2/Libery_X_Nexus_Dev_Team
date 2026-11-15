using Shared.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Insights;

/// <summary>Headline numbers for the advisor dashboard.</summary>
public record PracticeInsightsDto(
    int OpenTasks,
    int OverdueTasks,
    int DueTodayTasks,
    int DocumentsWaitingOnClients,
    int DocumentsToReview,
    int ClientsOnboarded,
    int ClientsTotal,
    int RenewalsNext30Days,
    int RenewalsNext90Days);

/// <summary>A policy that ends soon.</summary>
public record RenewalDto(int PolicyId, string PolicyName, string Provider, int ClientId, string ClientName,
    DateTime EndDate, int DaysLeft, double? PremiumAmount, bool HasOpenTask);

/// <summary>One thing that happened for a client, for their activity timeline.</summary>
/// <param name="Kind">meeting, document, message, note, task or policy.</param>
public record TimelineItemDto(string Kind, string Title, string? Detail, DateTime At, string? Link);

public record TimelinePageDto(List<TimelineItemDto> Items, bool HasMore);

/// <summary>Ask several clients for the same document at once.</summary>
public class BulkDocumentRequestInput
{
    [Required, MinLength(1, ErrorMessage = "Choose at least one client."), MaxLength(200)]
    public List<int> ClientIds { get; set; } = new();

    [Required]
    public DocumentType DocumentType { get; set; }

    [MaxLength(300)]
    public string? Note { get; set; }
}

public record BulkDocumentRequestSkip(int ClientId, string ClientName, string Reason);

public record BulkDocumentRequestResult(int Created, List<BulkDocumentRequestSkip> Skipped);
