using Shared.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Shared.Models
{
    public class Client
    {
        [Key]
        public int ClientId { get; set; }

        public string IdentityProviderSubjectId { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, MaxLength(256), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(30), Phone]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string? IdentityNumber { get; set; }

        [MaxLength(100)]
        public string? RiskProfile { get; set; }

        public ClientStatus Status { get; set; } = ClientStatus.Registered;

        public int? AdvisorId { get; set; }

        [ForeignKey(nameof(AdvisorId))]
        public Advisor? Advisor { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdateAt { get; set; }

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
