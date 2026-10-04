using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Clients
{
    public class CreateClientRequest
    {
        [Required, MaxLength(100)]
        public string? IdentityProviderSubjectId { get; set; }

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, MaxLength(100), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(30), Phone]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string? IdentificationNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(300)]
        public string? ResidentialAddress { get; set; }

        [MaxLength(60)]
        public string? MaritalStatus { get; set; }

        public int? Dependants { get; set; }

        [MaxLength(40)]
        public string? EmploymentStatus { get; set; }

        [MaxLength(100)]
        public string? Occupation { get; set; }
        [MaxLength(150)]
        public string? Employer { get; set; }

        public decimal? GrossMonthlyIncome { get; set; }
        public decimal? NetMonthlyIncome { get; set; }
        public decimal? MonthlyExpense { get; set; }

        [MaxLength(60)]
        public string? SourceOfFunds { get; set; }

        [MaxLength(30)]
        public string? TaxNumber { get; set; }

        public decimal? PropertyValue { get; set; }
        public decimal? ExistingInvestments { get; set; }
        public decimal? RetirmentSavings { get; set; }
        public decimal? OutstandingDebt { get; set; }

        [MaxLength(60)]
        public string? PrimaryGoal { get; set; }

        public int? InvestmentHorizonYears { get; set; }

        [MaxLength(100)]
        public string RiskProfile { get; set; }
        [Required]
        public bool PopiaConsent { get; set; }
        public int? AdvisorId { get; set; }

    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
