using System.Text.Json;
using Shared.Models;

namespace Backend.Services
{
    public record ClientProfile(int ClientId, string FirstName, string LastName, string Email, string? Phone, string? IdentityNumber, string? RiskProfile, string Status, DateTime CreatedAt, int? AdvisorId, string? AdvisorName);

    public class ClientService
    {
        private readonly string _clientsPath;
        private readonly string _advisorsPath;

        public ClientService(IWebHostEnvironment env)
        {
            var dataDirectory = Path.Combine(env.ContentRootPath, "Data");
            Directory.CreateDirectory(dataDirectory);
            _clientsPath = Path.Combine(dataDirectory, "clients.json");
            _advisorsPath = Path.Combine(dataDirectory, "advisors.json");
        }

        public async Task<List<ClientProfile>> SearchAsync(string? search, int? advisorId = null)
        {
            var clients = await ReadAsync<Client>(_clientsPath);

            if (advisorId is not null)
            {
                clients = clients.Where(c => c.AdvisorId == advisorId.Value).ToList();
            }

            var matches = string.IsNullOrWhiteSpace(search)
                ? clients
                : clients.Where(c =>
                    c.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    c.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    c.Status.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            var advisors = await ReadAsync<Advisor>(_advisorsPath);
            return matches.Select(c => ToProfile(c, advisors)).ToList();
        }

        public async Task<ClientProfile?> GetByIdAsync(int id)
        {
            var clients = await ReadAsync<Client>(_clientsPath);
            var client = clients.FirstOrDefault(c => c.ClientId == id);
            if (client is null)
            {
                return null;
            }

            var advisors = await ReadAsync<Advisor>(_advisorsPath);
            return ToProfile(client, advisors);
        }

        private static ClientProfile ToProfile(Client client, List<Advisor> advisors)
        {
            var advisor = advisors.FirstOrDefault(a => a.AdvisorId == client.AdvisorId);
            return new ClientProfile(client.ClientId, client.FirstName, client.LastName, client.Email, client.Phone, client.IdentityNumber, client.RiskProfile, client.Status.ToString(), client.CreatedAt, client.AdvisorId, advisor?.FullName);
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
