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
    }
}
