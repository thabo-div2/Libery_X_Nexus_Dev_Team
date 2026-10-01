using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Policies
{
    /// <summary>
    /// For the advisor to assign a policy to a client
    /// </summary>
    public class CreateClientPolicyRequest  
    {
        [Required, MaxLength(200)]
        public string PolicyName { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Provider { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public double? PremiumAmount { get; set; }

        [Range(0, double.MaxValue)]
        public double? CoverAmount { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? TargetSubmissionDate { get; set; }
    }
}
