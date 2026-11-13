using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TatumBank.Infrastructure.Repositories;
using TatumBank.Models;

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
        private readonly PasswordHasher<Admin> _hasher = new();

        public AdminAuthController(IConfiguration configuration, IAdminUserRepository adminUsers)
        {
            _configuration = configuration;
            _adminUsers = adminUsers;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(AdminLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username and password are required.");
            }

            var admin = await _adminUsers.GetByUsernameAsync(request.Username.Trim().ToLowerInvariant());

            if (admin == null || !admin.IsActive ||
                _hasher.VerifyHashedPassword(admin, admin.PasswordHash, request.Password)
                    == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Invalid username or password.");
            }

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