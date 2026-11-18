using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Availability
{
    /// <summary>
    /// The hours for one day of the week. Times are "HH:mm" in South African time.
    /// </summary>
    public class DayHoursDto
    {
        public DayOfWeek Day { get; set; }

        public bool IsWorkingDay { get; set; }

        public string? StartTime { get; set; }

        public string? EndTime { get; set; }
    }

    /// <summary>
    /// Custom hours for one date. Date is "yyyy-MM-dd" and times are "HH:mm".
    /// </summary>
    public class HoursOverrideDto
    {
        public string Date { get; set; } = string.Empty;

        public bool IsWorkingDay { get; set; }

        public string? StartTime { get; set; }

        public string? EndTime { get; set; }

        public string? Note { get; set; }
    }

    /// <summary>
    /// Everything the advisor's working hours page shows: the default week and the upcoming custom dates.
    /// </summary>
    public class AdvisorScheduleDto
    {
        /// <summary>False until the advisor has saved their hours. Until then nothing is restricted.</summary>
        public bool IsConfigured { get; set; }

        public List<DayHoursDto> Days { get; set; } = new();

        public List<HoursOverrideDto> Overrides { get; set; } = new();
    }

    /// <summary>
    /// The advisor's default week, sent when they save it. All seven days must be included.
    /// </summary>
    public class UpdateWeeklyHoursRequest
    {
        [Required]
        public List<DayHoursDto>? Days { get; set; }
    }

    /// <summary>
    /// Custom hours for one date, sent when the advisor adds or changes them.
    /// </summary>
    public class SaveHoursOverrideRequest
    {
        [Required]
        public string? Date { get; set; }

        public bool IsWorkingDay { get; set; }

        public string? StartTime { get; set; }

        public string? EndTime { get; set; }

        [MaxLength(200)]
        public string? Note { get; set; }
    }

    /// <summary>
    /// One open meeting time. StartUtc is what gets booked; Label is the South African time to show, e.g. "09:30".
    /// </summary>
    public class MeetingSlotDto
    {
        public DateTime StartUtc { get; set; }

        public string Label { get; set; } = string.Empty;
    }

    /// <summary>
    /// The open meeting times on one date.
    /// </summary>
    public class MeetingSlotsDto
    {
        /// <summary>False when the advisor hasn't set working hours, so any time can be requested.</summary>
        public bool IsConfigured { get; set; }

        public bool IsWorkingDay { get; set; } = true;

        /// <summary>Why there are no times, when there aren't any.</summary>
        public string Message { get; set; } = string.Empty;

        public List<MeetingSlotDto> Slots { get; set; } = new();
    }

    /// <summary>
    /// What a client sees about their advisor's availability right now.
    /// </summary>
    public class AdvisorAvailabilityDto
    {
        public bool IsConfigured { get; set; }

        public bool IsAvailableNow { get; set; } = true;

        /// <summary>A sentence for the client, e.g. "Your advisor is out of office right now. They'll be back tomorrow at 08:00."</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>The advisor's default week, so the client can see when to get in touch.</summary>
        public List<DayHoursDto> Days { get; set; } = new();
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
