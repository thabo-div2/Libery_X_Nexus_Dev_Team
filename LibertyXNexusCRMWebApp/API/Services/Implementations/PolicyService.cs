using API.DTOs.Policies;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using Shared.Models.Enums;

namespace API.Services.Implementations
{
    /// <summary>
    /// Service class for managing policies, including retrieval, creation, updating, and deletion of policy records.
    /// </summary>
    public class PolicyService : IPolicyService
    {
        private readonly IPolicyRepository policyRepository_;
        private readonly IClientRepository clientRepository_;
        private readonly ICaseRepository caseRepository_;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyService"/> class with the specified repositories.
        /// </summary>
        /// <param name="policyRepository"></param>
        /// <param name="clientRepository"></param>
        /// <param name="caseRepository"></param>
        public PolicyService(IPolicyRepository policyRepository, IClientRepository clientRepository, ICaseRepository caseRepository)
        {
            policyRepository_ = policyRepository;
            clientRepository_ = clientRepository;
            caseRepository_ = caseRepository;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves a policy by its unique identifier.
        /// </summary>
        /// <param name="policyId"></param>
        /// <returns></returns>
        public async Task<PolicyDto?> GetByIdAsync(int policyId)
        {
            var policy = await policyRepository_.GetByIdAsync(policyId);
            return policy is null ? null : MapToDto(policy);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves a policy along with its associated documents by the policy's unique identifier.
        /// </summary>
        /// <param name="policyId"></param>
        /// <returns></returns>
        public async Task<PolicyDto?> GetWithDocumentsAsync(int policyId)
        {
            var policy = await policyRepository_.GetWithDocumentAsync(policyId);
            return policy is null ? null : MapToDto(policy);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves all policies in the catalogue.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<PolicyDto>> GetCatalogueAsync()
        {
            var policies = await policyRepository_.GetCatalogoueAsync();
            return policies.Select(MapToDto);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves all policies associated with a specific client by the client's unique identifier.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<PolicyDto>> GetForClientAsync(int clientId)
        {
            var policies = await policyRepository_.GetByClientIdAsync(clientId);
            return policies.Select(MapToDto);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves all policies that match a specific status.
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public async Task<IEnumerable<PolicyDto>> GetByStatusAsync(PolicyStatus status)
        {
            var policies = await policyRepository_.GetByStatusAsync(status);
            return policies.Select(MapToDto);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Creates a new policy in the catalogue based on the provided request data.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PolicyDto> CreateCatalogueItemAsync(CreateCataloguePolicyRequest request)
        {
            var policy = new Policy
            {
                PolicyName = request.PolicyName,
                Provider = request.Provider,
                Description = request.Description,
                PremiumAmount = request.PremiumAmount,
                CoverAmount = request.CoverAmount,
                IsCatalogueItem = true,
                ClientId = null,
                Status = PolicyStatus.Active, 
                CreatedAt = DateTime.UtcNow
            };

            var created = await policyRepository_.AddAsync(policy);
            return MapToDto(created);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Creates a new policy for a specific client based on the provided request data.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public async Task<PolicyDto> CreateClientPolicyAsync(int clientId, CreateClientPolicyRequest request)
        {
            var clientExists = await clientRepository_.ExistsAsync(clientId);
            if (!clientExists)
            {
                throw new KeyNotFoundException($"Client {clientId} was not found.");
            }

            if (request.EndDate.HasValue && request.StartDate.HasValue && request.EndDate < request.StartDate)
            {
                throw new ArgumentException("End date cannot be before the start date.");
            }

            var policy = new Policy
            {
                ClientId = clientId,
                PolicyName = request.PolicyName,
                Provider = request.Provider,
                Description = request.Description,
                PremiumAmount = request.PremiumAmount,
                CoverAmount = request.CoverAmount,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsCatalogueItem = false,
                Status = PolicyStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var created = await policyRepository_.AddAsync(policy);

            await caseRepository_.AddAsync(new Case
            {
                PolicyId = created.PolicyId,
                Status = CaseStatus.InProgress,
                TargetSubmissionDate = request.TargetSubmissionDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            return MapToDto(created);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Updates an existing policy's details based on the provided request data.
        /// </summary>
        /// <param name="policyId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<PolicyDto?> UpdateAsync(int policyId, UpdatePolicyRequest request)
        {
            var policy = await policyRepository_.GetByIdAsync(policyId);
            if (policy is null)
            {
                return null;
            }

            if (request.EndDate.HasValue && request.StartDate.HasValue && request.EndDate < request.StartDate)
            {
                throw new ArgumentException("End date cannot be before the start date.");
            }

            policy.PolicyName = request.PolicyName;
            policy.Provider = request.Provider;
            policy.Description = request.Description;
            policy.PremiumAmount = request.PremiumAmount;
            policy.CoverAmount = request.CoverAmount;
            policy.StartDate = request.StartDate;
            policy.EndDate = request.EndDate;
            policy.UpdatedAt = DateTime.UtcNow;

            await policyRepository_.UpdateAsync(policy);
            return MapToDto(policy);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Updates the status of an existing policy based on the provided request data.
        /// </summary>
        /// <param name="policyId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<PolicyDto> UpdateStatusAsync(int policyId, UpdatePolicyStatusRequest request)
        {
            var policy = await policyRepository_.GetByIdAsync(policyId)
                ?? throw new KeyNotFoundException($"Policy {policyId} was not found.");

            if (policy.Status is PolicyStatus.Cancelled or PolicyStatus.Matured)
            {
                throw new InvalidOperationException($"A policy with status '{policy.Status}' cannot be changed further.");
            }

            policy.Status = request.NewStatus;
            policy.UpdatedAt = DateTime.UtcNow;

            await policyRepository_.UpdateAsync(policy);
            return MapToDto(policy);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Deletes an existing policy by its unique identifier.
        /// </summary>
        /// <param name="policyId"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(int policyId)
        {
            var exists = await policyRepository_.ExistsAsync(policyId);
            if (!exists)
            {
                return false;
            }

            await policyRepository_.DeleteAsync(policyId);
            return true;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Maps a Policy entity to a PolicyDto for data transfer purposes.
        /// </summary>
        /// <param name="policy"></param>
        /// <returns></returns>
        private static PolicyDto MapToDto(Policy policy) => new()
        {
            PolicyId = policy.PolicyId,
            ClientId = policy.ClientId,
            PolicyName = policy.PolicyName,
            Provider = policy.Provider,
            Description = policy.Description,
            Status = policy.Status.ToString(),
            PremiumAmount = policy.PremiumAmount,
            CoverAmount = policy.CoverAmount,
            StartDate = policy.StartDate,
            EndDate = policy.EndDate,
            IsCatalogueItem = policy.IsCatalogueItem,
            CreatedAt = policy.CreatedAt,
            UpdatedAt = policy.UpdatedAt
        };
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
