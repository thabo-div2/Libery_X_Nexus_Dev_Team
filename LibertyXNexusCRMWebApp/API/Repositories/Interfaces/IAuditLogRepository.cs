using Shared.Models;
using Shared.Models.Enums;

namespace API.Repositories.Interfaces
{
    public interface IAuditLogRepository
    {
        Task<AuditLog> AddAsync(AuditLog entry);
        Task LogAsync(
            int? userId,
            UserRole userRole,
            AuditActionType actionType,
            string entityAffected,
            int? entityId = null,
            string? details = null,
            string? ipAddress = null
            );
        Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to);
        Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityAffected, int entityId);
        Task<IEnumerable<AuditLog>> GetByUserAsync(int userId, UserRole userRole);
        Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 100);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
