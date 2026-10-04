using Shared.Models;
using Shared.Models.Enums;

namespace API.Repositories.Interfaces
{
    public interface IQueryRepository : IGenericRepository<ClientQuery>
    {
        Task<IEnumerable<ClientQuery>> GetByClientIdAsync(int clientId);
        Task<IEnumerable<ClientQuery>> GetByStatusAsync(QueryStatus status);
        Task<IEnumerable<ClientQuery>> GetOpenForAdvisorAsync(int advisorId);
        Task<bool> BelongsToClientAsync(int queryId, int clientId);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
