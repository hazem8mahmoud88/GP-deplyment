using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureVote.Entities;

namespace SecureVote.Persistence.EntitiesConfigurations;

public class ElectionConfiguration : IEntityTypeConfiguration<Election>
{
    public void Configure(EntityTypeBuilder<Election> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();

        builder.HasOne(x => x.CreatedByAdmin)
            .WithMany(x => x.CreatedElections)
            .HasForeignKey(x => x.CreatedByAdminId);
    }
}

public class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Symbol).HasMaxLength(100);
        builder.Property(x => x.PartyName).HasMaxLength(200);
        builder.Property(x => x.PhotoPath).HasMaxLength(500);

        builder.HasOne(x => x.Election)
            .WithMany(x => x.Candidates)
            .HasForeignKey(x => x.ElectionId);
    }
}

public class ElectionOrganizerConfiguration : IEntityTypeConfiguration<ElectionOrganizer>
{
    public void Configure(EntityTypeBuilder<ElectionOrganizer> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Election)
            .WithMany(x => x.ElectionOrganizers)
            .HasForeignKey(x => x.ElectionId);

        builder.HasOne(x => x.Organizer)
            .WithMany(x => x.ElectionAssignments)
            .HasForeignKey(x => x.OrganizerId);

        builder.HasOne(x => x.AssignedByAdmin)
            .WithMany(x => x.AssignedOrganizers)
            .HasForeignKey(x => x.AssignedByAdminId);

        // Composite unique constraint
        builder.HasIndex(x => new { x.ElectionId, x.OrganizerId }).IsUnique();
    }
}
