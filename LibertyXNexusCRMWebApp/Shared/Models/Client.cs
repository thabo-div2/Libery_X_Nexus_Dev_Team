using Shared.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Shared.Models
{
    /// <summary>
    /// Represents a client in the financial advisory system, including personal details, financial information, and relationships to advisors, policies, meetings, documents, queries, and notifications.
    /// </summary>
    public class Client
    {
        [Key]
        public int ClientId { get; set; }

        [MaxLength(100)]
        public string? IdentityProviderSubjectId { get; set; }

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, MaxLength(256), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(30), Phone]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string? IdentificationNumber { get; set; }

        [MaxLength(100)]
        public string? RiskProfile { get; set; }
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
        public String? Employer { get; set; }

        public decimal? GrossMonthlyIncome { get; set; }
        
        public decimal? NetMonthlyIncome { get; set; }
        
        public decimal? MonthlyExpenses { get; set; }

        [MaxLength(60)]
        public string? SourceOfFunds { get; set; }
        [MaxLength(30)]
        public string? TaxNumber { get; set; }
        
        public decimal? PropertyValue { get; set; }
        
        public decimal? ExistingInvestments { get; set; }
        
        public decimal? RetirementSavings { get; set; }
        
        public decimal? OutstandingDebt { get; set; }
        [MaxLength(60)]
        public string? PrimaryGoal { get; set; }
        public int? InvestmentHorizonYears { get; set; }

        public bool PopiaConsent { get; set; }
        public DateTime? PopiaConsentAt { get; set; }

        public ClientStatus Status { get; set; } = ClientStatus.Registered;

        public int? AdvisorId { get; set; }

        [ForeignKey(nameof(AdvisorId))]
        public Advisor? Advisor { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Policy> Policies { get; set; } = new List<Policy>();
        public ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public ICollection<ClientQuery> Queries { get; set; } = new List<ClientQuery>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
