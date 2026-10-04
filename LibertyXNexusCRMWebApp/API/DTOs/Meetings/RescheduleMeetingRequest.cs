using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Meetings
{
    public class RescheduleMeetingRequest
    {
        [Required]
        public DateTime NewMeetingDate {  get; set; }

        [Range(15, 480)]
        public int? DurationMinutes { get; set; }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
