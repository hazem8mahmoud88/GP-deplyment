using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SecureVote.Entities;
using System.Reflection;

namespace SecureVote.Persistence;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // User Management
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Organizer> Organizers => Set<Organizer>();

    // Geography
    public DbSet<Governorate> Governorates => Set<Governorate>();
    public DbSet<Constituency> Constituencies => Set<Constituency>();

    // Voters
    public DbSet<Voter> Voters => Set<Voter>();

    // Elections
    public DbSet<Election> Elections => Set<Election>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<ElectionOrganizer> ElectionOrganizers => Set<ElectionOrganizer>();
    public DbSet<ElectionVoter> ElectionVoters => Set<ElectionVoter>();
    public DbSet<VotingSession> VotingSessions => Set<VotingSession>();

    // Voting & Results
    public DbSet<Ballot> Ballots => Set<Ballot>();
    public DbSet<VoteResult> Results => Set<VoteResult>();
    public DbSet<GeographicResult> GeographicResults => Set<GeographicResult>();
    public DbSet<DemographicResult> DemographicResults => Set<DemographicResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Disable cascade delete for all foreign keys
        var cascadeFKs = modelBuilder.Model
            .GetEntityTypes()
            .SelectMany(t => t.GetForeignKeys())
            .Where(fk => fk.DeleteBehavior == DeleteBehavior.Cascade && !fk.IsOwnership);

        foreach (var fk in cascadeFKs)
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<AuditableEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
