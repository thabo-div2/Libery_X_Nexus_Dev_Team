using API.Data;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace API.Repositories.Implementations
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(dbContextFactory) { }

        public async Task<IEnumerable<Notification>> GetForClientAsync(int clientId, bool unreadOnly = false)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var notis = context.Notifications.Where(n => n.ClientId == clientId);

            if (unreadOnly) notis = notis.Where(n => !n.IsRead);

            return await notis.OrderByDescending(n => n.CreatedAt)
                            .AsNoTracking()
                            .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetForAdvisorAsync(int advisorId, bool unreadOnly = false) 
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var notis = context.Notifications.Where(n => n.AdvisorId == advisorId);

            if (unreadOnly) notis = notis.Where(n => !n.IsRead);

            return await notis.OrderByDescending(n => n.CreatedAt)
                            .AsNoTracking()
                            .ToListAsync();
        }

        public async Task<int> GetUnreadCountForClientAsync(int clientId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            return await context.Notifications
                            .CountAsync(n => n.ClientId == clientId && !n.IsRead);
        }

        public async Task<int> GetUnreadCountForAdvisorAsync(int advisorId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            return await context.Notifications
                            .CountAsync(n => n.AdvisorId == advisorId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var notification = await context.Notifications.FindAsync(notificationId);

            if (notification is null)
                throw new KeyNotFoundException($"Notification with id {notificationId} was not found.");

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        public async Task MarkAllAsReadForClientAsync(int clientId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            await context.Notifications
                    .Where(n => n.ClientId == clientId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(n => n.IsRead, true)
                        .SetProperty(n => n.ReadAt, DateTime.UtcNow));
        }

        public async Task MarkAllAsReadForAdvisorAsync(int advisorId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            await context.Notifications
                .Where(n => n.AdvisorId == advisorId)
                .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow));
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
