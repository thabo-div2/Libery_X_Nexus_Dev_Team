using Shared.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Shared.Models
{
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

        public DateTime? TargetSubmissionDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
