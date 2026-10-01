using API.DTOs.Invitations;
using static API.Services.Implementations.InvitationService;

namespace API.Services.Interfaces
{
    public interface IInvitationService
    {
        Task<InvitationResponse> CreateAsync(int advisorId, string email);
        Task<InvitationValidationResponse> ValidateAsync(string token);
    }
}
