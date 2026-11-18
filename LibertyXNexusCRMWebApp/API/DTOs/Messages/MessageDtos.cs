namespace API.DTOs.Messages
{
    public record MessageDto(
    int Id,
    int ClientId,
    int AdvisorId,
    bool FromAdvisor,
    string Text,
    DateTime SentAt);

    public record ConversationSummaryDto(
        int ClientId,
        string ClientName,
        string? LastMessage,
        DateTime? LastMessageAt,
        int UnreadCount = 0);

    public record SendMessageRequest(
        int ClientId,
        int AdvisorId,
        string Text);
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
