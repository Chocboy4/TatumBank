using Dapper;
using TatumBank.Models;

namespace TatumBank.Infrastructure.Repositories
{
    public class AdminUserRepository : IAdminUserRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AdminUserRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Admin?> GetByUsernameAsync(string username)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<Admin>(
                @"SELECT Id, Username, FullName, PasswordHash, IsActive, CreatedAt
                  FROM Admins WHERE Username = @Username;",
                new { Username = username });
        }
        public async Task<bool> AnyAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var count = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM Admins;");
            return count > 0;
        }

        public async Task CreateAsync(Admin admin)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                @"INSERT INTO Admins (Username, FullName, PasswordHash)
                  VALUES (@Username, @FullName, @PasswordHash);",
                admin);
        }

    }
}
