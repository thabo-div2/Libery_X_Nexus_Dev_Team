using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Shared.Models;
using Shared.Models.Enums;

namespace Backend.Services
{
    public record LoginRequest(string Email, string Password);
    public record RegisterRequest(string FirstName, string LastName, string Email, string? Phone, string? IdentityNumber, string Password, string Token);
    public record AuthResult(bool Success, string Message, string? Role, int? Id, string? FirstName, string? LastName, string? Email);

    public class AuthService
    {
        private readonly string _advisorsPath;
        private readonly string _clientsPath;
        private readonly InvitationService _invitationService;

        public AuthService(IWebHostEnvironment env, InvitationService invitationService)
        {
            var dataDirectory = Path.Combine(env.ContentRootPath, "Data");
            Directory.CreateDirectory(dataDirectory);
            _advisorsPath = Path.Combine(dataDirectory, "advisors.json");
            _clientsPath = Path.Combine(dataDirectory, "clients.json");
            _invitationService = invitationService;
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request)
        {
            var hash = Hash(request.Password);

            var advisors = await ReadAsync<Advisor>(_advisorsPath);
            var advisor = advisors.FirstOrDefault(a => a.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase));
            if (advisor is not null)
            {
                return advisor.PasswordHash == hash
                    ? new AuthResult(true, "Login successful", nameof(UserRole.FinancialAdviser), advisor.AdvisorId, advisor.FirstName, advisor.LastName, advisor.Email)
                    : new AuthResult(false, "Incorrect password", null, null, null, null, null);
            }

            var clients = await ReadAsync<Client>(_clientsPath);
            var client = clients.FirstOrDefault(c => c.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase));
            if (client is not null)
            {
                return client.PasswordHash == hash
                    ? new AuthResult(true, "Login successful", nameof(UserRole.RegisteredClient), client.ClientId, client.FirstName, client.LastName, client.Email)
                    : new AuthResult(false, "Incorrect password", null, null, null, null, null);
            }

            return new AuthResult(false, "Account not found", null, null, null, null, null);
        }

        public async Task<AuthResult> RegisterAsync(RegisterRequest request)
        {
            var invitation = await _invitationService.ValidateAsync(request.Token);
            if (!invitation.Valid)
            {
                return new AuthResult(false, invitation.Message, null, null, null, null, null);
            }

            if (!invitation.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
            {
                return new AuthResult(false, "This link was issued for a different email address", null, null, null, null, null);
            }

            var advisors = await ReadAsync<Advisor>(_advisorsPath);
            var clients = await ReadAsync<Client>(_clientsPath);

            var emailTaken = advisors.Any(a => a.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
                || clients.Any(c => c.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase));

            if (emailTaken)
            {
                return new AuthResult(false, "An account with this email already exists", null, null, null, null, null);
            }

            var client = new Client
            {
                ClientId = clients.Count == 0 ? 1 : clients.Max(c => c.ClientId) + 1,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                IdentityNumber = request.IdentityNumber,
                PasswordHash = Hash(request.Password),
                Status = ClientStatus.Registered,
                AdvisorId = invitation.AdvisorId
            };

            clients.Add(client);
            await WriteAsync(_clientsPath, clients);
            await _invitationService.RedeemAsync(request.Token, client.ClientId);

            return new AuthResult(true, "Account created", nameof(UserRole.RegisteredClient), client.ClientId, client.FirstName, client.LastName, client.Email);
        }

        private static string Hash(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes);
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

        private static async Task WriteAsync<T>(string path, List<T> data)
        {
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, data, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
