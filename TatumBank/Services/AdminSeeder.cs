using Microsoft.AspNetCore.Identity;
using TatumBank.Infrastructure.Repositories;
using TatumBank.Models;

namespace TatumBank.Services
{
    public class AdminSeeder
    {
        private readonly IAdminUserRepository _repository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminSeeder> _logger;

        public AdminSeeder(
            IAdminUserRepository repository,
            IConfiguration configuration,
            ILogger<AdminSeeder> logger)
        {
            _repository = repository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            _logger.LogWarning("AdminSeeder: started.");

            if (await _repository.AnyAsync())
            {
                _logger.LogWarning("AdminSeeder: Admins table already has a row, nothing to do.");
                return;
            }

            var username = _configuration["Admin:Username"];
            var password = _configuration["Admin:Password"];

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("AdminSeeder: Admin:Username or Admin:Password was not found in configuration.");
                return;
            }

            var admin = new Admin
            {
                Username = username.Trim().ToLowerInvariant(),
                FullName = _configuration["Admin:FullName"] ?? "Administrator"
            };

            admin.PasswordHash = new PasswordHasher<Admin>().HashPassword(admin, password);

            await _repository.CreateAsync(admin);

            _logger.LogWarning("AdminSeeder: created admin '{Username}'.", admin.Username);
        }
    }
}