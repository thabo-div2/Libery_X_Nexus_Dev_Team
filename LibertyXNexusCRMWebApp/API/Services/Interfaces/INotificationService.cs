using API.DTOs.Notifications;
using Shared.Models.Enums;

namespace API.Services.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationFeedDto> GetFeedForAdvisorAsync(int advisorId);
        Task<int> GetUnreadCountForAdvisorAsync(int advisorId);
        Task<bool> MarkAsReadForAdvisorAsync(int notificationId, int advisorId);
        Task MarkAllAsReadForAdvisorAsync(int advisorId);
        Task NotifyAdvisorAsync(int advisorId, NotificationType type, string message, string? linkUrl = null, int? clientId = null);

        Task<NotificationFeedDto> GetFeedForClientAsync(int clientId);
        Task<int> GetUnreadCountForClientAsync(int clientId);
        Task<bool> MarkAsReadForClientAsync(int notificationId, int clientId);
        Task MarkAllAsReadForClientAsync(int clientId);
        Task NotifyClientAsync(int clientId, NotificationType type, string message, string? linkUrl = null);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
