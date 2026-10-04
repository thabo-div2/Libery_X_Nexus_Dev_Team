using Shared.Models;
using Shared.Models.Enums;

namespace API.Repositories.Interfaces
{
    public interface IClientRepository : IGenericRepository<Client>
    {
        Task<Client?> GetWithDetailsAsync(int clientId);
        Task<Client?> GetByEmailAsync(string email);
        Task<Client?> GetByIdentitySubjectIdAsync(string subjectId);
        Task<IEnumerable<Client>> GetByAdvisorAsync(int advisorId);
        Task<IEnumerable<Client>> SearchAsync(string? searchTerm = null, ClientStatus? status = null, int? advisorId = null);
        Task<IEnumerable<Client>> GetDueForReviewAsync(DateTime reviewCutoff);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
