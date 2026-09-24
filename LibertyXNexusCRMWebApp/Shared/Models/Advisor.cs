using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Shared.Models
{
    /// <summary>
    /// The financial advisor 
    /// </summary>
    public class Advisor
    {
        [Key]
        public int AdvisorId { get; set; }

        [Required, MaxLength(100)]
        public string IdentityProviderSubjectId { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string FirstName { get; set; }

        [Required, MaxLength(100)]
        public string LastName { get; set; }

        [Required, MaxLength(256), EmailAddress]
        public string Email { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [Required, MaxLength(200)]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Client> Clients { get; set; } = new List<Client>();
        public ICollection<ClientQuery> Queries { get; set; } = new List<ClientQuery>();
        public ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}
