using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Policies
{
    public class CreateCataloguePolicyRequest   //for the advisor to add a new policy to the general catalogue
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
    }
}
