using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SecureVote.Entities;
using SecureVote.Persistence;
using SecureVote.Encryption;
using SecureVote.Constants;

namespace SecureVote.Extensions;

public static class TestDataSeeder
{
    private static readonly Random _random = new(); // Non-fixed seed for true randomness

    private static readonly string[] _maleNames =
    {
        "أحمد محمد", "محمد إبراهيم", "عمر خالد", "يوسف حسن", "علي حسين",
        "مصطفى أحمد", "حسن محمود", "كريم عبدالله", "طارق سعيد", "ياسر جمال",
        "هشام فوزي", "وليد حمدي", "سامح رضا", "خالد عادل", "عمرو بدر",
        "إسلام نبيل", "أيمن شريف", "رامي فاروق", "باسم وجيه", "نادر ثروت",
        "تامر حسني", "شريف منير", "عصام حسن", "جمال عبدالناصر", "فتحي صالح"
    };

    private static readonly string[] _femaleNames =
    {
        "فاطمة محمد", "نور أحمد", "سارة إبراهيم", "مريم حسن", "هدى علي",
        "آية خالد", "دينا محمود", "رنا عبدالله", "لمياء سعيد", "سلمى جمال",
        "ياسمين فوزي", "هبة حمدي", "نهى رضا", "إيمان عادل", "شيماء بدر",
        "أميرة نبيل", "منى شريف", "ريم فاروق", "نورهان وجيه", "حنان ثروت",
        "ولاء حسنين", "سمر عبدالعزيز", "غادة منصور", "رشا كمال", "عبير طلعت"
    };

    public static async Task SeedTestDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        logger.LogInformation("🚀 Starting test data seeding...");

        if (await context.Elections.AnyAsync(e => e.Title.Contains("[TEST]")))
        {
            logger.LogWarning("⚠️ Test data already exists! Call /clear-test-data first.");
            return;
        }

        // 1. Create test organizer
        var organizer = await CreateTestOrganizerAsync(userManager, context, logger);
        if (organizer is null) return;

        // 2. Get admin
        var admin = await context.Admins.FirstAsync();

        // 3. Load all governorates and constituencies
        var allGovernorates = await context.Governorates
            .Include(g => g.Constituencies)
            .OrderBy(g => g.Id)
            .ToListAsync();

        if (!allGovernorates.Any())
        {
            logger.LogError("❌ No governorates found! Run the app first to seed governorates.");
            return;
        }

        // 4. Find Cairo and Maadi specifically
        var cairo = allGovernorates.First(g => g.Code == "CAI");
        var maadi = cairo.Constituencies.First(c => c.NameAr == "المعادي");

        // 5. Create presidential voters (1000, randomly across ALL governorates)
        var presidentialVoters = await CreatePresidentialVotersAsync(context, allGovernorates, 1000, logger);

        // 6. Create university voters (500, no geographic data, unique)
        var universityVoters = await CreateUniversityVotersAsync(context, 500, logger);

        // 7. Create Maadi parliamentary voters (1000, Cairo/Maadi only, unique)
        var maadiVoters = await CreateMaadiVotersAsync(context, cairo, maadi, 1000, logger);

        // 8. Create Presidential Election
        await CreatePresidentialElectionAsync(context, encryptionService, admin, organizer, presidentialVoters, logger);

        // 9. Create University Election
        await CreateUniversityElectionAsync(context, encryptionService, admin, organizer, universityVoters, logger);

        // 10. Create Maadi Parliamentary Election
        await CreateMaadiParliamentaryElectionAsync(context, encryptionService, admin, organizer, maadiVoters, cairo, maadi, logger);

        logger.LogInformation("✅ Test data seeded successfully! (3 elections: Presidential + University + Parliamentary-Maadi)");
        logger.LogInformation("📊 Total voters: {P} presidential + {U} university + {M} Maadi = {T}",
            presidentialVoters.Count, universityVoters.Count, maadiVoters.Count,
            presidentialVoters.Count + universityVoters.Count + maadiVoters.Count);
    }

    // ─────────────────────────────────────────────────────────────────
    // ORGANIZER
    // ─────────────────────────────────────────────────────────────────

    private static async Task<Organizer?> CreateTestOrganizerAsync(
        UserManager<User> userManager, ApplicationDbContext context, ILogger logger)
    {
        const string email = "testorg@securevote.com";
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
            return await context.Organizers.FirstOrDefaultAsync(o => o.UserId == existingUser.Id);

        var user = new User
        {
            Email = email,
            UserName = "testorganizer",
            Role = AppRoles.Organizer,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, "Test@123456");
        if (!result.Succeeded)
        {
            logger.LogError("❌ Failed to create test organizer: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return null;
        }

        var organizer = new Organizer
        {
            UserId = user.Id,
            FullName = "منظم الاختبار",
            Organization = "هيئة الانتخابات",
            PhoneNumber = "+201000000000"
        };

        context.Organizers.Add(organizer);
        await context.SaveChangesAsync();

        logger.LogInformation("✅ Test organizer: {Email} / Test@123456", email);
        return organizer;
    }

    // ─────────────────────────────────────────────────────────────────
    // VOTERS
    // ─────────────────────────────────────────────────────────────────

    /// <summary>1000 presidential voters spread RANDOMLY across ALL governorates.</summary>
    private static async Task<List<Voter>> CreatePresidentialVotersAsync(
        ApplicationDbContext context, List<Governorate> allGov, int count, ILogger logger)
    {
        var voters = new List<Voter>();

        for (int i = 1; i <= count; i++)
        {
            // Truly random governorate (not uniform distribution)
            var gov = allGov[_random.Next(allGov.Count)];
            var constituency = gov.Constituencies.Any()
                ? gov.Constituencies.ElementAt(_random.Next(gov.Constituencies.Count))
                : null;

            var isMale = _random.Next(2) == 0;
            var name = isMale
                ? _maleNames[_random.Next(_maleNames.Length)]
                : _femaleNames[_random.Next(_femaleNames.Length)];

            voters.Add(new Voter
            {
                UniqueIdentifier = $"3{i:D013}",
                FullName = name,
                DateOfBirth = new DateTime(1960 + _random.Next(45), _random.Next(1, 13), _random.Next(1, 28)),
                Gender = isMale ? "Male" : "Female",
                PhoneNumber = $"+20100{_random.Next(1000000, 9999999)}",
                Email = $"pres{i}@test.com",
                GovernorateId = gov.Id,
                ConstituencyId = constituency?.Id
            });
        }

        context.Voters.AddRange(voters);
        await context.SaveChangesAsync();

        logger.LogInformation("✅ Created {Count} presidential voters across {G} governorates (random distribution)",
            voters.Count, voters.Select(v => v.GovernorateId).Distinct().Count());
        return voters;
    }

    /// <summary>500 university voters — no geographic data, unique IDs starting with 9.</summary>
    private static async Task<List<Voter>> CreateUniversityVotersAsync(
        ApplicationDbContext context, int count, ILogger logger)
    {
        var studentNames = new[]
        {
            "عمر طارق", "ياسمين فؤاد", "كريم سامي", "هبة محمود", "أحمد عاطف",
            "نورهان خالد", "محمد سامح", "سلمى عادل", "يوسف رامي", "آية حسام",
            "مصطفى هاني", "ريم وليد", "حسين فاروق", "دينا ماجد", "عبدالرحمن نادر",
            "شيماء أيمن", "باسم جمال", "لمياء سعد", "طارق هشام", "مريم باسم",
            "أحمد سامح", "سارة ماهر", "كريم وائل", "منة الله أحمد", "زياد عمر"
        };

        var voters = new List<Voter>();
        for (int i = 1; i <= count; i++)
        {
            var isMale = _random.Next(2) == 0;
            var name = studentNames[_random.Next(studentNames.Length)];

            voters.Add(new Voter
            {
                UniqueIdentifier = $"9{i:D013}",
                FullName = name,
                DateOfBirth = new DateTime(1998 + _random.Next(7), _random.Next(1, 13), _random.Next(1, 28)),
                Gender = isMale ? "Male" : "Female",
                PhoneNumber = $"+20101{_random.Next(1000000, 9999999)}",
                Email = $"student{i}@student.test.com",
                GovernorateId = null,
                ConstituencyId = null
            });
        }

        context.Voters.AddRange(voters);
        await context.SaveChangesAsync();

        logger.LogInformation("✅ Created {Count} university student voters (no geographic data)", voters.Count);
        return voters;
    }

    /// <summary>1000 Maadi voters — all in Cairo/المعادي, unique IDs starting with 7.</summary>
    private static async Task<List<Voter>> CreateMaadiVotersAsync(
        ApplicationDbContext context, Governorate cairo, Constituency maadi, int count, ILogger logger)
    {
        var voters = new List<Voter>();

        for (int i = 1; i <= count; i++)
        {
            var isMale = _random.Next(2) == 0;
            var name = isMale
                ? _maleNames[_random.Next(_maleNames.Length)]
                : _femaleNames[_random.Next(_femaleNames.Length)];

            voters.Add(new Voter
            {
                UniqueIdentifier = $"7{i:D013}",
                FullName = name,
                DateOfBirth = new DateTime(1960 + _random.Next(45), _random.Next(1, 13), _random.Next(1, 28)),
                Gender = isMale ? "Male" : "Female",
                PhoneNumber = $"+20102{_random.Next(1000000, 9999999)}",
                Email = $"maadi{i}@maadi.test.com",
                GovernorateId = cairo.Id,
                ConstituencyId = maadi.Id
            });
        }

        context.Voters.AddRange(voters);
        await context.SaveChangesAsync();

        logger.LogInformation("✅ Created {Count} Maadi voters (Cairo - المعادي only)", voters.Count);
        return voters;
    }

    // ─────────────────────────────────────────────────────────────────
    // ELECTIONS
    // ─────────────────────────────────────────────────────────────────

    private static async Task CreatePresidentialElectionAsync(
        ApplicationDbContext context, IEncryptionService encryptionService,
        Admin admin, Organizer organizer, List<Voter> voters, ILogger logger)
    {
        logger.LogInformation("🏛️ Creating Presidential Election (1000 voters)...");

        using var rsa = RSA.Create(2048);
        var publicKeyPem = rsa.ExportRSAPublicKeyPem();
        var privateKeyEncrypted = encryptionService.Encrypt(rsa.ExportRSAPrivateKeyPem());

        var election = new Election
        {
            Title = "[TEST] انتخابات رئاسية 2026",
            Type = ElectionType.Presidential,
            Description = "انتخابات رئاسية تجريبية — 1000 ناخب موزعين عشوائياً على جميع محافظات مصر",
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(-1),
            Status = ElectionStatus.Closed,
            PublicKey = publicKeyPem,
            PrivateKeyEncrypted = privateKeyEncrypted,
            CreatedByAdminId = admin.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        context.Elections.Add(election);
        await context.SaveChangesAsync();

        context.ElectionOrganizers.Add(new ElectionOrganizer
        {
            ElectionId = election.Id,
            OrganizerId = organizer.Id,
            AssignedByAdminId = admin.Id,
            CanDecrypt = true,
            AssignedAt = DateTime.UtcNow.AddDays(-10)
        });

        // 5 candidates with photos (1.jpg → 5.jpg)
        var candidateData = new[]
        {
            ("أحمد فؤاد يوسف",    "النجمة",  "حزب مصر القوية"),
            ("سارة المنصوري",     "الهلال",  "حزب الحرية"),
            ("محمود عبدالعزيز",   "الشمس",   "حزب المستقبل"),
            ("نادية حسين",        "النسر",   "حزب النور"),
            ("كريم الشافعي",      "السنبلة", "حزب الوحدة"),
        };

        var candidates = new List<Candidate>();
        for (int i = 0; i < candidateData.Length; i++)
        {
            var (name, symbol, party) = candidateData[i];
            candidates.Add(new Candidate
            {
                ElectionId = election.Id,
                FullName = name,
                Symbol = symbol,
                PartyName = party,
                PhotoPath = null,
                OrderNumber = i + 1,
                ConstituencyId = null
            });
        }

        context.Candidates.AddRange(candidates);
        await context.SaveChangesAsync();

        // Register all voters
        var electionVoters = voters.Select(v => new ElectionVoter
        {
            ElectionId = election.Id,
            VoterId = v.Id,
            IsEligible = true,
            HasVoted = false
        }).ToList();

        context.ElectionVoters.AddRange(electionVoters);
        await context.SaveChangesAsync();

        // Cast ~80% of votes with weighted distribution
        var weights = new[] { 35, 28, 20, 12, 5 };
        var votingVoters = electionVoters
            .OrderBy(_ => _random.Next())
            .Take((int)(electionVoters.Count * 0.80))
            .ToList();

        var ballots = new List<Ballot>();
        foreach (var ev in votingVoters)
        {
            var candidateId = PickWeightedCandidate(candidates, weights);
            var (encVote, encKey, iv, authTag) = EncryptVote(candidateId, publicKeyPem);

            ballots.Add(new Ballot
            {
                ElectionId = election.Id,
                ElectionVoterId = ev.Id,
                EncryptedVote = encVote,
                EncryptedAESKey = encKey,
                IV = iv,
                AuthTag = authTag,
                CastAt = DateTime.UtcNow.AddDays(-_random.Next(1, 7)).AddHours(_random.Next(8, 20))
            });

            ev.HasVoted = true;
            ev.VotedAt = ballots.Last().CastAt;
        }

        context.Ballots.AddRange(ballots);
        await context.SaveChangesAsync();

        logger.LogInformation("🏛️ Presidential: {V} voters | {B} votes cast (80% turnout)",
            electionVoters.Count, ballots.Count);
    }

    private static async Task CreateUniversityElectionAsync(
        ApplicationDbContext context, IEncryptionService encryptionService,
        Admin admin, Organizer organizer, List<Voter> studentVoters, ILogger logger)
    {
        logger.LogInformation("🎓 Creating University Election (500 voters)...");

        using var rsa = RSA.Create(2048);
        var publicKeyPem = rsa.ExportRSAPublicKeyPem();
        var privateKeyEncrypted = encryptionService.Encrypt(rsa.ExportRSAPrivateKeyPem());

        var election = new Election
        {
            Title = "[TEST] انتخابات اتحاد طلاب جامعة القاهرة 2026",
            Type = "University",
            Description = "انتخابات اتحاد الطلاب — 500 طالب من جميع الكليات بدون بيانات جغرافية",
            StartDate = DateTime.UtcNow.AddDays(-3),
            EndDate = DateTime.UtcNow.AddDays(-1),
            Status = ElectionStatus.Closed,
            PublicKey = publicKeyPem,
            PrivateKeyEncrypted = privateKeyEncrypted,
            CreatedByAdminId = admin.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        context.Elections.Add(election);
        await context.SaveChangesAsync();

        context.ElectionOrganizers.Add(new ElectionOrganizer
        {
            ElectionId = election.Id,
            OrganizerId = organizer.Id,
            AssignedByAdminId = admin.Id,
            CanDecrypt = true,
            AssignedAt = DateTime.UtcNow.AddDays(-5)
        });

        // 4 student candidates with photos 6-9
        var candidateData = new[]
        {
            ("يوسف عبدالرحمن", "الكتاب",  "قائمة المستقبل"),
            ("مريم الشافعي",   "القلم",   "قائمة التغيير"),
            ("أحمد جابر",      "النجمة",  "قائمة الأمل"),
            ("نور حسين",       "المصباح", "مستقل"),
        };

        var candidates = new List<Candidate>();
        for (int i = 0; i < candidateData.Length; i++)
        {
            var (name, symbol, party) = candidateData[i];
            candidates.Add(new Candidate
            {
                ElectionId = election.Id,
                FullName = name,
                Symbol = symbol,
                PartyName = party,
                PhotoPath = null,
                OrderNumber = i + 1,
                ConstituencyId = null
            });
        }

        context.Candidates.AddRange(candidates);
        await context.SaveChangesAsync();

        var electionVoters = studentVoters.Select(v => new ElectionVoter
        {
            ElectionId = election.Id,
            VoterId = v.Id,
            IsEligible = true,
            HasVoted = false
        }).ToList();

        context.ElectionVoters.AddRange(electionVoters);
        await context.SaveChangesAsync();

        var weights = new[] { 40, 30, 20, 10 };
        var votingStudents = electionVoters
            .OrderBy(_ => _random.Next())
            .Take((int)(electionVoters.Count * 0.85))
            .ToList();

        var ballots = new List<Ballot>();
        foreach (var ev in votingStudents)
        {
            var candidateId = PickWeightedCandidate(candidates, weights);
            var (encVote, encKey, iv, authTag) = EncryptVote(candidateId, publicKeyPem);

            ballots.Add(new Ballot
            {
                ElectionId = election.Id,
                ElectionVoterId = ev.Id,
                EncryptedVote = encVote,
                EncryptedAESKey = encKey,
                IV = iv,
                AuthTag = authTag,
                CastAt = DateTime.UtcNow.AddDays(-_random.Next(1, 3)).AddHours(_random.Next(8, 18))
            });

            ev.HasVoted = true;
            ev.VotedAt = ballots.Last().CastAt;
        }

        context.Ballots.AddRange(ballots);
        await context.SaveChangesAsync();

        logger.LogInformation("🎓 University: {V} voters | {B} votes cast (85% turnout)",
            electionVoters.Count, ballots.Count);
    }

    private static async Task CreateMaadiParliamentaryElectionAsync(
        ApplicationDbContext context, IEncryptionService encryptionService,
        Admin admin, Organizer organizer,
        List<Voter> maadiVoters,
        Governorate cairo, Constituency maadi,
        ILogger logger)
    {
        logger.LogInformation("🏠 Creating Parliamentary Election — دائرة المعادي (1000 voters)...");

        using var rsa = RSA.Create(2048);
        var publicKeyPem = rsa.ExportRSAPublicKeyPem();
        var privateKeyEncrypted = encryptionService.Encrypt(rsa.ExportRSAPrivateKeyPem());

        // IMPORTANT: only Maadi constituency — fully isolated, no other districts
        var election = new Election
        {
            Title = "[TEST] انتخابات برلمانية — دائرة المعادي 2026",
            Type = ElectionType.Parliamentary,
            Description = "انتخابات برلمانية لدائرة المعادي فقط — القاهرة. كل دائرة انتخابية مستقلة تماماً عن باقي الدوائر.",
            StartDate = DateTime.UtcNow.AddDays(-5),
            EndDate = DateTime.UtcNow.AddDays(-1),
            Status = ElectionStatus.Closed,
            PublicKey = publicKeyPem,
            PrivateKeyEncrypted = privateKeyEncrypted,
            CreatedByAdminId = admin.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };

        context.Elections.Add(election);
        await context.SaveChangesAsync();

        context.ElectionOrganizers.Add(new ElectionOrganizer
        {
            ElectionId = election.Id,
            OrganizerId = organizer.Id,
            AssignedByAdminId = admin.Id,
            CanDecrypt = true,
            AssignedAt = DateTime.UtcNow.AddDays(-8)
        });

        // ONLY Maadi candidates — ConstituencyId = maadi.Id
        var candidateData = new[]
        {
            ("عبدالرحمن السيد المعادي",  "المفتاح", "حزب البناء"),
            ("نادية فاروق حسين",         "الميزان", "حزب التقدم"),
            ("محمد رشدي إبراهيم",        "الشجرة",  "مستقل"),
            ("أمل الشرقاوي",             "النجمة",  "حزب المستقبل"),
        };

        var candidates = new List<Candidate>();
        for (int i = 0; i < candidateData.Length; i++)
        {
            var (name, symbol, party) = candidateData[i];
            candidates.Add(new Candidate
            {
                ElectionId = election.Id,
                FullName = name,
                Symbol = symbol,
                PartyName = party,
                PhotoPath = null,
                OrderNumber = i + 1,
                ConstituencyId = maadi.Id   // ← ALL candidates belong to Maadi ONLY
            });
        }

        context.Candidates.AddRange(candidates);
        await context.SaveChangesAsync();

        // Register all Maadi voters
        var electionVoters = maadiVoters.Select(v => new ElectionVoter
        {
            ElectionId = election.Id,
            VoterId = v.Id,
            IsEligible = true,
            HasVoted = false
        }).ToList();

        context.ElectionVoters.AddRange(electionVoters);
        await context.SaveChangesAsync();

        // Cast ~75% votes — each voter votes for a candidate in their constituency (all Maadi)
        var weights = new[] { 40, 30, 20, 10 };
        var votingVoters = electionVoters
            .OrderBy(_ => _random.Next())
            .Take((int)(electionVoters.Count * 0.75))
            .ToList();

        var ballots = new List<Ballot>();
        foreach (var ev in votingVoters)
        {
            // All voters are in Maadi, all candidates are in Maadi — no filtering needed
            var candidateId = PickWeightedCandidate(candidates, weights);
            var (encVote, encKey, iv, authTag) = EncryptVote(candidateId, publicKeyPem);

            ballots.Add(new Ballot
            {
                ElectionId = election.Id,
                ElectionVoterId = ev.Id,
                EncryptedVote = encVote,
                EncryptedAESKey = encKey,
                IV = iv,
                AuthTag = authTag,
                CastAt = DateTime.UtcNow.AddDays(-_random.Next(1, 5)).AddHours(_random.Next(8, 20))
            });

            ev.HasVoted = true;
            ev.VotedAt = ballots.Last().CastAt;
        }

        context.Ballots.AddRange(ballots);
        await context.SaveChangesAsync();

        logger.LogInformation("🏠 Maadi Parliamentary: {V} voters | {B} votes cast (75% turnout) | دائرة: {D}",
            electionVoters.Count, ballots.Count, maadi.NameAr);
    }

    // ─────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────

    private static int PickWeightedCandidate(List<Candidate> candidates, int[] weights)
    {
        var total = weights.Take(candidates.Count).Sum();
        var roll = _random.Next(total);
        var cumulative = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += i < weights.Length ? weights[i] : weights[^1];
            if (roll < cumulative)
                return candidates[i].Id;
        }

        return candidates.Last().Id;
    }

    private static (byte[] EncryptedVote, byte[] EncryptedAesKey, byte[] IV, byte[] AuthTag) EncryptVote(
        int candidateId, string publicKeyPem)
    {
        var votePayload = JsonSerializer.Serialize(new { CandidateId = candidateId, Timestamp = DateTime.UtcNow });
        var voteBytes = Encoding.UTF8.GetBytes(votePayload);

        var aesKey = RandomNumberGenerator.GetBytes(32);
        var iv = RandomNumberGenerator.GetBytes(12);
        var encryptedVote = new byte[voteBytes.Length];
        var authTag = new byte[16];

        using (var aesGcm = new AesGcm(aesKey, 16))
            aesGcm.Encrypt(iv, voteBytes, encryptedVote, authTag);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        var encryptedAesKey = rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256);

        return (encryptedVote, encryptedAesKey, iv, authTag);
    }

    // ─────────────────────────────────────────────────────────────────
    // CLEAR
    // ─────────────────────────────────────────────────────────────────

    public static async Task ClearTestDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        logger.LogInformation("🗑️ Clearing test data...");

        var testElections = await context.Elections
            .Where(e => e.Title.Contains("[TEST]"))
            .Select(e => e.Id)
            .ToListAsync();

        if (!testElections.Any())
        {
            logger.LogInformation("ℹ️ No test data found to clear.");
            return;
        }

        // Delete in FK order
        var demoResults = context.DemographicResults.Where(dr => testElections.Contains(dr.ElectionId));
        context.DemographicResults.RemoveRange(demoResults);

        var geoResults = context.GeographicResults.Where(gr => testElections.Contains(gr.ElectionId));
        context.GeographicResults.RemoveRange(geoResults);

        var results = context.Results.Where(r => testElections.Contains(r.ElectionId));
        context.Results.RemoveRange(results);

        var ballots = context.Ballots.Where(b => testElections.Contains(b.ElectionId));
        context.Ballots.RemoveRange(ballots);

        var electionVoters = context.ElectionVoters.Where(ev => testElections.Contains(ev.ElectionId));
        context.ElectionVoters.RemoveRange(electionVoters);

        var candidates = context.Candidates.Where(c => testElections.Contains(c.ElectionId));
        context.Candidates.RemoveRange(candidates);

        var organizers = context.ElectionOrganizers.Where(eo => testElections.Contains(eo.ElectionId));
        context.ElectionOrganizers.RemoveRange(organizers);

        var elections = context.Elections.Where(e => testElections.Contains(e.Id));
        context.Elections.RemoveRange(elections);

        // Delete all test voters (all 3 types)
        var testVoters = context.Voters.Where(v =>
            v.Email != null &&
            (v.Email.EndsWith("@test.com") ||
             v.Email.EndsWith("@student.test.com") ||
             v.Email.EndsWith("@maadi.test.com")));
        context.Voters.RemoveRange(testVoters);

        await context.SaveChangesAsync();

        // Delete test organizer
        var testUser = await userManager.FindByEmailAsync("testorg@securevote.com");
        if (testUser != null)
        {
            var testOrg = await context.Organizers.FirstOrDefaultAsync(o => o.UserId == testUser.Id);
            if (testOrg != null) context.Organizers.Remove(testOrg);
            await context.SaveChangesAsync();
            await userManager.DeleteAsync(testUser);
        }

        logger.LogInformation("✅ Test data cleared! Removed {Count} elections and all associated data.", testElections.Count);
    }
}
