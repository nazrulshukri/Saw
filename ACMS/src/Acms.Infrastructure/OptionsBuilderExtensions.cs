using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Acms.Infrastructure;

public static class OptionsBuilderExtensions
{
    /// <summary>
    /// Binds <paramref name="section"/> like <c>Bind</c>, except that a <c>string[]</c> set in
    /// configuration replaces the default from code. The plain binder appends configured items to
    /// the default, so <c>["WSTYPE","STATE"]</c> in appsettings.json would become
    /// <c>["WSTYPE","STATE","WSTYPE","STATE"]</c>.
    /// </summary>
    public static OptionsBuilder<T> BindReplacingArrays<T>(this OptionsBuilder<T> builder, IConfigurationSection section)
        where T : class
    {
        var arrayProperties = typeof(T).GetProperties()
            .Where(p => p.PropertyType == typeof(string[]) && p.CanWrite)
            .ToArray();

        return builder
            .Bind(section)
            .PostConfigure(options =>
            {
                foreach (var property in arrayProperties)
                {
                    var configured = section.GetSection(property.Name);
                    if (configured.GetChildren().Any())
                    {
                        property.SetValue(options, configured.Get<string[]>());
                    }
                }
            });
    }
}
