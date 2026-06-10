using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureVote.Entities;

namespace SecureVote.Persistence.EntitiesConfigurations;

public class GeographicResultConfiguration : IEntityTypeConfiguration<GeographicResult>
{
    public void Configure(EntityTypeBuilder<GeographicResult> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Percentage).HasPrecision(5, 2);

        builder.HasOne(x => x.Election)
            .WithMany()
            .HasForeignKey(x => x.ElectionId);

        builder.HasOne(x => x.Candidate)
            .WithMany()
            .HasForeignKey(x => x.CandidateId);

        builder.HasOne(x => x.Governorate)
            .WithMany()
            .HasForeignKey(x => x.GovernorateId);

        builder.HasOne(x => x.Constituency)
            .WithMany()
            .HasForeignKey(x => x.ConstituencyId);
    }
}
