using API.DTOs.Clients;
using API.Repositories.Implementations;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using Shared.Models;
using Shared.Models.Enums;

namespace API.Services.Implementations
{
    public class ClientService : IClientService
    {
        private readonly IClientRepository clientRepository_;

        public ClientService(IClientRepository clientRepository) 
        {
            clientRepository_ = clientRepository;
        }

        public async Task<ClientDetailDto?> GetByIdAsync(int clientId)
        {
            var client = await clientRepository_.GetWithDetailsAsync(clientId);
            return client is null ? null : MapToDetailDto(client);
        }

        public async Task<IEnumerable<ClientListItemDto>> SearchAsync(string? searchTerm, ClientStatus? status, int? advisorId)
        {
            var clients = await clientRepository_.SearchAsync(searchTerm, status, advisorId);
            return clients.Select(MapToListItemDto);
        }

        public async Task<ClientDetailDto> CreateAsync(CreateClientRequest request)
        {
            var existing = await clientRepository_.GetByEmailAsync(request.Email);
            if (existing != null)
            {
                throw new InvalidOperationException($"A client with email '{request.Email}' already exists");
            }

            var client = new Client
            {
                IdentityProviderSubjectId = request.IdentityProviderSubjectId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                IdentificationNumber = request.IdentificationNumber,
                RiskProfile = request.RiskProfile,
                AdvisorId = request.AdvisorId,
                Status = ClientStatus.Registered,
                CreatedAt = DateTime.UtcNow
            };

            var created = await clientRepository_.AddAsync(client);
            return MapToDetailDto(created);
        }

        public async Task<ClientDetailDto?> UpdateAsync(int clientId, UpdateClientRequest request)
        {

            var client = await clientRepository_.GetByIdAsync(clientId);

            if (client is null)
            {
                return null;
            }

            client.FirstName = request.FirstName;
            client.LastName = request.LastName;
            client.Phone = request.Phone;
            client.IdentificationNumber = request.IdentificationNumber;
            client.RiskProfile = request.RiskProfile;
            client.UpdatedAt = DateTime.UtcNow;

            await clientRepository_.UpdateAsync(client);
            return MapToDetailDto(client);
        }

        public async Task<bool> DeleteAsync(int clientId)
        {
            var exists = await clientRepository_.ExistsAsync(clientId);
            if (!exists)
            {
                return false;
            }
            await clientRepository_.DeleteAsync(clientId);
            return true;
        }

        private static ClientDetailDto MapToDetailDto(Client client) => new()
        {
            ClientId = client.ClientId,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Email = client.Email,
            Phone = client.Phone,
            IdentificationNumber = client.IdentificationNumber,
            RiskProfile = client.RiskProfile,
            AdvisorId = client.AdvisorId,
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt,
            Status = client.Status.ToString(),
            FullName = client.FullName,
        };

        private static ClientListItemDto MapToListItemDto(Client client) => new()
        {
            ClientId = client.ClientId,
            FullName = client.FullName,
            Email = client.Email,
            Status = client.Status.ToString(),
            AdvisorId = client.AdvisorId,
            CreatedAt = client.CreatedAt,
        };

    }
}
