using Shared.Models;
using Shared.Models.Enums;

namespace API.Repositories.Interfaces
{
    public interface IInvitationRepository : IGenericRepository<Invitation>
    {
        Task<Invitation?> GetByTokenAsync(string token);
        Task<Invitation?> GetValidByTokenAsync(string token);
        Task<IEnumerable<Invitation>> GetByAdvisorAsync(int advisorId);
        Task<IEnumerable<Invitation>> GetPendingAsync();
        Task MarkRedeemedAsync(int invitationId, int clientId);
        Task<int> ExpireOverdueAsync();
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
