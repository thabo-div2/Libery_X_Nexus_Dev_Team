using API.DTOs.Messages;

namespace API.Services.Interfaces
{
    public interface IMessageService 
    {
        Task<IEnumerable<MessageDto>> GetConversationForClientAsync(int clientId, int? latest = null);
        Task<IEnumerable<MessageDto>> GetConversationForAdvisorAsync(int advisorId, int clientId, int? latest = null);
        Task<IEnumerable<ConversationSummaryDto>> GetConversationsForAdvisorAsync(int advisorId);
        Task<MessageDto> SendAsync(SendMessageRequest request, bool fromAdvisor);

        /// <summary>How many messages are waiting to be read by this advisor (forAdvisor) or this client.</summary>
        Task<int> GetUnreadCountAsync(bool forAdvisor, int id);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
