using Shared.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Policies
{
    public class UpdatePolicyStatusRequest
    {
        [Required]
        public PolicyStatus NewStatus { get; set; }
    }
}
