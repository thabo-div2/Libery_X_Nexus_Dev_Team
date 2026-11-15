using System.ComponentModel.DataAnnotations;
namespace Shared.Models;
public class AdvisorTask
{
    public int AdvisorTaskId { get; set; }
    public int AdvisorId { get; set; }
    public Advisor Advisor { get; set; } = null!;
    public int? ClientId { get; set; }
    public Client? Client { get; set; }
    public int? PolicyId { get; set; }
    public Policy? Policy { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string? Note { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    /// <summary>
    /// Set on tasks the app creates itself (e.g. "document:42:replace"), so the same event never makes a second task.
    /// Null for tasks an advisor typed in.
    /// </summary>
    [MaxLength(120)] public string? SourceKey { get; set; }
}
