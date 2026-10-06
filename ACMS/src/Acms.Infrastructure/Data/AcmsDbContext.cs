using Acms.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Acms.Infrastructure.Data;

public sealed class AcmsDbContext : DbContext
{
    // SQL Server and SQLite do not store DateTimeKind; mark values read back as UTC so the API
    // serializes them with a "Z" and clients do not mistake them for local time.
    private static readonly ValueConverter<DateTime, DateTime> Utc =
        new(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> NullableUtc =
        new(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

    public AcmsDbContext(DbContextOptions<AcmsDbContext> options)
        : base(options)
    {
    }

    public DbSet<AwacsServer> Servers => Set<AwacsServer>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AwacsServer>(e =>
        {
            e.ToTable("AwacsServers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.BaseUrl).HasMaxLength(400).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.UpdatedBy).HasMaxLength(256);
            e.Property(x => x.CreatedUtc).HasConversion(Utc);
            e.Property(x => x.UpdatedUtc).HasConversion(NullableUtc);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<AuditEntry>(e =>
        {
            e.ToTable("AuditEntries");
            e.HasKey(x => x.Id);
            e.Property(x => x.TimestampUtc).HasConversion(Utc);
            e.Property(x => x.UserName).HasMaxLength(256).IsRequired();
            e.Property(x => x.Action).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Outcome).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.ServerName).HasMaxLength(100);
            e.Property(x => x.WsId).HasMaxLength(64);
            e.Property(x => x.Message).HasMaxLength(2000);
            e.Property(x => x.CorrelationId).HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.TimestampUtc);
            e.HasIndex(x => new { x.ServerId, x.WsId });
            e.HasIndex(x => x.UserName);
        });
    }
}
