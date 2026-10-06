using Acms.Core.Domain;
using Acms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Acms.Web;

/// <summary>
/// Development convenience: creates the database and demo servers when configured.
/// Production databases are created from database/001_create_schema.sql.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        var config = app.Configuration;

        if (!config.GetValue<bool>("Database:EnsureCreated"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AcmsDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (app.Environment.IsDevelopment()
            && config.GetValue<bool>("Database:SeedDemoServers")
            && !await db.Servers.AnyAsync())
        {
            db.Servers.AddRange(
                new AwacsServer
                {
                    Name = "AWACS Demo 1",
                    BaseUrl = "http://awacs-demo-1.local/",
                    Description = "Demo data from the fake AWACS client",
                    CreatedUtc = DateTime.UtcNow,
                    CreatedBy = "seed",
                },
                new AwacsServer
                {
                    Name = "AWACS Demo 2",
                    BaseUrl = "http://awacs-demo-2.local/",
                    Description = "Demo data from the fake AWACS client",
                    CreatedUtc = DateTime.UtcNow,
                    CreatedBy = "seed",
                });

            await db.SaveChangesAsync();
        }
    }
}
