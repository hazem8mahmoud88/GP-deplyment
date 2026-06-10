using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureVote.Entities;

namespace SecureVote.Persistence.EntitiesConfigurations;

public class ElectionVoterConfiguration : IEntityTypeConfiguration<ElectionVoter>
{
    public void Configure(EntityTypeBuilder<ElectionVoter> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Election)
            .WithMany(x => x.ElectionVoters)
            .HasForeignKey(x => x.ElectionId);

        builder.HasOne(x => x.Voter)
            .WithMany(x => x.ElectionVoters)
            .HasForeignKey(x => x.VoterId);

        // Composite unique constraint
        builder.HasIndex(x => new { x.ElectionId, x.VoterId }).IsUnique();
    }
}

public class VotingSessionConfiguration : IEntityTypeConfiguration<VotingSession>
{
    public void Configure(EntityTypeBuilder<VotingSession> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Token).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(50);

        builder.HasIndex(x => x.Token).IsUnique();

        builder.HasOne(x => x.ElectionVoter)
            .WithMany(x => x.VotingSessions)
            .HasForeignKey(x => x.ElectionVoterId);
    }
}

public class BallotConfiguration : IEntityTypeConfiguration<Ballot>
{
    public void Configure(EntityTypeBuilder<Ballot> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Election)
            .WithMany(x => x.Ballots)
            .HasForeignKey(x => x.ElectionId);

        builder.HasOne(x => x.ElectionVoter)
            .WithOne(x => x.Ballot)
            .HasForeignKey<Ballot>(x => x.ElectionVoterId);

        // Unique constraint on ElectionVoterId (when not null)
        builder.HasIndex(x => x.ElectionVoterId)
            .IsUnique()
            .HasFilter("[ElectionVoterId] IS NOT NULL");
    }
}

public class VoteResultConfiguration : IEntityTypeConfiguration<VoteResult>
{
    public void Configure(EntityTypeBuilder<VoteResult> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Percentage).HasPrecision(5, 2);

        builder.HasOne(x => x.Election)
            .WithMany(x => x.Results)
            .HasForeignKey(x => x.ElectionId);

        builder.HasOne(x => x.Candidate)
            .WithMany(x => x.Results)
            .HasForeignKey(x => x.CandidateId);

        builder.HasOne(x => x.CountedByOrganizer)
            .WithMany(x => x.CountedResults)
            .HasForeignKey(x => x.CountedByOrganizerId);

        // One result per candidate per election
        builder.HasIndex(x => new { x.ElectionId, x.CandidateId }).IsUnique();
    }
}
