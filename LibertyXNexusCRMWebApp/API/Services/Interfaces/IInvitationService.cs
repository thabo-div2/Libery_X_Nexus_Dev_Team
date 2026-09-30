using static API.Services.Implementations.InvitationService;

namespace API.Services.Interfaces
{
    public interface IInvitationService
    {
        Task<InvitationResult> CreateInvitation(CreateInvitationRequest request, int advisorId);
        Task<InvitationDetails> Validate(string token);
    }
}
