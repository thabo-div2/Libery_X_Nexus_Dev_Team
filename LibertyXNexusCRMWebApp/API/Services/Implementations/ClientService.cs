using API.DTOs.Clients;
using API.Repositories.Interfaces;
using API.Services.Interfaces;

namespace API.Services.Implementations
{
    public class ClientService : IClientService
    {
        private readonly IClientRepository _clientRepository;

        public ClientService(IClientRepository clientRepository) 
        {
          _clientRepository = clientRepository;
        }

        public async Task<ClientDetailDto?> GetByIdAsync(int clientId) { 
            var client = await _clientRepository.GetWithDetailsAsync(clientId);
            return client is null ? null: MapToDetailDto(client);
        }

        public async Task<IEnumerable<ClientListItemDto>> SearchAsync(string? searchTerm, ClientStatus? status, int? advisorId)
        { 
        var clients = await _clientRepository.SearchAsync(searchTerm, status, advisorId);
            return clients.Select(MapToListItemDto);
        }

        public async Task<ClientDetailDto> CreateAsync(CreateClientRequest request)
        { 
        var existing = await _clientRepository.GetByEmailAsync(request.Email);
            if (existing != null) { 
            throw new InvalidOperationException($"A client with email '{request.Email}' already exists")
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

            var created = await _clientRepository.AddAsync(client);
            return MapToDetailDto(created);
        }

        public async Task<ClientDetailDto?> UpdateAsync(int clientId, UpdateClientRequest request) 
        {
        var client = await _clientRepository.GetByIdAsync(clientId);
            if (client != null) {
                return null;
            }

            client.FirstName = request.FirstName;
            client.LastName = request.LastName;
            client.Phone = request.Phone;
            client.IdentificationNumber = request.IdentificationNumber;
            client.RiskProfile = request.RiskProfile;
            client.UpdatedAt = DateTime.UtcNow;

            await _clientRepository.UpdateAsync(client);
            return MapToDetailDto(client);
        }

        public async Task<bool> DeleteAsync(int clientId)
        {
            var exists = await _clientRepository.ExistsAsync(clientId);
            if (exists != null)
            {
                return false;
            }

            await _clientRepository.DeleteAsync(clientId);
            return true;
        }

        private static ClientDetailDto MapToDetailDto(Client client) => new()
        {
            ClientId = client.ClientId,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Phone = client.Phone,
            IdentificationNumber = client.IdentificationNumber,
            RiskProfile = client.RiskProfile,
            AdvisorId = client.AdvisorId,
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt,
            Status = client.Status,
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
