using API.Data;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Models.Enums;

namespace API.Repositories.Implementations
{
    public class InvitationRepository : GenericRepository<Invitation>, IInvitationRepository
    {
        public InvitationRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(dbContextFactory) { }

        public async Task<Invitation?> GetByTokenAsync(string token)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var invitation = await context.Invitations
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(i => i.Token == token);

            return invitation;
        }

        public async Task<Invitation?> GetValidByTokenAsync(string token)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var invitation = await context.Invitations
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(i => 
                                        i.Token == token &&
                                        i.Status == InvitationStatus.Pending &&
                                        i.ExpiresAt > DateTime.UtcNow);

            return invitation;
        }

        public async Task<IEnumerable<Invitation>> GetByAdvisorAsync(int advisorId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var invitations = await context.Invitations
                                .Where(i => i.AdvisorId == advisorId)
                                .OrderByDescending(i => i.CreatedAt)
                                .AsNoTracking()
                                .ToListAsync();

            return invitations;
        }

        public async Task<IEnumerable<Invitation>> GetPendingAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var invitations = await context.Invitations
                                .Where(i => i.Status == InvitationStatus.Pending &&
                                            i.ExpiresAt > DateTime.UtcNow)
                                .OrderByDescending(i => i.ExpiresAt)
                                .AsNoTracking()
                                .ToListAsync();

            return invitations;
        }

        public async Task MarkRedeemedAsync(int invitationId, int clientId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var invitation = await context.Invitations.FindAsync(invitationId);

            if (invitation is null)
                throw new KeyNotFoundException($"Invitation with id {invitationId} was not found.");

            if (invitation.Status != InvitationStatus.Pending)
                throw new InvalidOperationException($"Invitation {invitationId} is {invitation.Status} and cannot be redeemed.");

            if (invitation.ExpiresAt <= DateTime.UtcNow)
                throw new InvalidOperationException($"Invitation {invitationId} expired on {invitation.ExpiresAt:u}.");

            invitation.Status = InvitationStatus.Redeemed;
            invitation.RedeemedByIdClient = clientId;
            invitation.RedeemedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
        }

        public async Task<int> ExpireOverdueAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            return await context.Invitations
                            .Where(i => i.Status == InvitationStatus.Pending &&
                                        i.ExpiresAt <= DateTime.UtcNow)
                            .ExecuteUpdateAsync(s => s
                                    .SetProperty(i => i.Status, InvitationStatus.Expired));
        }
    }
}
