namespace API.DTOs.Clients
{
    public class UpdateClientRequest
    {
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(30), Phone]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string? IdentificationNumber { get; set; }

        [MaxLength(100)]
        public string? RiskProfile { get; set; }
    }
}
