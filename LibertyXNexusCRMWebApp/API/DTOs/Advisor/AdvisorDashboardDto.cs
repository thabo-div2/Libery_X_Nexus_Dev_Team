namespace API.DTOs.Advisor
{
    public class AdvisorDashboardDto
    {
        public string AdvisorFirstName { get; set; } = string.Empty;
        public string AdvisorLastName { get; set; } = string.Empty;
        public int TotalClients { get; set; }
        public int ActiveCases { get; set; }
        public int WaitingOnClient { get; set; }
        public int AwaitingDocuments { get; set; }
        public int MeetingsThisWeek { get; set; }
        public int MeetingsToday { get; set; }
        public double PipelineValue { get; set; }
        public string AwaitingDocumentsChange { get; set; } = string.Empty;
        public string MeetingsChange { get; set; } = string.Empty;
        public string PipelineValueChange { get; set; } = string.Empty;
        public string ActiveCasesChange { get; set; } = string.Empty;
        public List<CasePipelineItemDto> Cases { get; set; } = new();
        public List<DeadlineItemDto> Deadlines { get; set; } = new();
        public List<InstitutionItemDto> Institutions { get; set; } = new();
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
