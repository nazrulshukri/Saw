using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Acms.Web.Security;

/// <summary>Adds ACMS role claims based on the user's Active Directory group membership.</summary>
public sealed class AdGroupClaimsTransformation : IClaimsTransformation
{
    private const string AuthenticationType = "AcmsRoles";

    private readonly AcmsSecurityOptions _options;
    private readonly ILogger<AdGroupClaimsTransformation> _logger;

    public AdGroupClaimsTransformation(IOptions<AcmsSecurityOptions> options, ILogger<AdGroupClaimsTransformation> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true
            || principal.Identities.Any(i => i.AuthenticationType == AuthenticationType))
        {
            return Task.FromResult(principal);
        }

        var roles = _options.RoleMappings
            .Where(mapping => mapping.Value.Any(group => IsMember(principal, group)))
            .Select(mapping => mapping.Key)
            .ToList();

        if (roles.Count == 0)
        {
            _logger.LogInformation("User {User} is not in any ACMS group", principal.Identity.Name);
            return Task.FromResult(principal);
        }

        var identity = new ClaimsIdentity(AuthenticationType, ClaimTypes.Name, ClaimTypes.Role);
        identity.AddClaims(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        principal.AddIdentity(identity);

        return Task.FromResult(principal);
    }

    private bool IsMember(ClaimsPrincipal principal, string group)
    {
        if (string.IsNullOrWhiteSpace(group))
        {
            return false;
        }

        // Group SIDs come through as claims on Windows identities.
        if (principal.HasClaim(c => c.Type == ClaimTypes.GroupSid && string.Equals(c.Value, group, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        try
        {
            if (OperatingSystem.IsWindows() && principal.Identity is WindowsIdentity windowsIdentity)
            {
                return new WindowsPrincipal(windowsIdentity).IsInRole(group);
            }

            return principal.IsInRole(group);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check membership of group {Group}", group);
            return false;
        }
    }
}
