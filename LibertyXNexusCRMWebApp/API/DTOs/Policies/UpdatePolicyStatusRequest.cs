using Shared.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Policies
{
    public class UpdatePolicyStatusRequest
    {
        [Required]
        [EnumDataType(typeof(PolicyStatus))]
        public PolicyStatus? NewStatus { get; set; }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
