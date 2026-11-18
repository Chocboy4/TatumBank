using Dapper;
using System.Data;
using TatumBank.Application.Abstractions;

namespace TatumBank.Infrastructure.Repositories
{
    public class AirtimeRepository : IAirtimeRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AirtimeRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // Takes the money and writes a Pending row, or does neither.
        public async Task<bool> DebitAndRecordPendingAsync(
            string accountNumber,
            string phoneNumber,
            string network,
            decimal amount,
            string reference,
            string description)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != ConnectionState.Open) connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var rows = await connection.ExecuteAsync(
                    @"UPDATE Accounts
                      SET Balance = Balance - @Amount
                      WHERE AccountNumber = @AccountNumber
                        AND Balance >= @Amount
                        AND IsFrozen = 0;",
                    new { Amount = amount, AccountNumber = accountNumber },
                    transaction);

                if (rows != 1)
                {
                    transaction.Rollback();
                    return false;
                }

                await connection.ExecuteAsync(
                    @"INSERT INTO Transactions
                      (Reference, SenderAccountNumber, RecipientAccountNumber, Amount,
                       TransactionType, TransactionCategory, Description, Status,
                       AccountNumber, PhoneNumber, Network)
                      VALUES
                      (@Reference, @AccountNumber, NULL, @Amount,
                       'Debit', 'Airtime', @Description, 'Pending',
                       @AccountNumber, @PhoneNumber, @Network);",
                    new
                    {
                        Reference = reference,
                        AccountNumber = accountNumber,
                        Amount = amount,
                        Description = description,
                        PhoneNumber = phoneNumber,
                        Network = network
                    },
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