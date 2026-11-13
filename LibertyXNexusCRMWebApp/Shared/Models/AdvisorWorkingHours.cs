using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models
{
    /// <summary>
    /// The advisor's normal working hours for one day of the week (their default week).
    /// There is one row per advisor per day. Times are South African time.
    /// </summary>
    public class AdvisorWorkingHours
    {
        [Key]
        public int AdvisorWorkingHoursId { get; set; }

        [Required]
        public int AdvisorId { get; set; }

        [ForeignKey(nameof(AdvisorId))]
        public Advisor Advisor { get; set; } = null!;

        public DayOfWeek DayOfWeek { get; set; }

        /// <summary>False when the advisor is off on this day of the week.</summary>
        public bool IsWorkingDay { get; set; }

        public TimeOnly StartTime { get; set; }

        public TimeOnly EndTime { get; set; }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
