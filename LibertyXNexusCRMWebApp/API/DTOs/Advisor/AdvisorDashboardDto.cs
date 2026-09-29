namespace API.DTOs.Advisor
{
    public class AdvisorDashboardDto
    {
        public string AdvisorFirstName { get; set; } = string.Empty;
        public int ActiveCases { get; set; }
        public int WaitingOnClient { get; set; }
        public int AwaitingDocuments { get; set; }
        public int MeetingsThisWeek { get; set; }
        public double PipelineValue { get; set; }
        public List<CasePipelineItemDto> Cases { get; set; } = new();
        public List<DeadlineItemDto> Deadlines { get; set; } = new();
        public List<InstitutionItemDto> Institutions { get; set; } = new();
    }
}
