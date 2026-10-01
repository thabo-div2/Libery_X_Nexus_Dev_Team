namespace API.DTOs.Clients
{
    public class ClientDetailDto
    {
        public int ClientId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? Phone { get; set; }
        public string? IdentificationNumber { get; set; }
        public string? RiskProfile { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? AdvisorId { get; set; }
        public string? AdvisorName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
