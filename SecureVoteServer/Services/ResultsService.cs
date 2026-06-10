using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SecureVote.Abstractions;
using SecureVote.Contracts.Results;
using SecureVote.Encryption;
using SecureVote.Entities;
using SecureVote.Errors;
using SecureVote.Persistence;

namespace SecureVote.Services;

public class ResultsService(
    ApplicationDbContext context,
    IEncryptionService encryptionService) : IResultsService
{
    public async Task<Result<CountVotesResponse>> CountVotesAsync(int electionId, int organizerId)
    {
        // 1. Get election
        var election = await context.Elections
            .Include(e => e.Candidates)
            .Include(e => e.Results)
            .FirstOrDefaultAsync(e => e.Id == electionId);

        if (election is null)
            return Result.Failure<CountVotesResponse>(ResultsErrors.ElectionNotFound);

        // 2. Verify election is closed
        if (election.Status != Constants.ElectionStatus.Closed)
            return Result.Failure<CountVotesResponse>(ResultsErrors.ElectionNotClosed);

        // 3. Verify organizer is assigned with CanDecrypt permission
        var electionOrganizer = await context.ElectionOrganizers
            .FirstOrDefaultAsync(eo => eo.ElectionId == electionId && eo.OrganizerId == organizerId);

        if (electionOrganizer is null)
            return Result.Failure<CountVotesResponse>(ResultsErrors.NotAuthorized);

        if (!electionOrganizer.CanDecrypt)
            return Result.Failure<CountVotesResponse>(ResultsErrors.CannotDecrypt);

        // 4. Check if already counted
        if (election.Results.Any())
            return Result.Failure<CountVotesResponse>(ResultsErrors.AlreadyCounted);

        // 5. Get all ballots (include voter info for geographic stats - before anonymization)
        var ballots = await context.Ballots
            .Include(b => b.ElectionVoter)
                .ThenInclude(ev => ev!.Voter)
            .Where(b => b.ElectionId == electionId)
            .ToListAsync();

        if (!ballots.Any())
            return Result.Failure<CountVotesResponse>(ResultsErrors.NoBallots);

        // 6. Decrypt election private key
        string privateKeyPem;
        try
        {
            privateKeyPem = encryptionService.Decrypt(election.PrivateKeyEncrypted);
        }
        catch
        {
            return Result.Failure<CountVotesResponse>(ResultsErrors.DecryptionFailed);
        }

        // 7. Decrypt each ballot and count votes (cache results for geographic stats)
        var voteCounts = new Dictionary<int, int>();
        var ballotCandidateMap = new Dictionary<int, int>(); // ballotId -> candidateId
        var failedBallots = 0;
        foreach (var candidate in election.Candidates)
        {
            voteCounts[candidate.Id] = 0;
        }

        foreach (var ballot in ballots)
        {
            try
            {
                var candidateId = DecryptBallot(ballot, privateKeyPem);
                ballotCandidateMap[ballot.Id] = candidateId;
                if (voteCounts.ContainsKey(candidateId))
                {
                    voteCounts[candidateId]++;
                }
            }
            catch
            {
                failedBallots++;
                continue;
            }
        }

        // 8. Calculate percentages and save results
        var totalVotes = voteCounts.Values.Sum();
        var countedAt = DateTime.UtcNow;

        foreach (var candidate in election.Candidates)
        {
            var voteCount = voteCounts[candidate.Id];
            var percentage = totalVotes > 0 
                ? Math.Round((decimal)voteCount / totalVotes * 100, 2) 
                : 0;

            var result = new VoteResult
            {
                ElectionId = electionId,
                CandidateId = candidate.Id,
                VoteCount = voteCount,
                Percentage = percentage,
                CountedByOrganizerId = organizerId,
                CountedAt = countedAt
            };

            context.Results.Add(result);
        }

        // 9. Compute geographic vote breakdown (BEFORE anonymization) — reuse cached decryption
        var geoVoteCounts = new Dictionary<(int CandidateId, int GovernorateId, int? ConstituencyId), int>();

        foreach (var ballot in ballots)
        {
            if (ballot.ElectionVoter?.Voter?.GovernorateId is null)
                continue;

            if (!ballotCandidateMap.TryGetValue(ballot.Id, out var candidateId))
                continue;

            var govId = ballot.ElectionVoter.Voter.GovernorateId.Value;
            var constId = ballot.ElectionVoter.Voter.ConstituencyId;

            // Count by governorate
            var govKey = (candidateId, govId, (int?)null);
            geoVoteCounts[govKey] = geoVoteCounts.GetValueOrDefault(govKey) + 1;

            // Count by constituency (if available)
            if (constId is not null)
            {
                var constKey = (candidateId, govId, constId);
                geoVoteCounts[constKey] = geoVoteCounts.GetValueOrDefault(constKey) + 1;
            }
        }

        // Save geographic results
        var govGroups = geoVoteCounts
            .Where(kv => kv.Key.ConstituencyId is null)
            .GroupBy(kv => kv.Key.GovernorateId);

        foreach (var govGroup in govGroups)
        {
            var govTotal = govGroup.Sum(kv => kv.Value);
            foreach (var kv in govGroup)
            {
                var pct = govTotal > 0 ? Math.Round((decimal)kv.Value / govTotal * 100, 2) : 0;
                context.GeographicResults.Add(new GeographicResult
                {
                    ElectionId = electionId,
                    CandidateId = kv.Key.CandidateId,
                    GovernorateId = kv.Key.GovernorateId,
                    ConstituencyId = null,
                    VoteCount = kv.Value,
                    Percentage = pct
                });
            }
        }

        // Save constituency-level results
        var constGroups = geoVoteCounts
            .Where(kv => kv.Key.ConstituencyId is not null)
            .GroupBy(kv => (kv.Key.GovernorateId, kv.Key.ConstituencyId));

        foreach (var constGroup in constGroups)
        {
            var constTotal = constGroup.Sum(kv => kv.Value);
            foreach (var kv in constGroup)
            {
                var pct = constTotal > 0 ? Math.Round((decimal)kv.Value / constTotal * 100, 2) : 0;
                context.GeographicResults.Add(new GeographicResult
                {
                    ElectionId = electionId,
                    CandidateId = kv.Key.CandidateId,
                    GovernorateId = kv.Key.GovernorateId,
                    ConstituencyId = kv.Key.ConstituencyId,
                    VoteCount = kv.Value,
                    Percentage = pct
                });
            }
        }

        // 10. Compute demographic vote breakdown (BEFORE anonymization)
        var ageGroups = new[] { "18-25", "26-35", "36-45", "46-60", "60+" };
        var demographicCounts = new Dictionary<(int CandidateId, string Category, string GroupName), int>();

        foreach (var ballot in ballots)
        {
            if (ballot.ElectionVoter?.Voter is null)
                continue;

            if (!ballotCandidateMap.TryGetValue(ballot.Id, out var candidateId))
                continue;

            var voter = ballot.ElectionVoter.Voter;

            // Gender breakdown
            if (!string.IsNullOrEmpty(voter.Gender))
            {
                var genderKey = (candidateId, "Gender", voter.Gender);
                demographicCounts[genderKey] = demographicCounts.GetValueOrDefault(genderKey) + 1;
            }

            // Age breakdown
            if (voter.DateOfBirth is not null)
            {
                var ageGroup = GetAgeGroup(voter.DateOfBirth.Value);
                var ageKey = (candidateId, "AgeGroup", ageGroup);
                demographicCounts[ageKey] = demographicCounts.GetValueOrDefault(ageKey) + 1;
            }
        }

        // Save demographic results
        foreach (var category in new[] { "Gender", "AgeGroup" })
        {
            var categoryItems = demographicCounts.Where(kv => kv.Key.Category == category).ToList();
            var categoryGroups = categoryItems.GroupBy(kv => kv.Key.CandidateId);

            foreach (var candidateGroup in categoryGroups)
            {
                var candidateTotal = candidateGroup.Sum(kv => kv.Value);
                foreach (var kv in candidateGroup)
                {
                    var pct = candidateTotal > 0 ? Math.Round((decimal)kv.Value / candidateTotal * 100, 2) : 0;
                    context.DemographicResults.Add(new DemographicResult
                    {
                        ElectionId = electionId,
                        CandidateId = kv.Key.CandidateId,
                        Category = kv.Key.Category,
                        GroupName = kv.Key.GroupName,
                        VoteCount = kv.Value,
                        Percentage = pct
                    });
                }
            }
        }

        // 11. Anonymize ballots - remove voter link permanently
        foreach (var ballot in ballots)
        {
            ballot.ElectionVoterId = null;
        }

        // 12. Update election status to Counted
        election.Status = Constants.ElectionStatus.Counted;
        election.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        var message = failedBallots > 0
            ? $"Votes counted successfully. {failedBallots} ballot(s) failed decryption. Ballots have been anonymized."
            : "Votes counted successfully. Ballots have been anonymized.";

        return Result.Success(new CountVotesResponse(
            Success: true,
            Message: message,
            TotalVotesCounted: totalVotes,
            FailedBallots: failedBallots,
            TotalCandidates: election.Candidates.Count,
            CountedAt: countedAt
        ));
    }

    public async Task<Result<ElectionResultsResponse>> GetResultsAsync(int electionId)
    {
        var election = await context.Elections
            .Include(e => e.Results)
                .ThenInclude(r => r.Candidate)
            .FirstOrDefaultAsync(e => e.Id == electionId);

        if (election is null)
            return Result.Failure<ElectionResultsResponse>(ResultsErrors.ElectionNotFound);

        if (!election.Results.Any())
            return Result.Failure<ElectionResultsResponse>(ResultsErrors.ResultsNotAvailable);

        var totalVotes = election.Results.Sum(r => r.VoteCount);
        var countedAt = election.Results.FirstOrDefault()?.CountedAt;

        var candidateResults = election.Results
            .OrderByDescending(r => r.VoteCount)
            .Select(r => new CandidateResultResponse(
                CandidateId: r.CandidateId,
                CandidateName: r.Candidate.FullName,
                Party: r.Candidate.PartyName,
                PhotoUrl: r.Candidate.PhotoPath,
                VoteCount: r.VoteCount,
                Percentage: r.Percentage
            ))
            .ToList();

        return Result.Success(new ElectionResultsResponse(
            ElectionId: election.Id,
            ElectionTitle: election.Title,
            ElectionType: election.Type,
            Status: election.Status,
            TotalVotes: totalVotes,
            CountedAt: countedAt,
            CandidateResults: candidateResults
        ));
    }

    private int DecryptBallot(Ballot ballot, string privateKeyPem)
    {
        // 1. Decrypt AES key using RSA private key
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var aesKey = rsa.Decrypt(ballot.EncryptedAESKey, RSAEncryptionPadding.OaepSHA256);

        // 2. Decrypt vote using AES-GCM
        var decryptedVote = new byte[ballot.EncryptedVote.Length];

        using (var aesGcm = new AesGcm(aesKey, 16))
        {
            aesGcm.Decrypt(ballot.IV, ballot.EncryptedVote, ballot.AuthTag, decryptedVote);
        }

        // 3. Parse vote payload and extract candidate ID
        var voteJson = Encoding.UTF8.GetString(decryptedVote);
        var voteData = JsonSerializer.Deserialize<VotePayload>(voteJson);

        return voteData?.CandidateId ?? throw new InvalidOperationException("Invalid vote payload");
    }

    private record VotePayload(int CandidateId, DateTime Timestamp);

    public async Task<Result<GovernorateStatsResponse>> GetStatsByGovernorateAsync(int electionId)
    {
        var election = await context.Elections.FindAsync(electionId);
        if (election is null)
            return Result.Failure<GovernorateStatsResponse>(ResultsErrors.ElectionNotFound);

        var geoResults = await context.GeographicResults
            .Include(gr => gr.Candidate)
            .Include(gr => gr.Governorate)
            .Where(gr => gr.ElectionId == electionId && gr.ConstituencyId == null)
            .ToListAsync();

        if (!geoResults.Any())
            return Result.Failure<GovernorateStatsResponse>(ResultsErrors.ResultsNotAvailable);

        var governorates = geoResults
            .GroupBy(gr => gr.GovernorateId)
            .Select(g => new GovernorateBreakdown(
                GovernorateId: g.Key,
                GovernorateNameAr: g.First().Governorate.NameAr,
                GovernorateNameEn: g.First().Governorate.NameEn,
                TotalVotes: g.Sum(x => x.VoteCount),
                Candidates: g.OrderByDescending(x => x.VoteCount).Select(x => new CandidateBreakdown(
                    CandidateId: x.CandidateId,
                    CandidateName: x.Candidate.FullName,
                    Party: x.Candidate.PartyName,
                    VoteCount: x.VoteCount,
                    Percentage: x.Percentage
                ))
            ))
            .OrderByDescending(g => g.TotalVotes)
            .ToList();

        return Result.Success(new GovernorateStatsResponse(
            ElectionId: electionId,
            ElectionTitle: election.Title,
            TotalVotes: governorates.Sum(g => g.TotalVotes),
            Governorates: governorates
        ));
    }

    public async Task<Result<ConstituencyStatsResponse>> GetStatsByConstituencyAsync(int electionId, int governorateId)
    {
        var election = await context.Elections.FindAsync(electionId);
        if (election is null)
            return Result.Failure<ConstituencyStatsResponse>(ResultsErrors.ElectionNotFound);

        var governorate = await context.Governorates.FindAsync(governorateId);
        if (governorate is null)
            return Result.Failure<ConstituencyStatsResponse>(ResultsErrors.ElectionNotFound);

        var geoResults = await context.GeographicResults
            .Include(gr => gr.Candidate)
            .Include(gr => gr.Constituency)
            .Where(gr => gr.ElectionId == electionId && gr.GovernorateId == governorateId && gr.ConstituencyId != null)
            .ToListAsync();

        if (!geoResults.Any())
            return Result.Failure<ConstituencyStatsResponse>(ResultsErrors.ResultsNotAvailable);

        var constituencies = geoResults
            .GroupBy(gr => gr.ConstituencyId)
            .Select(g => new ConstituencyBreakdown(
                ConstituencyId: g.Key!.Value,
                ConstituencyNameAr: g.First().Constituency!.NameAr,
                ConstituencyNameEn: g.First().Constituency!.NameEn,
                TotalVotes: g.Sum(x => x.VoteCount),
                Candidates: g.OrderByDescending(x => x.VoteCount).Select(x => new CandidateBreakdown(
                    CandidateId: x.CandidateId,
                    CandidateName: x.Candidate.FullName,
                    Party: x.Candidate.PartyName,
                    VoteCount: x.VoteCount,
                    Percentage: x.Percentage
                ))
            ))
            .OrderByDescending(c => c.TotalVotes)
            .ToList();

        return Result.Success(new ConstituencyStatsResponse(
            ElectionId: electionId,
            GovernorateId: governorateId,
            GovernorateNameAr: governorate.NameAr,
            TotalVotes: constituencies.Sum(c => c.TotalVotes),
            Constituencies: constituencies
        ));
    }

    public async Task<Result<ParticipationStatsResponse>> GetParticipationStatsAsync(int electionId)
    {
        var election = await context.Elections.FindAsync(electionId);
        if (election is null)
            return Result.Failure<ParticipationStatsResponse>(ResultsErrors.ElectionNotFound);

        var electionVoters = await context.ElectionVoters
            .Include(ev => ev.Voter)
                .ThenInclude(v => v.Governorate)
            .Where(ev => ev.ElectionId == electionId)
            .ToListAsync();

        var totalRegistered = electionVoters.Count;
        var totalVoted = electionVoters.Count(ev => ev.HasVoted);
        var overallPct = totalRegistered > 0
            ? Math.Round((decimal)totalVoted / totalRegistered * 100, 2) : 0;

        var byGovernorate = electionVoters
            .Where(ev => ev.Voter.GovernorateId is not null)
            .GroupBy(ev => ev.Voter.GovernorateId!.Value)
            .Select(g =>
            {
                var gov = g.First().Voter.Governorate!;
                var registered = g.Count();
                var voted = g.Count(ev => ev.HasVoted);
                var maleVoted = g.Count(ev => ev.HasVoted && (ev.Voter.Gender == "Male" || ev.Voter.Gender == "ذكر"));
                var femaleVoted = g.Count(ev => ev.HasVoted && (ev.Voter.Gender == "Female" || ev.Voter.Gender == "أنثى"));
                var pct = registered > 0 ? Math.Round((decimal)voted / registered * 100, 2) : 0;
                return new GovernorateParticipation(
                    GovernorateId: gov.Id,
                    GovernorateNameAr: gov.NameAr,
                    GovernorateNameEn: gov.NameEn,
                    Registered: registered,
                    Voted: voted,
                    MaleVoted: maleVoted,
                    FemaleVoted: femaleVoted,
                    Percentage: pct
                );
            })
            .OrderByDescending(g => g.Percentage)
            .ToList();

        return Result.Success(new ParticipationStatsResponse(
            ElectionId: electionId,
            ElectionTitle: election.Title,
            TotalRegistered: totalRegistered,
            TotalVoted: totalVoted,
            OverallPercentage: overallPct,
            ByGovernorate: byGovernorate
        ));
    }

    public async Task<Result<DemographicStatsResponse>> GetDemographicStatsAsync(int electionId)
    {
        var election = await context.Elections
            .Include(e => e.Candidates)
            .FirstOrDefaultAsync(e => e.Id == electionId);

        if (election is null)
            return Result.Failure<DemographicStatsResponse>(ResultsErrors.ElectionNotFound);

        var demoResults = await context.DemographicResults
            .Include(dr => dr.Candidate)
            .Where(dr => dr.ElectionId == electionId)
            .ToListAsync();

        if (!demoResults.Any())
            return Result.Failure<DemographicStatsResponse>(ResultsErrors.ResultsNotAvailable);

        // ── Gender Stats ──
        var genderResults = demoResults.Where(dr => dr.Category == "Gender").ToList();
        var totalMaleVotes = genderResults.Where(r => r.GroupName == "Male" || r.GroupName == "ذكر").Sum(r => r.VoteCount);
        var totalFemaleVotes = genderResults.Where(r => r.GroupName == "Female" || r.GroupName == "أنثى").Sum(r => r.VoteCount);
        var totalGenderVotes = totalMaleVotes + totalFemaleVotes;

        var candidateGenderBreakdowns = genderResults
            .GroupBy(r => r.CandidateId)
            .Select(g =>
            {
                var male = g.Where(r => r.GroupName == "Male" || r.GroupName == "ذكر").Sum(r => r.VoteCount);
                var female = g.Where(r => r.GroupName == "Female" || r.GroupName == "أنثى").Sum(r => r.VoteCount);
                var total = male + female;
                return new CandidateGenderBreakdown(
                    CandidateId: g.Key,
                    CandidateName: g.First().Candidate.FullName,
                    Party: g.First().Candidate.PartyName,
                    MaleVotes: male,
                    FemaleVotes: female,
                    MalePercentage: total > 0 ? Math.Round((decimal)male / total * 100, 2) : 0,
                    FemalePercentage: total > 0 ? Math.Round((decimal)female / total * 100, 2) : 0
                );
            })
            .OrderByDescending(c => c.MaleVotes + c.FemaleVotes)
            .ToList();

        var genderStats = new GenderStatsBreakdown(
            TotalMaleVotes: totalMaleVotes,
            TotalFemaleVotes: totalFemaleVotes,
            MalePercentage: totalGenderVotes > 0 ? Math.Round((decimal)totalMaleVotes / totalGenderVotes * 100, 2) : 0,
            FemalePercentage: totalGenderVotes > 0 ? Math.Round((decimal)totalFemaleVotes / totalGenderVotes * 100, 2) : 0,
            CandidateBreakdowns: candidateGenderBreakdowns
        );

        // ── Age Stats ──
        var ageResults = demoResults.Where(dr => dr.Category == "AgeGroup").ToList();
        var totalAgeVotes = ageResults.Sum(r => r.VoteCount);

        var ageGroupOrder = new[] { "18-25", "26-35", "36-45", "46-60", "60+" };

        var ageGroupSummaries = ageGroupOrder
            .Select(ag =>
            {
                var count = ageResults.Where(r => r.GroupName == ag).Sum(r => r.VoteCount);
                return new AgeGroupSummary(
                    AgeGroup: ag,
                    VoteCount: count,
                    Percentage: totalAgeVotes > 0 ? Math.Round((decimal)count / totalAgeVotes * 100, 2) : 0
                );
            })
            .ToList();

        var candidateAgeBreakdowns = ageResults
            .GroupBy(r => r.CandidateId)
            .Select(g =>
            {
                var candidateTotal = g.Sum(r => r.VoteCount);
                return new CandidateAgeBreakdown(
                    CandidateId: g.Key,
                    CandidateName: g.First().Candidate.FullName,
                    Party: g.First().Candidate.PartyName,
                    AgeGroups: ageGroupOrder.Select(ag =>
                    {
                        var count = g.Where(r => r.GroupName == ag).Sum(r => r.VoteCount);
                        return new AgeGroupVotes(
                            AgeGroup: ag,
                            VoteCount: count,
                            Percentage: candidateTotal > 0 ? Math.Round((decimal)count / candidateTotal * 100, 2) : 0
                        );
                    })
                );
            })
            .OrderByDescending(c => c.AgeGroups.Sum(a => a.VoteCount))
            .ToList();

        var ageStats = new AgeStatsBreakdown(
            AgeGroups: ageGroupSummaries,
            CandidateBreakdowns: candidateAgeBreakdowns
        );

        var totalVotes = election.Results?.Sum(r => r.VoteCount) ?? demoResults.Max(r => r.VoteCount);

        return Result.Success(new DemographicStatsResponse(
            ElectionId: electionId,
            ElectionTitle: election.Title,
            TotalVotes: totalGenderVotes > 0 ? totalGenderVotes : totalAgeVotes,
            GenderStats: genderStats,
            AgeStats: ageStats
        ));
    }

    private static string GetAgeGroup(DateTime dateOfBirth)
    {
        var age = DateTime.UtcNow.Year - dateOfBirth.Year;
        if (dateOfBirth > DateTime.UtcNow.AddYears(-age)) age--;

        return age switch
        {
            <= 25 => "18-25",
            <= 35 => "26-35",
            <= 45 => "36-45",
            <= 60 => "46-60",
            _ => "60+"
        };
    }
}
