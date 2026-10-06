using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Acms.Web.Security;

/// <summary>Signs every request in as the configured development user. Development only.</summary>
public sealed class DevelopmentAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Development";

    private readonly AcmsSecurityOptions _security;

    public DevelopmentAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<AcmsSecurityOptions> security)
        : base(options, logger, encoder)
    {
        _security = security.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var dev = _security.DevAuthentication;
        var claims = new List<Claim> { new(ClaimTypes.Name, dev.UserName) };
        claims.AddRange(dev.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, SchemeName, ClaimTypes.Name, ClaimTypes.Role);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
