using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Shared.Models
{
    /// <summary>
    /// The financial advisor 
    /// </summary>
    public class Advisor
    {
        public int AdvisorId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Client> Clients { get; set; } = new List<Client>();
        public ICollection<ClientQuery> Queries { get; set; } = new List<ClientQuery>();
        public ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}
