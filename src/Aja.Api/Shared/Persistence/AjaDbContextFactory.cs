using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aja.Api.Shared.Persistence;

public sealed class AjaDbContextFactory : IDesignTimeDbContextFactory<AjaDbContext>
{
    public AjaDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("AJA_DESIGN_CONNECTION") ?? "Data Source=aja.db";
        var options = new DbContextOptionsBuilder<AjaDbContext>().UseSqlite(connection).Options;
        return new AjaDbContext(options);
    }
}
