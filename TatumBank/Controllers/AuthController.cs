using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TatumBank.DTOs;
using TatumBank.Infrastructure.Repositories;
using TatumBank.Models;
using TatumBank.Services;

namespace TatumBank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IAccountRepository _accountRepository;
        private readonly PasswordService _passwordService;

        public AuthController(
            IConfiguration configuration,
            IAccountRepository accountRepository,
            PasswordService passwordService)
        {
            _configuration = configuration;
            _accountRepository = accountRepository;
            _passwordService = passwordService;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AccountNumber) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Account number and password are required.");
            }

            var account = await _accountRepository.GetAccountByNumberAsync(request.AccountNumber);

            if (account == null ||
                !_passwordService.VerifyPassword(account, account.PasswordHash, request.Password))
            {
                return Unauthorized("Invalid account number or password.");
            }

            var jwtSection = _configuration.GetSection("Jwt");

            var key = jwtSection["Key"]
                ?? throw new InvalidOperationException("JWT signing key is not configured.");

            var issuer = jwtSection["Issuer"]
                ?? throw new InvalidOperationException("JWT issuer is not configured.");

            var audience = jwtSection["Audience"]
                ?? throw new InvalidOperationException("JWT audience is not configured.");

            var expiryMinutes = int.TryParse(jwtSection["ExpiryMinutes"], out var minutes)
                ? minutes
                : 30;

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, account.AccountNumber),
                new Claim(ClaimTypes.Name, account.AccountHolderName)
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                message = "Login successful.",
                account = new
                {
                    account.AccountNumber,
                    account.AccountHolderName
                },
                accessToken = tokenString,
                tokenType = "Bearer",
                expiresIn = expiryMinutes * 60
            });
        }
    }
}