namespace API.DTOs.Policies
{
    public class PolicyDto
    {
        public int PolicyId { get; set; }
        public int? ClientId { get; set; }
        public string PolicyName { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public double? PremiumAmount { get; set; }
        public double? CoverAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCatalogueItem { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
