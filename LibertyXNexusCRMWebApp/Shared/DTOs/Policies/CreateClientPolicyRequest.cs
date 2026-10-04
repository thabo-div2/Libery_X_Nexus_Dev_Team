using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Policies
{
    public class CreateClientPolicyRequest  //for the advisor to assign a policy to a client
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
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
