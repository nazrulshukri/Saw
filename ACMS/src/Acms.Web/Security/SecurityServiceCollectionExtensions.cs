using System.Security.Claims;
using Acms.Core.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;

namespace Acms.Web.Security;

public static class SecurityServiceCollectionExtensions
{
    /// <summary>
    /// Windows Authentication (Negotiate) with AD groups mapped to ACMS roles.
    /// Every page and API requires at least the Viewer role unless marked [AllowAnonymous].
    /// </summary>
    public static IServiceCollection AddAcmsSecurity(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var section = configuration.GetSection(AcmsSecurityOptions.SectionName);
        services.Configure<AcmsSecurityOptions>(section);

        var options = section.Get<AcmsSecurityOptions>() ?? new AcmsSecurityOptions();
        var useDevAuth = environment.IsDevelopment() && options.DevAuthentication.Enabled;

        if (useDevAuth)
        {
            services.AddAuthentication(DevelopmentAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
                    DevelopmentAuthenticationHandler.SchemeName, null);
        }
        else
        {
            // On IIS this defers to the IIS Windows Authentication module.
            services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
            services.AddTransient<IClaimsTransformation, AdGroupClaimsTransformation>();
        }

        services.AddAuthorization(authorization =>
        {
            authorization.AddPolicy(AcmsPolicies.CanView, p => p.RequireAuthenticatedUser().RequireRole(AcmsRoles.All));
            authorization.AddPolicy(AcmsPolicies.CanEditEquipment, p => p.RequireAuthenticatedUser()
                .RequireRole(AcmsRoles.Engineer, AcmsRoles.Administrator));
            authorization.AddPolicy(AcmsPolicies.CanAdminister, p => p.RequireAuthenticatedUser()
                .RequireRole(AcmsRoles.Administrator));

            authorization.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireRole(AcmsRoles.All)
                .Build();
        });

        return services;
    }

    public static string AcmsUserName(this ClaimsPrincipal user) =>
        string.IsNullOrWhiteSpace(user.Identity?.Name) ? "unknown" : user.Identity.Name;

    public static IEnumerable<string> AcmsRoleNames(this ClaimsPrincipal user) =>
        AcmsRoles.All.Where(user.IsInRole);
}
