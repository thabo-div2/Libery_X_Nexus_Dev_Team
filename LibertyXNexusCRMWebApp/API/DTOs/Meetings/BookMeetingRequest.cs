using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Meetings
{
    public class BookMeetingRequest
    {
        [Required]
        public int ClientId { get; set; }

        [Required]
        public DateTime MeetingDate { get; set; }

        [Range(15, 480)]
        public int DurationMinutes { get; set; } = 60;

        [MaxLength(100)]
        public string? MeetingType { get; set; }

        [MaxLength(300)]
        public string? Location { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }
    }
}
