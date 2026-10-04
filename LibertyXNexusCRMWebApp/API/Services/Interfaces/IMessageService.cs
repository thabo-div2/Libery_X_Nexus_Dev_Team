using API.DTOs.Messages;

namespace API.Services.Interfaces
{
    public interface IMessageService 
    {
        Task<IEnumerable<MessageDto>> GetConversationForClientAsync(int clientId);
        Task<IEnumerable<MessageDto>> GetConversationForAdvisorAsync(int advisorId, int clientId);
        Task<IEnumerable<ConversationSummaryDto>> GetConversationsForAdvisorAsync(int advisorId);
        Task<MessageDto> SendAsync(SendMessageRequest request, bool fromAdvisor);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
