using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SecureVote.Entities;
using SecureVote.Persistence;

namespace SecureVote.Extensions;

public static class DatabaseSeeder
{
    public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            // Ensure database is created and migrated
            await context.Database.MigrateAsync();

            // Seed default admin if not exists
            await SeedDefaultAdminAsync(userManager, context, logger);

            // Seed Egyptian governorates and constituencies
            await SeedGovernoratesAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedDefaultAdminAsync(
        UserManager<User> userManager,
        ApplicationDbContext context,
        ILogger logger)
    {
        const string adminEmail = "admin@securevote.com";
        const string adminUsername = "admin";
        const string adminPassword = "Admin@123456";

        // Check if admin already exists
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin != null)
        {
            logger.LogInformation("Default admin already exists, skipping seed");
            return;
        }

        logger.LogInformation("Creating default admin user...");

        // Create the user
        var user = new User
        {
            Email = adminEmail,
            UserName = adminUsername,
            Role = AppRoles.Admin,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, adminPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to create admin user: {Errors}", errors);
            return;
        }

        // Create admin profile
        var admin = new Admin
        {
            UserId = user.Id,
            FullName = "System Administrator",
            Department = "IT",
            PhoneNumber = "+1234567890"
        };

        context.Admins.Add(admin);
        await context.SaveChangesAsync();

        logger.LogInformation("Default admin created successfully!");
        logger.LogInformation("Email: {Email}", adminEmail);
        logger.LogInformation("Password: {Password}", adminPassword);
    }

    private static async Task SeedGovernoratesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Governorates.AnyAsync())
        {
            logger.LogInformation("Governorates already seeded, skipping");
            return;
        }

        logger.LogInformation("Seeding Egyptian governorates and constituencies...");

        // 27 Egyptian Governorates with 5 constituencies each
        var governoratesData = new (string Code, string NameAr, string NameEn, string[] Constituencies)[]
        {
            ("CAI", "القاهرة", "Cairo", new[] { "مصر الجديدة", "مدينة نصر", "المعادي", "شبرا", "حلوان" }),
            ("GIZ", "الجيزة", "Giza", new[] { "الدقي", "العجوزة", "الهرم", "6 أكتوبر", "البدرشين" }),
            ("ALX", "الإسكندرية", "Alexandria", new[] { "المنتزه", "وسط", "العامرية", "الجمرك", "العطارين" }),
            ("ASW", "أسوان", "Aswan", new[] { "أسوان", "دراو", "كوم أمبو", "إدفو", "نصر النوبة" }),
            ("AST", "أسيوط", "Asyut", new[] { "أسيوط", "ديروط", "القوصية", "منفلوط", "أبنوب" }),
            ("BHR", "البحيرة", "Beheira", new[] { "دمنهور", "كفر الدوار", "رشيد", "إيتاي البارود", "أبو حمص" }),
            ("BNS", "بني سويف", "Beni Suef", new[] { "بني سويف", "الواسطى", "ناصر", "إهناسيا", "ببا" }),
            ("DKH", "الدقهلية", "Dakahlia", new[] { "المنصورة", "طلخا", "ميت غمر", "أجا", "دكرنس" }),
            ("DMT", "دمياط", "Damietta", new[] { "دمياط", "فارسكور", "كفر سعد", "الزرقا", "رأس البر" }),
            ("FYM", "الفيوم", "Faiyum", new[] { "الفيوم", "سنورس", "إطسا", "طامية", "يوسف الصديق" }),
            ("GHR", "الغربية", "Gharbia", new[] { "طنطا", "المحلة الكبرى", "زفتى", "كفر الزيات", "السنطة" }),
            ("ISM", "الإسماعيلية", "Ismailia", new[] { "الإسماعيلية", "فايد", "القنطرة شرق", "القنطرة غرب", "التل الكبير" }),
            ("KFS", "كفر الشيخ", "Kafr El Sheikh", new[] { "كفر الشيخ", "دسوق", "فوه", "بيلا", "الحامول" }),
            ("LUX", "الأقصر", "Luxor", new[] { "الأقصر", "الطود", "إسنا", "أرمنت", "القرنة" }),
            ("MNF", "المنوفية", "Monufia", new[] { "شبين الكوم", "منوف", "الباجور", "أشمون", "تلا" }),
            ("MNA", "المنيا", "Minya", new[] { "المنيا", "ملوي", "بني مزار", "سمالوط", "أبو قرقاص" }),
            ("MTS", "مطروح", "Matrouh", new[] { "مرسى مطروح", "الحمام", "العلمين", "الضبعة", "سيوة" }),
            ("NBE", "شمال سيناء", "North Sinai", new[] { "العريش", "بئر العبد", "الشيخ زويد", "رفح", "الحسنة" }),
            ("PTS", "بورسعيد", "Port Said", new[] { "بورسعيد", "الضواحي", "المناخ", "الزهور", "بورفؤاد" }),
            ("QLB", "القليوبية", "Qalyubia", new[] { "بنها", "شبرا الخيمة", "القناطر الخيرية", "قليوب", "طوخ" }),
            ("QNA", "قنا", "Qena", new[] { "قنا", "نجع حمادي", "دشنا", "قوص", "أبو تشت" }),
            ("WAD", "الوادي الجديد", "New Valley", new[] { "الخارجة", "الداخلة", "الفرافرة", "باريس", "بلاط" }),
            ("SHR", "الشرقية", "Sharqia", new[] { "الزقازيق", "العاشر من رمضان", "بلبيس", "أبو حماد", "فاقوس" }),
            ("SUH", "سوهاج", "Sohag", new[] { "سوهاج", "أخميم", "جرجا", "طهطا", "المراغة" }),
            ("SBE", "جنوب سيناء", "South Sinai", new[] { "الطور", "شرم الشيخ", "دهب", "نويبع", "طابا" }),
            ("SUZ", "السويس", "Suez", new[] { "السويس", "الأربعين", "عتاقة", "الجناين", "فيصل" }),
            ("BSR", "البحر الأحمر", "Red Sea", new[] { "الغردقة", "سفاجا", "القصير", "مرسى علم", "رأس غارب" })
        };

        foreach (var (code, nameAr, nameEn, constituencies) in governoratesData)
        {
            var governorate = new Governorate
            {
                Code = code,
                NameAr = nameAr,
                NameEn = nameEn
            };

            context.Governorates.Add(governorate);
            await context.SaveChangesAsync(); // Save to get the Id

            var index = 1;
            foreach (var constName in constituencies)
            {
                context.Constituencies.Add(new Constituency
                {
                    GovernorateId = governorate.Id,
                    Code = $"{code}-{index:D2}",
                    NameAr = constName,
                    NameEn = $"{nameEn} District {index}"
                });
                index++;
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded 27 governorates with 135 constituencies successfully");
    }
}
