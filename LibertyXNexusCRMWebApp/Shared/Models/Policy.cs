using Shared.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Shared.Models
{
    /// <summary>
    /// Represents an insurance policy associated with a client.
    /// </summary>
    public class Policy
    {
        [Key]
        public int PolicyId { get; set; }

        public int? ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public Client? Client { get; set; }

        [Required, MaxLength(200)]
        public string PolicyName { get; set; } = string.Empty;
        [Required, MaxLength(150)]
        public string Provider { get; set; } = string.Empty;
        [MaxLength(2000)]
        public string? Description { get; set; }
        public PolicyStatus Status { get; set; }
        public double? PremiumAmount { get; set; }
        public double? CoverAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool IsCatalogueItem { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }


        // Navigation properties
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public Case? Case { get; set; }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
