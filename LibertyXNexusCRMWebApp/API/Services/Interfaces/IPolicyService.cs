using API.DTOs.Policies;
using Shared.Models.Enums;

namespace API.Services.Interfaces
{
    public interface IPolicyService
    {
        Task<PolicyDto?> GetByIdAsync(int policyId);
        Task<PolicyDto?> GetWithDocumentsAsync(int policyId);
        Task<IEnumerable<PolicyDto>> GetCatalogueAsync();
        Task<IEnumerable<PolicyDto>> GetForClientAsync(int clientId);
        Task<IEnumerable<PolicyDto>> GetByStatusAsync(PolicyStatus status);
        Task<PolicyDto> CreateCatalogueItemAsync(CreateCataloguePolicyRequest request);
        Task<PolicyDto> CreateClientPolicyAsync(int clientId, CreateClientPolicyRequest request);
        Task<PolicyDto?> UpdateAsync(int policyId, UpdatePolicyRequest request);
        Task<PolicyDto> UpdateStatusAsync(int policyId, UpdatePolicyStatusRequest request);
        Task<bool> DeleteAsync(int policyId);
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
