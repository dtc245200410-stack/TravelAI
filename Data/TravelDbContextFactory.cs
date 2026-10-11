using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TravelAI.Data;

// EF tooling can create migrations without starting the web app or seeding data.
public sealed class TravelDbContextFactory : IDesignTimeDbContextFactory<TravelDbContext>
{
    public TravelDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
        return new TravelDbContext(new DbContextOptionsBuilder<TravelDbContext>()
            .UseSqlServer(configuration.GetConnectionString("DefaultConnection")).Options);
    }
}
