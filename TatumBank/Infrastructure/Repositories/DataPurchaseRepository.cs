
using Dapper;

namespace TatumBank.Infrastructure.Repositories
{
    public class DataPurchaseRepository : IDataPurchaseRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public DataPurchaseRepository(
            IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> PurchaseAsync(
            string accountNumber,
            string phoneNumber,
            string network,
            string planCode,
            decimal amount,
            string reference)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Lock the account row and check its balance.
                const string balanceSql = @"
                    SELECT Balance
                    FROM Accounts
                    WHERE AccountNumber = @AccountNumber
                    FOR UPDATE;";

                var balance = await connection.QuerySingleOrDefaultAsync<decimal?>(
                    balanceSql,
                    new { AccountNumber = accountNumber },
                    transaction);

                if (balance == null || balance.Value < amount)
                {
                    transaction.Rollback();
                    return false;
                }

                // 2. Deduct the purchase amount.
                const string updateBalanceSql = @"
                    UPDATE Accounts
                    SET Balance = Balance - @Amount
                    WHERE AccountNumber = @AccountNumber;";

                var rowsUpdated = await connection.ExecuteAsync(
                    updateBalanceSql,
                    new
                    {
                        AccountNumber = accountNumber,
                        Amount = amount
                    },
                    transaction);

                if (rowsUpdated != 1)
                {
                    transaction.Rollback();
                    return false;
                }

                // 3. Record the data purchase using your actual columns.
                const string insertTransactionSql = @"
                        INSERT INTO Transactions
                        (
                             Reference, SenderAccountNumber, RecipientAccountNumber, Amount,
                            TransactionType, TransactionCategory, Description, Status,
                            AccountNumber, PhoneNumber, Network, DataPlan, CreatedAt
                        )
                     VALUES
                        (
                            @Reference, @AccountNumber, NULL, @Amount,
                            'Debit', 'Data', @Description, 'Successful',
                             @AccountNumber, @PhoneNumber, @Network, @PlanCode, @CreatedAt
                        );";


                await connection.ExecuteAsync( insertTransactionSql,
                    new
                         {
                               Reference = reference,
                               AccountNumber = accountNumber,
                               Amount = amount,
                              Description = $"{planCode} data for {phoneNumber}",
                               PhoneNumber = phoneNumber,
                               Network = network,
                               PlanCode = planCode,
                               CreatedAt = DateTime.UtcNow
                         },
                        transaction);

                // 4. Commit both operations together.
                transaction.Commit();

                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
