namespace Shared.DTOs.Advisor
{
    public class CasePipelineItemDto
    {
        public string ClientName { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public string Institution { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}
