using Shared.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Shared.Models
{
    /// <summary>
    /// Represents an invitation sent by an advisor to a potential client.
    /// </summary>
    public class Invitation
    {
        [Key]
        public int InvitationId { get; set; }

        [Required]
        public int AdvisorId { get; set; }

        [ForeignKey(nameof(AdvisorId))]
        public Advisor Advisor { get; set; } = null;

        [Required, MaxLength(256), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(128)]
        public string Token { get; set; } = string.Empty;

        public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

        public int? RedeemedByIdClient { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(14);
        public DateTime? RedeemedAt { get; set; }

        [NotMapped]
        public bool IsValid => Status == InvitationStatus.Pending && ExpiresAt > DateTime.UtcNow;
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
