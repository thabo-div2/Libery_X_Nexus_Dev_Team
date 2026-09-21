using API.DTOs.Clients;
using Shared.Models.Enums;

namespace API.Services.Interfaces
{
    public interface IClientService
    {
        Task<ClientDetailDto?> GetByIdAsync(int clientId);
        Task<IEnumerable<ClientListItemDto>> SearchAsync(string? searchTerm, ClientStatus? status, int? advisorId);
        Task<ClientDetailDto> CreateAsync(CreateClientRequest request);
        Task<ClientDetailDto?> UpdateAsync(int clientId, UpdateClientRequest request);
        Task<bool> DeleteAsync(int clientId);
    }
}
