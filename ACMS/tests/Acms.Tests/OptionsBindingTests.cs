using Acms.Core.Services;
using Acms.Infrastructure;
using Acms.Infrastructure.Awacs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Acms.Tests;

public class OptionsBindingTests
{
    [Fact]
    public void Configured_arrays_replace_code_defaults_and_missing_ones_keep_them()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EquipmentRules:ReadOnlyAttributes:0"] = "WSID",
                ["EquipmentRules:ReadOnlyAttributes:1"] = "WSTYPE",
                ["Awacs:WorkstationIdNames:0"] = "EQUIPMENTID",
                ["Awacs:TimeoutSeconds"] = "10",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<EquipmentRulesOptions>().BindReplacingArrays(configuration.GetSection("EquipmentRules"));
        services.AddOptions<AwacsOptions>().BindReplacingArrays(configuration.GetSection("Awacs"));
        using var provider = services.BuildServiceProvider();

        var rules = provider.GetRequiredService<IOptions<EquipmentRulesOptions>>().Value;
        var awacs = provider.GetRequiredService<IOptions<AwacsOptions>>().Value;

        Assert.Equal(["WSID", "WSTYPE"], rules.ReadOnlyAttributes);
        Assert.Equal(["EQUIPMENTID"], awacs.WorkstationIdNames);
        Assert.Equal(["ws", "workstation"], awacs.WorkstationElementNames);
        Assert.Equal(10, awacs.TimeoutSeconds);
    }
}
