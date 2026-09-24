using System.Text.Json;
using Shared.Models;

namespace Backend.Services
{
    public record ClientProfile(int ClientId, string FirstName, string LastName, string Email, string? Phone, string? IdentityNumber, string? RiskProfile, string Status, DateTime CreatedAt);

    public class ClientService
    {
        private readonly string _clientsPath;

        public ClientService(IWebHostEnvironment env)
        {
            var dataDirectory = Path.Combine(env.ContentRootPath, "Data");
            Directory.CreateDirectory(dataDirectory);
            _clientsPath = Path.Combine(dataDirectory, "clients.json");
        }

        public async Task<List<ClientProfile>> SearchAsync(string? search)
        {
            var clients = await ReadAsync<Client>(_clientsPath);

            var matches = string.IsNullOrWhiteSpace(search)
                ? clients
                : clients.Where(c =>
                    c.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    c.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    c.Status.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            return matches.Select(ToProfile).ToList();
        }

        public async Task<ClientProfile?> GetByIdAsync(int id)
        {
            var clients = await ReadAsync<Client>(_clientsPath);
            var client = clients.FirstOrDefault(c => c.ClientId == id);
            return client is null ? null : ToProfile(client);
        }

        private static ClientProfile ToProfile(Client client)
        {
            return new ClientProfile(client.ClientId, client.FirstName, client.LastName, client.Email, client.Phone, client.IdentityNumber, client.RiskProfile, client.Status.ToString(), client.CreatedAt);
        }

        private static async Task<List<T>> ReadAsync<T>(string path)
        {
            if (!File.Exists(path))
            {
                return new List<T>();
            }

            await using var stream = File.OpenRead(path);
            var data = await JsonSerializer.DeserializeAsync<List<T>>(stream);
            return data ?? new List<T>();
        }
    }
}
