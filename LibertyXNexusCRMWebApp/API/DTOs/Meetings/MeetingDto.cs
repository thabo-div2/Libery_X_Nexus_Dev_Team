namespace API.DTOs.Meetings
{
    public class MeetingDto
    {
        public int MeetingId { get; set; }
        public int ClientId { get; set; }
        public int MeetidId { get; set; }
        public string? ClientName { get; set; }

        public DateTime MeetingDate { get; set; }

        public int DurationMinutes { get; set; }

        public string? MeetingType { get; set; }

        public string? Location { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt {  get; set; }

    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
