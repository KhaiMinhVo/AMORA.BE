using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Amora.Infrastructure.Data;

public sealed class AmoraDbContextFactory : IDesignTimeDbContextFactory<AmoraDbContext>
{
    public AmoraDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AmoraDbContext>();
        optionsBuilder.UseNpgsql("Host=127.0.0.1;Port=5444;Database=AmoraCoreDb;Username=postgres;Password=postgres");
        return new AmoraDbContext(optionsBuilder.Options);
    }
}