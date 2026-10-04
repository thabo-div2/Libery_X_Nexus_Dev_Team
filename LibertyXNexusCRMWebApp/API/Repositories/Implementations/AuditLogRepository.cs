using API.Data;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Models.Enums;

namespace API.Repositories.Implementations
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

        public AuditLogRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory) 
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<AuditLog> AddAsync(AuditLog entry)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            if (entry is null) throw new ArgumentNullException(nameof(entry));

            await context.AuditLogs.AddAsync(entry);
            await context.SaveChangesAsync();
            return entry;
        }

        public async Task LogAsync(
            int? userId,
            UserRole userRole,
            AuditActionType actionType,
            string entityAffected,
            int? entityId = null,
            string? details = null,
            string? ipAddress = null
            )
        {
            await AddAsync(new AuditLog
            {
                UserId = userId,
                UserRole = userRole,
                ActionType = actionType,
                EntityAffected = entityAffected,
                EntityId = entityId,
                Details = details,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            return await context.AuditLogs
                .Where(a => a.Timestamp >= from && a.Timestamp <= to)
                .OrderByDescending(a => a.Timestamp)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityAffected, int entityId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            return await context.AuditLogs
                .Where(a => a.EntityAffected == entityAffected && a.EntityId == entityId)
                .OrderByDescending(a => a.Timestamp)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetByUserAsync(int userId, UserRole userRole)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            return await context.AuditLogs
                .Where(a => a.UserId == userId && a.UserRole == userRole)
                .OrderByDescending(a => a.Timestamp)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 100)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            return await context.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
