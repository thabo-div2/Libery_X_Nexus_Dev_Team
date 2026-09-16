using Shared.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Shared.Models
{
    public class AuditLog
    {
        [Key]
        public long AuditLogId { get; set; }

        public int? UserId { get; set; }

        public UserRole UserRole { get; set; }

        public AuditActionType ActionType { get; set; }

        [Required, MaxLength(100)]
        public string EntityAffected { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        [MaxLength(2000)]
        public string? Details { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
