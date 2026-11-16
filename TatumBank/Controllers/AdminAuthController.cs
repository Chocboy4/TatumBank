using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TatumBank.Infrastructure.Repositories;
using TatumBank.Models;
using TatumBank.Services;

namespace TatumBank.Controllers
{
    public class AdminLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/admin/auth")]
    [AllowAnonymous]
    public class AdminAuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IAdminUserRepository _adminUsers;
        private readonly IAuditService _audit;
        private readonly PasswordHasher<Admin> _hasher = new();

        public AdminAuthController(
            IConfiguration configuration,
            IAdminUserRepository adminUsers,
            IAuditService audit)
        {
            _configuration = configuration;
            _adminUsers = adminUsers;
            _audit = audit;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(AdminLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username and password are required.");
            }

            var username = request.Username.Trim().ToLowerInvariant();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var admin = await _adminUsers.GetByUsernameAsync(username);

            if (admin == null || !admin.IsActive ||
                _hasher.VerifyHashedPassword(admin, admin.PasswordHash, request.Password)
                    == PasswordVerificationResult.Failed)
            {
                await _audit.LogAsync(admin?.Id, username, "AdminLoginFailed", "Admin", username, $"IP: {ip}");
                return Unauthorized("Invalid username or password.");
            }

            await _audit.LogAsync(admin.Id, admin.FullName, "AdminLogin", "Admin", admin.Username, $"IP: {ip}");

            var jwtSection = _configuration.GetSection("Jwt");
            var key = jwtSection["Key"]
                ?? throw new InvalidOperationException("JWT signing key is not configured.");
            var issuer = jwtSection["Issuer"]
                ?? throw new InvalidOperationException("JWT issuer is not configured.");
            var audience = jwtSection["Audience"]
                ?? throw new InvalidOperationException("JWT audience is not configured.");

            // Shorter lifetime than customers, since admin tokens are more powerful.
            var expiryMinutes = 15;

            var claims = new[]
            {
                new Claim("AdminId", admin.Id.ToString()),
                new Claim(ClaimTypes.Name, admin.FullName),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials);

            return Ok(new
            {
                message = "Login successful.",
                admin = new { admin.Username, admin.FullName },
                accessToken = new JwtSecurityTokenHandler().WriteToken(token),
                tokenType = "Bearer",
                expiresIn = expiryMinutes * 60
            });
        }
    }
}