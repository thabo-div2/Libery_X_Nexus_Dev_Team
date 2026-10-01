using Shared.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Policies
{
    public class UpdatePolicyStatusRequest
    {
        [Required]
        public PolicyStatus NewStatus { get; set; }
    }
}
