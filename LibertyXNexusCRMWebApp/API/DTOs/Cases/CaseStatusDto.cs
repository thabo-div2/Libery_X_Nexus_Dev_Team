namespace API.DTOs.Cases
{
    public class CaseStatusDto
    {
        public int CaseId { get; set; }
        public int PolicyId { get; set; }
        public string? PolicyName { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
