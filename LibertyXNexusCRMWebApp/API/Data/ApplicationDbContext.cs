using Microsoft.EntityFrameworkCore;
using Shared.Models;
using API.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace API.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Advisor> Advisors => Set<Advisor>();
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Policy> Policies => Set<Policy>();
        public DbSet<Meeting> Meetings => Set<Meeting>();
        public DbSet<Document> Documents => Set<Document>();
        public DbSet<Case> Cases => Set<Case>();
        public DbSet<ClientQuery> Queries => Set<ClientQuery>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Invitation> Invitations => Set<Invitation>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Message> Messages => Set<Message>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Client>(entity =>
            {
                entity.Property(c => c.GrossMonthlyIncome).HasPrecision(18, 2);
                entity.Property(c => c.NetMonthlyIncome).HasPrecision(18, 2);
                entity.Property(c => c.MonthlyExpenses).HasPrecision(18, 2);
                entity.Property(c => c.PropertyValue).HasPrecision(18, 2);
                entity.Property(c => c.ExistingInvestments).HasPrecision(18, 2);
                entity.Property(c => c.RetirementSavings).HasPrecision(18, 2);
                entity.Property(c => c.OutstandingDebt).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Advisor>(entity =>
            {
                entity.HasIndex(a => a.Email).IsUnique();
                entity.HasIndex(a => a.IdentityProviderSubjectId).IsUnique();
            });

            modelBuilder.Entity<Client>(entity =>
            {
                entity.HasIndex(c => c.Email).IsUnique();
                entity.HasIndex(c => c.IdentityProviderSubjectId).IsUnique();

                entity.HasIndex(c => new { c.LastName, c.FirstName });
                entity.HasIndex(c => c.Status);

                entity.HasOne(c => c.Advisor)
                    .WithMany(a => a.Clients)
                    .HasForeignKey(c => c.AdvisorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(c => c.Status)
                    .HasConversion<int>();
            });

            modelBuilder.Entity<Policy>(entity =>
            {
                entity.HasIndex(p => p.ClientId);
                entity.HasIndex(p => p.IsCatalogueItem);

                entity.HasOne(p => p.Client)
                    .WithMany(c => c.Policies)
                    .HasForeignKey(p => p.ClientId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(p => p.Status)
                    .HasConversion<int>();

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Policy_CatalogueOrClientHeld",
                    "([IsCatalogueItem] = 0 AND [ClientId] IS NOT NULL) OR " +
                    "([IsCatalogueItem] = 1 AND [ClientId] IS NULL)"
                    ));
            });

            modelBuilder.Entity<Case>(entity =>
            {
                entity.HasOne(c => c.Policy)
                    .WithOne(p => p.Case)
                    .HasForeignKey<Case>(c => c.PolicyId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(c => c.PolicyId);
                entity.HasIndex(c => c.Status);

                entity.Property(c => c.Status)
                    .HasConversion<int>();
            });

            modelBuilder.Entity<Meeting>(entity =>
            {
                entity.HasOne(m => m.Client)
                    .WithMany(c => c.Meetings)
                    .HasForeignKey(m => m.ClientId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(m => m.MeetingDate);
                entity.HasIndex(m => new { m.ClientId, m.MeetingDate });

                entity.Property(m => m.Status)
                    .HasConversion<int>();
            });

            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasOne(d => d.Client)
                    .WithMany(c => c.Documents)
                    .HasForeignKey(d => d.ClientId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Policy)
                    .WithMany(p => p.Documents)
                    .HasForeignKey(d => d.PolicyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(d => d.ClientId);
                entity.HasIndex(d => new { d.ClientId, d.VisibleToClient });

                entity.Property(d => d.DocumentType)
                    .HasConversion<int>();
            });

            modelBuilder.Entity<ClientQuery>(entity =>
            {
                entity.HasOne(q => q.Client)
                    .WithMany(c => c.Queries)
                    .HasForeignKey(q => q.ClientId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(q => q.Advisor)
                    .WithMany(a => a.Queries)
                    .HasForeignKey(q => q.AdvisorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(q => q.Status);
                entity.HasIndex(q => new { q.ClientId, q.CreatedAt });

                entity.Property(q => q.Status)
                    .HasConversion<int>();
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasOne(n => n.Client)
                    .WithMany(c => c.Notifications)
                    .HasForeignKey(n => n.ClientId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(n => n.Advisor)
                    .WithMany()
                    .HasForeignKey(n => n.AdvisorId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(n => new { n.ClientId, n.IsRead });
                entity.HasIndex(n => new { n.AdvisorId, n.IsRead });

                entity.Property(n => n.Type)
                    .HasConversion<int>();

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Notification_SingleRecipient",
                    "([ClientId] IS NOT NULL AND [AdvisorId] IS NULL) OR " +
                    "([ClientId] IS NULL AND [AdvisorId] IS NOT NULL)"
                    ));
            });

            modelBuilder.Entity<Invitation>(entity =>
            {
                entity.HasOne(i => i.Advisor)
                    .WithMany(a => a.Invitations)
                    .HasForeignKey(i => i.AdvisorId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(i => i.Token).IsUnique();
                entity.HasIndex(i => i.Email);

                entity.Property(i => i.Status)
                    .HasConversion<int>();
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasOne(m => m.Client)
                    .WithMany()
                    .HasForeignKey(m => m.ClientId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Advisor)
                    .WithMany()
                    .HasForeignKey(m => m.AdvisorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(m => new { m.ClientId, m.SentAt });
                entity.HasIndex(m => new { m.AdvisorId, m.SentAt });
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {

                entity.HasIndex(a => a.Timestamp);
                entity.HasIndex(a => new { a.EntityAffected, a.EntityId });

                entity.Property(a => a.ActionType)
                    .HasConversion<int>();
                entity.Property(a => a.UserRole)
                    .HasConversion<int>();
            });
        }

        public override int SaveChanges()
        {
            ApplyTimeStamps();
            return base.SaveChanges();
        }

        public override System.Threading.Tasks.Task<int> SaveChangesAsync(
            System.Threading.CancellationToken cancellationToken = default
            )
        {
            ApplyTimeStamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyTimeStamps()
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State != EntityState.Modified) continue;

                switch (entry.Entity)
                {
                    case Client c: c.UpdatedAt = DateTime.UtcNow; break;
                    case Policy p: p.UpdatedAt = DateTime.UtcNow; break;
                    case Meeting m: m.UpdatedAt = DateTime.UtcNow; break;
                    case Case cs: cs.UpdatedAt = DateTime.UtcNow; break;
                }
            }
        }
    }
}
