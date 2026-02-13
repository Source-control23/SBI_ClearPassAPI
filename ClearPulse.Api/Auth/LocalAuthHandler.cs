using ClearPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;


namespace ClearPulse.Api.Auth
{
    public class LocalAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly ClearPulseDbContext _db;
        public LocalAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
                                ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock,
                                ClearPulseDbContext db)
            : base(options, logger, encoder, clock) => _db = db;

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
                return AuthenticateResult.NoResult();

            var header = Request.Headers.Authorization.ToString();
            if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                return AuthenticateResult.NoResult();

            var raw = header.Substring("Basic ".Length);
            string decoded;
            try { decoded = Encoding.UTF8.GetString(Convert.FromBase64String(raw)); }
            catch { return AuthenticateResult.Fail("Invalid base64"); }

            var parts = decoded.Split(':', 2);
            if (parts.Length != 2) return AuthenticateResult.Fail("Invalid basic auth format");

            var username = parts[0];
            var password = parts[1];

            var user = await _db.SystemUsers.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            if (user == null) return AuthenticateResult.Fail("User not found");

            if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return AuthenticateResult.Fail("Invalid credentials");

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("circleCode", user.CircleCode ?? string.Empty),
            new Claim("tenantId", user.TenantId ?? string.Empty)
        };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }

}
