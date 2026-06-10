using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SecureVote.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use a hardcoded connection string for design-time operations
        optionsBuilder.UseSqlServer("Server=.;Database=SecureVoteDb;Trusted_Connection=true;TrustServerCertificate=true");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
