using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models
{
    /// <summary>
    /// Custom hours for one specific date. It replaces the advisor's normal hours for that date:
    /// either different hours, or the whole day off. Times are South African time.
    /// </summary>
    public class AdvisorHoursOverride
    {
        [Key]
        public int AdvisorHoursOverrideId { get; set; }

        [Required]
        public int AdvisorId { get; set; }

        [ForeignKey(nameof(AdvisorId))]
        public Advisor Advisor { get; set; } = null!;

        public DateOnly Date { get; set; }

        /// <summary>False when the advisor is off for the whole of this date.</summary>
        public bool IsWorkingDay { get; set; }

        public TimeOnly? StartTime { get; set; }

        public TimeOnly? EndTime { get; set; }

        /// <summary>Optional reason, e.g. "Public holiday" or "Conference".</summary>
        [MaxLength(200)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
