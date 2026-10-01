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

        public DateTime? DateOfBirth { get; set; }
        public string? ResidentialAddress { get; set; }
        public string? MaritalStatus { get; set; }
        public int? Dependants { get; set; }

        public string? EmploymentStatus { get; set; }
        public string? Occupation { get; set; }
        public string? Employer { get; set; }
        public decimal? GrossMonthlyIncome { get; set; }
        public decimal? NetMonthlyIncome { get; set; }
        public decimal? MonthlyExpenses { get; set; }
        public string? SourceOfFunds { get; set; }
        public string? TaxNumber { get; set; }
        public decimal? PropertyValue { get; set; }
        public decimal? ExistingInvestments { get; set; }
        public decimal? RetirementSavings { get; set; }
        public decimal? OutstandingDebt { get; set; }
        public string? PrimaryGoal { get; set; }
        public int? InvestmentHorizonYears { get; set; }

        public bool PopiaConsent { get; set; }
        public DateTime? PopiaConsentAt { get; set; }
    }
}
