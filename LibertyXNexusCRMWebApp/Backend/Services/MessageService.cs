using System.Text.Json;
using Shared.Models;

namespace Backend.Services
{
    public record ChatMessage(int Id, int ClientId, int AdvisorId, bool FromAdvisor, string Text, DateTime SentAt);
    public record SendMessageRequest(int ClientId, int AdvisorId, bool FromAdvisor, string Text);
    public record ConversationSummary(int ClientId, string ClientName, string? LastMessage, DateTime? LastMessageAt);

    public class MessageService
    {
        private readonly string _messagesPath;
        private readonly string _clientsPath;

        public MessageService(IWebHostEnvironment env)
        {
            var dataDirectory = Path.Combine(env.ContentRootPath, "Data");
            Directory.CreateDirectory(dataDirectory);
            _messagesPath = Path.Combine(dataDirectory, "messages.json");
            _clientsPath = Path.Combine(dataDirectory, "clients.json");
        }

        public async Task<List<ChatMessage>> GetConversationAsync(int clientId)
        {
            var messages = await ReadAsync<ChatMessage>(_messagesPath);
            return messages.Where(m => m.ClientId == clientId).OrderBy(m => m.SentAt).ToList();
        }

        public async Task<ChatMessage> SendAsync(SendMessageRequest request)
        {
            var messages = await ReadAsync<ChatMessage>(_messagesPath);

            var message = new ChatMessage(
                messages.Count == 0 ? 1 : messages.Max(m => m.Id) + 1,
                request.ClientId,
                request.AdvisorId,
                request.FromAdvisor,
                request.Text,
                DateTime.UtcNow);

            messages.Add(message);
            await WriteAsync(_messagesPath, messages);

            return message;
        }

        public async Task<List<ConversationSummary>> GetConversationsForAdvisorAsync(int advisorId)
        {
            var clients = await ReadAsync<Client>(_clientsPath);
            var messages = await ReadAsync<ChatMessage>(_messagesPath);

            return clients
                .Where(c => c.AdvisorId == advisorId)
                .Select(c =>
                {
                    var last = messages.Where(m => m.ClientId == c.ClientId).OrderByDescending(m => m.SentAt).FirstOrDefault();
                    return new ConversationSummary(c.ClientId, $"{c.FirstName} {c.LastName}", last?.Text, last?.SentAt);
                })
                .ToList();
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
