using API.Data;
using API.DTOs.Notifications;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Models.Enums;
using API.Services.Interfaces;

namespace API.Services.Implementations
{
    /// <summary>
    /// Service for managing notifications, including fetching notification feeds, marking notifications as read, and sending new notifications to advisors.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Constructor for NotificationService, initializing the notification repository and database context factory.
        /// </summary>
        /// <param name="notificationRepository"></param>
        /// <param name="contextFactory"></param>
        public NotificationService(INotificationRepository notificationRepository, IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _notificationRepository = notificationRepository;
            _contextFactory = contextFactory;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Get the notification feed for the specified advisor, including both persisted notifications and live reminders.
        /// </summary>
        /// <param name="advisorId"></param>
        /// <returns></returns>
        public async Task<NotificationFeedDto> GetFeedForAdvisorAsync(int advisorId)
        {
            var persisted = await _notificationRepository.GetForAdvisorAsync(advisorId);

            var notifications = persisted
                .Select(n => new NotificationDtos(
                    n.NotificationId,
                    n.Type.ToString(),
                    n.Message,
                    n.LinkUrl,
                    n.IsRead,
                    n.CreatedAt,
                    IsReminder: false))
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            var reminders = (await GetLiveRemindersAsync(advisorId))
                .OrderBy(r => r.CreatedAt)
                .ToList();

            var ordered = reminders.Concat(notifications).ToList();
            var unreadCount = persisted.Count(n => !n.IsRead);

            return new NotificationFeedDto(unreadCount, ordered);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Get the count of unread notifications for the specified advisor.
        /// </summary>
        /// <param name="advisorId"></param>
        /// <returns></returns>
        public async Task<int> GetUnreadCountForAdvisorAsync(int advisorId)
        {
            return await _notificationRepository.GetUnreadCountForAdvisorAsync(advisorId);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Mark a specific notification as read for the specified advisor.
        /// </summary>
        /// <param name="notificationId"></param>
        /// <param name="advisorId"></param>
        /// <returns></returns>
        public async Task<bool> MarkAsReadForAdvisorAsync(int notificationId, int advisorId)
        {
            if (notificationId <= 0) return false;

            var notification = await _notificationRepository.GetByIdAsync(notificationId);

            if (notification == null || notification.AdvisorId != advisorId)
            {
                return false;
            }

            await _notificationRepository.MarkAsReadAsync(notificationId);
            return true;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Mark all notifications as read for the specified advisor.
        /// </summary>
        /// <param name="advisorId"></param>
        /// <returns></returns>
        public async Task MarkAllAsReadForAdvisorAsync(int advisorId)
        {
            await _notificationRepository.MarkAllAsReadForAdvisorAsync(advisorId);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Notify the advisor with a new notification.
        /// </summary>
        /// <param name="advisorId"></param>
        /// <param name="type"></param>
        /// <param name="message"></param>
        /// <param name="linkUrl"></param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task NotifyAdvisorAsync(int advisorId, NotificationType type, string message, string? linkUrl = null, int? clientId = null)
        {
            var notification = new Notification
            {
                AdvisorId = advisorId,
                ClientId = null,
                Type = type,
                Message = message,
                LinkUrl = linkUrl,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Get live reminders for the advisor, including upcoming meetings and policy renewals.
        /// </summary>
        /// <param name="advisorId"></param>
        /// <returns></returns>
        private async Task<List<NotificationDtos>> GetLiveRemindersAsync(int advisorId)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();

            var now = DateTime.UtcNow;
            var reminders = new List<NotificationDtos>();

            var upcomingMeetings = await db.Meetings
                .AsNoTracking()
                .Include(m => m.Client)
                .Where(m => m.Client != null
                    && m.Client.AdvisorId == advisorId
                    && m.Status != MeetingStatus.Cancelled
                    && m.Status != MeetingStatus.Completed
                    && m.MeetingDate >= now
                    && m.MeetingDate <= now.AddHours(48))
                .OrderBy(m => m.MeetingDate)
                .ToListAsync();

            reminders.AddRange(upcomingMeetings.Select(m => new NotificationDtos(
                0,
                NotificationType.PendingAction.ToString(),
                $"Meeting with {m.Client!.FullName} on {m.MeetingDate:ddd d MMM, HH:mm}",
                "/calendar",
                IsRead: false,
                CreatedAt: m.MeetingDate,
                IsReminder: true)));

            var deadline = now.AddDays(30);

            var renewals = await db.Policies
                .AsNoTracking()
                .Include(p => p.Client)
                .Where(p => p.Client != null
                    && p.Client.AdvisorId == advisorId
                    && !p.IsCatalogueItem
                    && p.Status != PolicyStatus.Cancelled
                    && p.Status != PolicyStatus.Matured
                    && p.EndDate != null
                    && p.EndDate >= now
                    && p.EndDate <= deadline)
                .OrderBy(p => p.EndDate)
                .ToListAsync();

            reminders.AddRange(renewals.Select(p => new NotificationDtos(
                0,
                NotificationType.PendingAction.ToString(),
                $"{p.PolicyName} for {p.Client!.FullName} renews on {p.EndDate:d MMM yyyy}",
                "/clients",
                IsRead: false,
                CreatedAt: p.EndDate!.Value,
                IsReminder: true)));

            return reminders;
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
