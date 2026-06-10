using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureVote.Entities;

namespace SecureVote.Persistence.EntitiesConfigurations;

public class VoterConfiguration : IEntityTypeConfiguration<Voter>
{
    public void Configure(EntityTypeBuilder<Voter> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UniqueIdentifier).HasMaxLength(50).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(200);
        builder.Property(x => x.Gender).HasMaxLength(20);
        builder.Property(x => x.PhoneNumber).HasMaxLength(20);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.PhotoUrl).HasMaxLength(500);

        builder.HasIndex(x => x.UniqueIdentifier).IsUnique();

        builder.HasOne(x => x.Governorate)
            .WithMany(x => x.Voters)
            .HasForeignKey(x => x.GovernorateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Constituency)
            .WithMany(x => x.Voters)
            .HasForeignKey(x => x.ConstituencyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
