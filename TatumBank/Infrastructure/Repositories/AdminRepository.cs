using Dapper;
using TatumBank.DTOs;

namespace TatumBank.Infrastructure.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AdminRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<PagedResult<AdminAccountDto>> SearchAccountsAsync(AdminAccountFilterDto filter)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string where = @"
                WHERE (@Pattern IS NULL OR AccountNumber LIKE @Pattern OR AccountHolderName LIKE @Pattern)
                  AND (@IsFrozen IS NULL OR IsFrozen = @IsFrozen)
                  AND (@MinBalance IS NULL OR Balance >= @MinBalance)
                  AND (@MaxBalance IS NULL OR Balance <= @MaxBalance)";

            var parameters = new
            {
                Pattern = string.IsNullOrWhiteSpace(filter.Search) ? null : $"%{filter.Search.Trim()}%",
                filter.IsFrozen,
                filter.MinBalance,
                filter.MaxBalance,
                PageSize = filter.PageSize,
                Offset = (filter.Page - 1) * filter.PageSize
            };

            var total = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Accounts " + where, parameters);

            var items = await connection.QueryAsync<AdminAccountDto>(
                @"SELECT AccountNumber, AccountHolderName, Balance, IsFrozen
                  FROM Accounts " + where + @"
                  ORDER BY AccountNumber
                  LIMIT @PageSize OFFSET @Offset;", parameters);

            return new PagedResult<AdminAccountDto>
            {
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = total,
                Items = items.ToList()
            };
        }

        public async Task<AdminAccountDto?> GetAccountAsync(string accountNumber)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<AdminAccountDto>(
                @"SELECT AccountNumber, AccountHolderName, Balance, IsFrozen
                  FROM Accounts WHERE AccountNumber = @AccountNumber;",
                new { AccountNumber = accountNumber });
        }

        public async Task<bool> SetFrozenAsync(string accountNumber, bool frozen)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = await connection.ExecuteAsync(
                "UPDATE Accounts SET IsFrozen = @Frozen WHERE AccountNumber = @AccountNumber;",
                new { Frozen = frozen, AccountNumber = accountNumber });
            return rows == 1;
        }

        public async Task<PagedResult<AdminTransactionDto>> SearchTransactionsAsync(AdminTransactionFilterDto filter)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string where = @"
                WHERE (@AccountNumber IS NULL
                       OR SenderAccountNumber = @AccountNumber
                       OR RecipientAccountNumber = @AccountNumber)
                  AND (@Reference IS NULL OR Reference = @Reference)
                  AND (@Category IS NULL OR TransactionCategory = @Category)
                  AND (@Status IS NULL OR Status = @Status)
                  AND (@MinAmount IS NULL OR Amount >= @MinAmount)
                  AND (@MaxAmount IS NULL OR Amount <= @MaxAmount)
                  AND (@StartDate IS NULL OR CreatedAt >= @StartDate)
                  AND (@EndDateExclusive IS NULL OR CreatedAt < @EndDateExclusive)";

            var parameters = new
            {
                AccountNumber = Clean(filter.AccountNumber),
                Reference = Clean(filter.Reference),
                Category = Clean(filter.Category),
                Status = Clean(filter.Status),
                filter.MinAmount,
                filter.MaxAmount,
                StartDate = filter.StartDate?.Date,
                EndDateExclusive = filter.EndDate?.Date.AddDays(1),
                PageSize = filter.PageSize,
                Offset = (filter.Page - 1) * filter.PageSize
            };

            var total = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Transactions " + where, parameters);

            var items = await connection.QueryAsync<AdminTransactionDto>(
                @"SELECT Id, Reference, SenderAccountNumber, RecipientAccountNumber, Amount,
                         TransactionCategory, Status, Description, PhoneNumber, Network, DataPlan, CreatedAt
                  FROM Transactions " + where + @"
                  ORDER BY CreatedAt DESC, Id DESC
                  LIMIT @PageSize OFFSET @Offset;", parameters);

            return new PagedResult<AdminTransactionDto>
            {
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = total,
                Items = items.ToList()
            };
        }

        private static string? Clean(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}