using Dapper;
using TatumBank.Models;

namespace TatumBank.Infrastructure.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public TransactionRepository(
            IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> AddAsync(Transfer transfer)
        {
            using var connection =
                _connectionFactory.CreateConnection();

            const string sql = """
                INSERT INTO Transactions
                (
                    Reference,
                    SenderAccountNumber,
                    RecipientAccountNumber,
                    Amount,
                    TransactionType,
                    Status
                )
                VALUES
                (
                    @Reference,
                    @SenderAccountNumber,
                    @RecipientAccountNumber,
                    @Amount,
                    @TransactionType,
                    @Status
                );
                """;

            var rowsAffected = await connection.ExecuteAsync(
                sql,
                transfer);

            return rowsAffected > 0;
        }

        public async Task<List<Transaction>> GetByAccountNumberAsync(
            string accountNumber)
        {
            using var connection =
                _connectionFactory.CreateConnection();

            const string sql = """
                SELECT
                    Id,
                    Reference,
                    SenderAccountNumber,
                    RecipientAccountNumber,
                    Amount,
                    TransactionType,
                    Status,
                    CreatedAt
                FROM Transactions
                WHERE SenderAccountNumber = @AccountNumber
                   OR RecipientAccountNumber = @AccountNumber
                ORDER BY CreatedAt DESC, Id DESC;
                """;

            var transactions = await connection.QueryAsync<Transaction>(
                sql,
                new { AccountNumber = accountNumber });

            return transactions.ToList();
        }
    }
}