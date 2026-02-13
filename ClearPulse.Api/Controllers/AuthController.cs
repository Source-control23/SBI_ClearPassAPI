using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ClearPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ClearPulse.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ClearPulseDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(ClearPulseDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        public class LoginResponse
        {
            public string Token { get; set; }
            public DateTime Expires { get; set; }
            public string Username { get; set; }
            public string Role { get; set; }
            public string CircleCode { get; set; }
            public string TenantId { get; set; }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (!_config.GetValue<bool>("Auth:AllowLocalAuth"))
                return Forbid("Local authentication is disabled.");

            if (string.IsNullOrWhiteSpace(req?.Username) || string.IsNullOrWhiteSpace(req?.Password))
                return BadRequest("username and password required.");

            var user = await _db.SystemUsers
                                .Where(u => u.Username == req.Username && u.IsActive)
                                .FirstOrDefaultAsync();

            if (user == null)
                return Unauthorized("Invalid credentials.");

            if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials.");

            // Claims (same as LocalAuthHandler)
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role ?? string.Empty),
                new Claim("circleCode", user.CircleCode ?? string.Empty),
                new Claim("tenantId", user.TenantId ?? string.Empty)
            };

            // Check config for local signing key
            var signingKey = _config["Auth:Local:SigningKey"];
            if (string.IsNullOrWhiteSpace(signingKey))
            {
                // No signing key configured => return success info but no token
                return Ok(new LoginResponse
                {
                    Token = null,
                    Expires = DateTime.UtcNow,
                    Username = user.Username,
                    Role = user.Role,
                    CircleCode = user.CircleCode,
                    TenantId = user.TenantId
                });
            }

            var issuer = _config["Auth:Local:Issuer"] ?? _config["Auth:Authority"] ?? "ClearPulseLocal";
            var audience = _config["Auth:Audience"] ?? "ClearPulseApi";
            var expiryMinutes = _config.GetValue<int?>("Auth:Local:ExpiryMinutes") ?? 60;

            var keyBytes = Encoding.UTF8.GetBytes(signingKey);
            var securityKey = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(expiryMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Optionally update last login time (non-blocking)
            try
            {
                user.LastLogin = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            catch
            {
                // ignore DB update failure for login response
            }

            return Ok(new LoginResponse
            {
                Token = tokenString,
                Expires = expires,
                Username = user.Username,
                Role = user.Role,
                CircleCode = user.CircleCode,
                TenantId = user.TenantId
            });
        }
    }
}