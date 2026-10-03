using API.Data;
using API.DTOs.Messages;
using API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Models.Enums;

namespace API.Services.Implementations
{
    /// <summary>
    /// Service for managing messages between clients and advisors.
    /// </summary>
    public class MessageService : IMessageService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly INotificationService _notificationService;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageService"/> class.
        /// </summary>
        /// <param name="contextFactory"></param>
        /// <param name="notificationService"></param>
        public MessageService(IDbContextFactory<ApplicationDbContext> contextFactory, INotificationService notificationService)
        {
            _contextFactory = contextFactory;
            _notificationService = notificationService;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves the conversation messages for a specific client.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves the conversation messages for a specific advisor and client.
        /// </summary>
        /// <param name="advisorId"></param>
        /// <param name="clientId"></param>
        /// <returns></returns>
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves a summary of conversations for a specific advisor, including the latest message from each client.
        /// </summary>
        /// <param name="advisorId"></param>
        /// <returns></returns>
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Sends a message from either a client or an advisor. 
        /// Validates the request and ensures that the client is assigned to the advisor before sending the message. 
        /// If the message is sent by a client, it also notifies the advisor.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="fromAdvisor"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
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

            if (!fromAdvisor)
            {
                await _notificationService.NotifyAdvisorAsync(
                    request.AdvisorId,
                    NotificationType.MessageReceived,
                    $"{client.FullName} sent you a new message.",
                    "/messages",
                    client.ClientId);
            }

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

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
