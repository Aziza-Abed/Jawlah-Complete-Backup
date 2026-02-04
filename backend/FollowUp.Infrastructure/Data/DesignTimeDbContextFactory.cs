using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FollowUp.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FollowUpDbContext>
{
    public FollowUpDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../FollowUp.API"))
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<FollowUpDbContext>();
        optionsBuilder.UseSqlServer(connectionString, options =>
        {
            options.UseNetTopologySuite();
        });

        return new FollowUpDbContext(optionsBuilder.Options);
    }
}
