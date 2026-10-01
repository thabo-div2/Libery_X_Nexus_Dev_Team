using API.Data;
using API.DTOs.Messages;
using API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace API.Services.Implementations
{
    public class MessageService : IMessageService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public MessageService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<IEnumerable<MessageDto>> GetConversationForClientAsync(int clientId)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();

            return await db.Messages
                .AsNoTracking()
                .Where(m => m.ClientId == clientId)
                .OrderBy(m => m.SentAt)
                .Select(m => new MessageDto(
                    m.MessageId,
                    m.ClientId,
                    m.AdvisorId,
                    m.FromAdvisor,
                    m.Text,
                    m.SentAt))
                .ToListAsync();
        }

        public async Task<IEnumerable<MessageDto>> GetConversationForAdvisorAsync(int advisorId, int clientId)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();

            return await db.Messages
                .AsNoTracking()
                .Where(m => m.ClientId == clientId && m.AdvisorId == advisorId)
                .OrderBy(m => m.SentAt)
                .Select(m => new MessageDto(
                    m.MessageId,
                    m.ClientId,
                    m.AdvisorId,
                    m.FromAdvisor,
                    m.Text,
                    m.SentAt))
                .ToListAsync();
        }

        public async Task<IEnumerable<ConversationSummaryDto>> GetConversationsForAdvisorAsync(int advisorId)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();

            var messages = await db.Messages
                .AsNoTracking()
                .Where(m => m.AdvisorId == advisorId)
                .Include(m => m.Client)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            return messages
                .GroupBy(m => m.ClientId)
                .Select(g =>
                {
                    var latest = g.First();
                    return new ConversationSummaryDto(
                        latest.ClientId,
                        latest.Client.FullName,
                        latest.Text,
                        latest.SentAt);
                })
                .OrderByDescending(c => c.LastMessageAt)
                .ToList();
        }

        public async Task<MessageDto> SendAsync(SendMessageRequest request, bool fromAdvisor)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                throw new ArgumentException("Message text is required.");

            if (request.Text.Length > 4000)
                throw new ArgumentException("Message text cannot exceed 4000 characters.");

            await using var db = await _contextFactory.CreateDbContextAsync();

            var client = await db.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClientId == request.ClientId);

            if (client is null)
                throw new KeyNotFoundException($"Client {request.ClientId} was not found.");

            if (client.AdvisorId != request.AdvisorId)
                throw new InvalidOperationException("The client is not assigned to this advisor.");

            var advisorExists = await db.Advisors
                .AsNoTracking()
                .AnyAsync(a => a.AdvisorId == request.AdvisorId);

            if (!advisorExists)
                throw new KeyNotFoundException($"Advisor {request.AdvisorId} was not found.");

            var message = new Message
            {
                ClientId = request.ClientId,
                AdvisorId = request.AdvisorId,
                FromAdvisor = fromAdvisor,
                Text = request.Text.Trim(),
                SentAt = DateTime.UtcNow
            };

            db.Messages.Add(message);
            await db.SaveChangesAsync();

            return new MessageDto(
                message.MessageId,
                message.ClientId,
                message.AdvisorId,
                message.FromAdvisor,
                message.Text,
                message.SentAt);
        }
    }
}
