using API.Data;
using API.DTOs.Notes;
using API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace API.Services.Implementations
{
    /// <summary>
    /// The advisor's private notes about their clients.
    /// </summary>
    public class ClientNoteService : IClientNoteService
    {
        private const int MaxTextLength = 2000;

        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public ClientNoteService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the notes for one client, newest first.
        /// </summary>
        /// <exception cref="KeyNotFoundException"></exception>
        public async Task<IEnumerable<ClientNoteDto>> GetForClientAsync(int clientId, int advisorId)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();

            await EnsureOwnClientAsync(db, clientId, advisorId);

            var notes = await db.ClientNotes
                .AsNoTracking()
                .Where(n => n.ClientId == clientId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return notes.Select(MapToDto).ToList();
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Adds a note to a client.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="KeyNotFoundException"></exception>
        public async Task<ClientNoteDto> CreateAsync(int clientId, int advisorId, CreateClientNoteRequest request)
        {
            var text = request?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Write something in the note before saving it.");
            }

            if (text.Length > MaxTextLength)
            {
                throw new ArgumentException($"A note cannot be longer than {MaxTextLength} characters.");
            }

            await using var db = await _contextFactory.CreateDbContextAsync();

            await EnsureOwnClientAsync(db, clientId, advisorId);

            var note = new ClientNote
            {
                ClientId = clientId,
                AdvisorId = advisorId,
                Text = text,
                CreatedAt = DateTime.UtcNow
            };

            db.ClientNotes.Add(note);
            await db.SaveChangesAsync();

            return MapToDto(note);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Deletes a note. The advisor check is part of the query, so one advisor can never delete
        /// a note about another advisor's client. Returns false if the note wasn't found.
        /// </summary>
        public async Task<bool> DeleteAsync(int clientNoteId, int advisorId)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();

            var note = await db.ClientNotes
                .FirstOrDefaultAsync(n => n.ClientNoteId == clientNoteId && n.Client.AdvisorId == advisorId);

            if (note is null)
            {
                return false;
            }

            db.ClientNotes.Remove(note);
            await db.SaveChangesAsync();
            return true;
        }

        private static async Task EnsureOwnClientAsync(ApplicationDbContext db, int clientId, int advisorId)
        {
            var isOwnClient = await db.Clients.AnyAsync(c => c.ClientId == clientId && c.AdvisorId == advisorId);

            if (!isOwnClient)
            {
                throw new KeyNotFoundException($"Client {clientId} was not found.");
            }
        }

        private static ClientNoteDto MapToDto(ClientNote note) => new()
        {
            ClientNoteId = note.ClientNoteId,
            ClientId = note.ClientId,
            Text = note.Text,
            CreatedAt = note.CreatedAt
        };
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
