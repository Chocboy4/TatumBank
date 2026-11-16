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
                    IsFrozen,
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
                    IsFrozen,
                    CreatedAt
                FROM Accounts;
                """;

            var accounts = await connection.QueryAsync<Account>(sql);

            return accounts.ToList();
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

            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                const string selectSql = """
                    SELECT
                        Id,
                        AccountNumber,
                        AccountHolderName,
                        Balance,
                        IsFrozen
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

                // The sender row is locked now, so these checks cannot go stale.
                if (sender.IsFrozen || sender.Balance < amount)
                {
                    transaction.Rollback();
                    return false;
                }

                // 1. Debit the sender.
                const string debitSql = """
                    UPDATE Accounts
                    SET Balance = Balance - @Amount
                    WHERE AccountNumber = @AccountNumber
                      AND Balance >= @Amount
                      AND IsFrozen = 0;
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

                // 2. Credit the recipient. A frozen account can still receive.
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

                // 3. Record one transaction for the whole transfer.
                const string insertTransactionSql = """
                    INSERT INTO Transactions
                    (
                        Reference,
                        SenderAccountNumber,
                        RecipientAccountNumber,
                        Amount,
                        TransactionType,
                        TransactionCategory,
                        Description,
                        Status
                    )
                    VALUES
                    (
                        @Reference,
                        @SenderAccountNumber,
                        @RecipientAccountNumber,
                        @Amount,
                        'Debit',
                        'Transfer',
                        @Description,
                        'Successful'
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
                        Description = $"Transfer from {senderAccountNumber} to {recipientAccountNumber}"
                    },
                    transaction);

                // 4. Commit everything together.
                transaction.Commit();

                return true;
            }
            catch
            {
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

        
        public async Task<bool> CreditAsync(string accountNumber, decimal amount)
        {
            if (amount <= 0) return false;

            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
                UPDATE Accounts
                SET Balance = Balance + @Amount
                WHERE AccountNumber = @AccountNumber;
                """;

            var rowsAffected = await connection.ExecuteAsync(
                sql,
                new { AccountNumber = accountNumber, Amount = amount });

            return rowsAffected == 1;
        }

        // Removes money only if the funds are there and the account is not frozen,
        // all in the same statement.
        public async Task<bool> DebitAsync(string accountNumber, decimal amount)
        {
            if (amount <= 0) return false;

            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
                UPDATE Accounts
                SET Balance = Balance - @Amount
                WHERE AccountNumber = @AccountNumber
                  AND Balance >= @Amount
                  AND IsFrozen = 0;
                """;

            var rowsAffected = await connection.ExecuteAsync(
                sql,
                new { AccountNumber = accountNumber, Amount = amount });

            return rowsAffected == 1;
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

        // Adds the money and records the deposit in one database transaction.
public async Task<bool> DepositAsync(
    string accountNumber,
    decimal amount,
    string reference)
{
    if (amount <= 0 ||
        string.IsNullOrWhiteSpace(accountNumber) ||
        string.IsNullOrWhiteSpace(reference))
    {
        return false;
    }

    using var connection = _connectionFactory.CreateConnection();

    if (connection.State != System.Data.ConnectionState.Open)
        connection.Open();

    using var transaction = connection.BeginTransaction();

    try
    {
        const string creditSql = """
            UPDATE Accounts
            SET Balance = Balance + @Amount
            WHERE AccountNumber = @AccountNumber;
            """;

        var rows = await connection.ExecuteAsync(
            creditSql,
            new { AccountNumber = accountNumber, Amount = amount },
            transaction);

        if (rows != 1)
        {
            transaction.Rollback();
            return false;
        }

        const string insertSql = """
            INSERT INTO Transactions
            (
                Reference,
                SenderAccountNumber,
                RecipientAccountNumber,
                Amount,
                TransactionType,
                TransactionCategory,
                Description,
                Status,
                AccountNumber
            )
            VALUES
            (
                @Reference,
                NULL,
                @AccountNumber,
                @Amount,
                'Credit',
                'Deposit',
                'Deposit',
                'Successful',
                @AccountNumber
            );
            """;

        await connection.ExecuteAsync(
            insertSql,
            new { Reference = reference, AccountNumber = accountNumber, Amount = amount },
            transaction);

        transaction.Commit();
        return true;
    }
    catch
    {
        try { transaction.Rollback(); } catch { }
        throw;
    }
}

public async Task<bool> WithdrawAsync(
    string accountNumber,
    decimal amount,
    string reference)
{
    if (amount <= 0 ||
        string.IsNullOrWhiteSpace(accountNumber) ||
        string.IsNullOrWhiteSpace(reference))
    {
        return false;
    }

    using var connection = _connectionFactory.CreateConnection();

    if (connection.State != System.Data.ConnectionState.Open)
        connection.Open();

    using var transaction = connection.BeginTransaction();

    try
    {
        const string debitSql = """
            UPDATE Accounts
            SET Balance = Balance - @Amount
            WHERE AccountNumber = @AccountNumber
              AND Balance >= @Amount
              AND IsFrozen = 0;
            """;

        var rows = await connection.ExecuteAsync(
            debitSql,
            new { AccountNumber = accountNumber, Amount = amount },
            transaction);

        if (rows != 1)
        {
            transaction.Rollback();
            return false;
        }

        const string insertSql = """
            INSERT INTO Transactions
            (
                Reference,
                SenderAccountNumber,
                RecipientAccountNumber,
                Amount,
                TransactionType,
                TransactionCategory,
                Description,
                Status,
                AccountNumber
            )
            VALUES
            (
                @Reference,
                @AccountNumber,
                NULL,
                @Amount,
                'Debit',
                'Withdrawal',
                'Withdrawal',
                'Successful',
                @AccountNumber
            );
            """;

        await connection.ExecuteAsync(
            insertSql,
            new { Reference = reference, AccountNumber = accountNumber, Amount = amount },
            transaction);

        transaction.Commit();
        return true;
    }
    catch
    {
        try { transaction.Rollback(); } catch { }
        throw;
    }
}
    }
}