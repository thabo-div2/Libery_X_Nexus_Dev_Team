using System.ComponentModel.DataAnnotations;

namespace frontend.Models.Forms
{
    /// <summary>
    /// The "request a meeting" form used on the chat and message pages.
    /// </summary>
    public class MeetingRequestForm
    {
        [Required(ErrorMessage = "Pick a date and time for the meeting.")]
        [FutureDate]
        public DateTime? MeetingDateTime { get; set; }

        [MaxLength(500, ErrorMessage = "Keep the note under 500 characters.")]
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Advisor version of the meeting form, where the advisor also picks which client it is with.
    /// </summary>
    public sealed class AdvisorMeetingRequestForm : MeetingRequestForm
    {
        [Required(ErrorMessage = "Choose a client.")]
        public int? ClientId { get; set; }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
