using Acms.Core.Abstractions;
using Acms.Core.Services;
using Acms.Infrastructure.Awacs;
using Acms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Acms.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the database, repositories, AWACS connector and ACMS services.</summary>
    /// <param name="allowFakeAwacs">Only pass true in the Development environment.</param>
    public static IServiceCollection AddAcms(this IServiceCollection services, IConfiguration configuration, bool allowFakeAwacs)
    {
        var connectionString = configuration.GetConnectionString("Acms")
            ?? throw new InvalidOperationException("Connection string 'Acms' is not configured.");

        var provider = configuration["Database:Provider"] ?? "SqlServer";

        services.AddDbContext<AcmsDbContext>(options =>
        {
            if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(connectionString);
            }
            else
            {
                options.UseSqlServer(connectionString);
            }
        });

        services.AddScoped<IServerRepository, EfServerRepository>();
        services.AddScoped<IAuditLog, EfAuditLog>();

        services.AddOptions<AwacsOptions>().BindReplacingArrays(configuration.GetSection(AwacsOptions.SectionName));
        services.AddOptions<EquipmentRulesOptions>().BindReplacingArrays(configuration.GetSection(EquipmentRulesOptions.SectionName));

        var awacs = configuration.GetSection(AwacsOptions.SectionName).Get<AwacsOptions>() ?? new AwacsOptions();

        if (awacs.UseFake && allowFakeAwacs)
        {
            services.AddSingleton<IAwacsClient, FakeAwacsClient>();
        }
        else
        {
            services.AddHttpClient<IAwacsClient, AwacsHttpClient>(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(awacs.TimeoutSeconds);
                })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    UseDefaultCredentials = awacs.UseDefaultCredentials,
                });
        }

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<EquipmentChangeService>();
        services.AddScoped<ServerAdminService>();
        services.AddScoped<BulkUpdateService>();

        return services;
    }
}
