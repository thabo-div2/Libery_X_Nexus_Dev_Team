using Shared.Models;

namespace API.Repositories.Interfaces
{
    public interface IAdvisorRepository : IGenericRepository<Advisor>
    {
        Task<Advisor?> GetEmailAsync(string email);
        Task<Advisor?> GetByIdentitySubjectIdAsync(string subjectId);
        Task<Advisor?> GetWithClientAsync(int advisorId);
    }
}
