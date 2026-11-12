
using Dapper;
using TatumBank.Models;

namespace TatumBank.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AccountRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> AddAsync(Account account)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
                INSERT INTO Accounts
                (
                    AccountNumber,
                    AccountHolderName,
                    PasswordHash,
                    Balance
                )
                VALUES
                (
                    @AccountNumber,
                    @AccountHolderName,
                    @PasswordHash,
                    @Balance
                );
                """;

            var rowsAffected = await connection.ExecuteAsync(
                sql,
                new
                {
                    account.AccountNumber,
                    account.AccountHolderName,
                    account.PasswordHash,
                    account.Balance
                });

            return rowsAffected > 0;
        }

        public async Task<Account?> GetAccountByNumberAsync(
            string accountNumber)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
                SELECT
                    Id,
                    AccountNumber,
                    AccountHolderName,
                    Balance,
                    PasswordHash,
                    CreatedAt
                FROM Accounts
                WHERE AccountNumber = @AccountNumber;
                """;

            return await connection.QueryFirstOrDefaultAsync<Account>(
                sql,
                new { AccountNumber = accountNumber });
        }

        public async Task<List<Account>> GetAllAsync()
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
                SELECT
                    Id,
                    AccountNumber,
                    AccountHolderName,
                    Balance,
                    CreatedAt
                FROM Accounts;
                """;

            var accounts = await connection.QueryAsync<Account>(sql);

            return accounts.ToList();
        }

        public Task<List<Transaction>> GetTransactionHistoryAsync(string accountNumber)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> TransferAsync(
            string senderAccountNumber,
            string recipientAccountNumber,
            decimal amount,
            string reference)
        {
            if (amount <= 0 ||
                string.IsNullOrWhiteSpace(senderAccountNumber) ||
                string.IsNullOrWhiteSpace(recipientAccountNumber) ||
                senderAccountNumber == recipientAccountNumber ||
                string.IsNullOrWhiteSpace(reference))
            {
                return false;
            }

            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                const string selectSql = """
                    SELECT
                        Id,
                        AccountNumber,
                        AccountHolderName,
                        Balance
                    FROM Accounts
                    WHERE AccountNumber = @AccountNumber
                    FOR UPDATE;
                    """;

                // Lock accounts in a consistent order to reduce deadlocks.
                string firstAccountNumber =
                    string.CompareOrdinal(
                        senderAccountNumber,
                        recipientAccountNumber) < 0
                        ? senderAccountNumber
                        : recipientAccountNumber;

                string secondAccountNumber =
                    firstAccountNumber == senderAccountNumber
                        ? recipientAccountNumber
                        : senderAccountNumber;

                var firstAccount =
                    await connection.QueryFirstOrDefaultAsync<Account>(
                        selectSql,
                        new { AccountNumber = firstAccountNumber },
                        transaction);

                var secondAccount =
                    await connection.QueryFirstOrDefaultAsync<Account>(
                        selectSql,
                        new { AccountNumber = secondAccountNumber },
                        transaction);

                if (firstAccount == null || secondAccount == null)
                {
                    transaction.Rollback();
                    return false;
                }

                var sender = firstAccountNumber == senderAccountNumber
                    ? firstAccount
                    : secondAccount;

                if (sender.Balance < amount)
                {
                    transaction.Rollback();
                    return false;
                }

                // 1. Debit the sender.
                const string debitSql = """
                    UPDATE Accounts
                    SET Balance = Balance - @Amount
                    WHERE AccountNumber = @AccountNumber
                      AND Balance >= @Amount;
                    """;

                int debited = await connection.ExecuteAsync(
                    debitSql,
                    new
                    {
                        Amount = amount,
                        AccountNumber = senderAccountNumber
                    },
                    transaction);

                if (debited != 1)
                {
                    transaction.Rollback();
                    return false;
                }

                // 2. Credit the recipient.
                const string creditSql = """
                    UPDATE Accounts
                    SET Balance = Balance + @Amount
                    WHERE AccountNumber = @AccountNumber;
                    """;

                int credited = await connection.ExecuteAsync(
                    creditSql,
                    new
                    {
                        Amount = amount,
                        AccountNumber = recipientAccountNumber
                    },
                    transaction);

                if (credited != 1)
                {
                    transaction.Rollback();
                    return false;
                }

                // 3. Record one transaction for the entire transfer.
                const string insertTransactionSql = """
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

                await connection.ExecuteAsync(
                    insertTransactionSql,
                    new
                    {
                        Reference = reference,
                        SenderAccountNumber = senderAccountNumber,
                        RecipientAccountNumber = recipientAccountNumber,
                        Amount = amount,
                        TransactionType = "Transfer",
                        Status = "Successful"
                    },
                    transaction);

                // 6. Commit all changes together.
                transaction.Commit();

                return true;
            }
            catch
            {
                // Roll back balance changes and transaction entries
                // if any operation fails before the commit.
                try
                {
                    transaction.Rollback();
                }
                catch
                {
                    // Preserve the original exception.
                }

                throw;
            }
        }

        public async Task<bool> UpdateBalance(
            string accountNumber,
            decimal newBalance)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
                UPDATE Accounts
                SET Balance = @Balance
                WHERE AccountNumber = @AccountNumber;
                """;

            var rowsAffected = await connection.ExecuteAsync(
                sql,
                new
                {
                    AccountNumber = accountNumber,
                    Balance = newBalance
                });

            return rowsAffected > 0;
        }
    }
}
