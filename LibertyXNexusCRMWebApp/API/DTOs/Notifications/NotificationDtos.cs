namespace API.DTOs.Notifications
{
    public record NotificationDtos
    (int NotificationId,
        string Type,
        string Message,
        string? LinkUrl,
        bool IsRead,
        DateTime CreatedAt,
        bool IsReminder);

    public record NotificationFeedDto(
        int UnreadCount,
        IReadOnlyList<NotificationDtos> Items);
}
