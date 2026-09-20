using Shared.Models;

namespace API.Repositories.Interfaces
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetForClientAsync(int clientId, bool unreadOnly = false);
        Task<IEnumerable<Notification>> GetForAdvisorAsync(int advisorId, bool unreadOnly = false);
        Task<int> GetUnreadCountForClientAsync(int clientId);
        Task<int> GetUnreadCountForAdvisorAsync(int advisorId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadForClientAsync(int clientId);
    }
}
