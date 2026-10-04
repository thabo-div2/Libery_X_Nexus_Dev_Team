using Shared.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Shared.Models
{
    /// <summary>
    /// Represents a case associated with a policy, which can have various statuses and notes.
    /// </summary>
    public class Case
    {
        [Key]
        public int CaseId { get; set; }

        [Required]
        public int PolicyId { get; set; }

        [ForeignKey(nameof(PolicyId))]
        public Policy Policy { get; set; } = null;

        public CaseStatus Status { get; set; } = CaseStatus.InProgress;

        public string? Notes { get; set; }

        public DateTime? DetailsSubmittedAt { get; set; }
        public DateTime? AdviserReviewAt { get; set; }
        public DateTime? FicaVerifiedAt { get; set; }
        public DateTime? SubmittedToLibertyAt { get; set; }
        public DateTime? PolicyIssuedAt { get; set; }

        public DateTime? TargetSubmissionDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
