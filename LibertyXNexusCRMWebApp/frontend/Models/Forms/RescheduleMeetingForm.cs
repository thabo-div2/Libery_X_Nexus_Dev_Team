using System.ComponentModel.DataAnnotations;

namespace frontend.Models.Forms
{
    /// <summary>
    /// The "Reschedule" form in the meeting details panel on the Calendar page.
    /// </summary>
    public sealed class RescheduleMeetingForm
    {
        [Required(ErrorMessage = "Pick a new date and time.")]
        [FutureDate]
        public DateTime? NewMeetingDateTime { get; set; }

        [Range(15, 480, ErrorMessage = "Choose a duration between 15 minutes and 8 hours.")]
        public int DurationMinutes { get; set; } = 60;
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
