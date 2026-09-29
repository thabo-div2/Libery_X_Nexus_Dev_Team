namespace API.DTOs.Advisor
{
    public class DeadlineItemDto
    {
        public string Title { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Urgency { get; set; } = string.Empty;
    }
}
