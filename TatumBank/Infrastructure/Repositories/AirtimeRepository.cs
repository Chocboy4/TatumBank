
using Dapper;
using TatumBank.DTOs;
using TatumBank.Models;

namespace TatumBank.Infrastructure.Repositories
{
    public class AirtimeRepository : IAirtimeRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AirtimeRepository(
            IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<AirtimePurchaseResponseDto> PurchaseAsync(
            string accountNumber,
            string phoneNumber,
            string network,
            decimal amount)
        {
            using var connection = _connectionFactory.CreateConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Debit only if the account has sufficient funds.
                const string debitSql = @"
                    UPDATE Accounts
                    SET Balance = Balance - @Amount
                    WHERE AccountNumber = @AccountNumber
                      AND Balance >= @Amount;";

                var rowsAffected = await connection.ExecuteAsync(
                    debitSql,
                    new { Amount = amount, AccountNumber = accountNumber },
                    transaction);

                if (rowsAffected != 1)
                {
                    var account = await connection.QuerySingleOrDefaultAsync<Account>(
                        @"SELECT AccountNumber, Balance, IsFrozen
          FROM Accounts WHERE AccountNumber = @AccountNumber;",
                        new { AccountNumber = accountNumber },
                        transaction);

                    transaction.Rollback();

                    if (account == null)
                        throw new InvalidOperationException("Account not found.");

                    if (account.IsFrozen)
                        throw new InvalidOperationException("Your account is frozen and cannot buy airtime.");

                    throw new InvalidOperationException("Insufficient balance.");
                }

                var reference = $"AIR-{Guid.NewGuid():N}";

                
                const string insertSql = @"
    INSERT INTO Transactions
    (
        Reference, SenderAccountNumber, RecipientAccountNumber, Amount,
        TransactionType, TransactionCategory, Description, Status,
        AccountNumber, PhoneNumber, Network
    )
    VALUES
    (
        @Reference, @AccountNumber, NULL, @Amount,
        'Debit', 'Airtime', @Description, 'Successful',
        @AccountNumber, @PhoneNumber, @Network
    );";

                await connection.ExecuteAsync(
                    insertSql,
                    new
                    {
                        Reference = reference,
                        AccountNumber = accountNumber,
                        Amount = amount,
                        Description = $"{network} airtime for {phoneNumber}",
                        PhoneNumber = phoneNumber,
                        Network = network
                    },
                    transaction);

                transaction.Commit();

                return new AirtimePurchaseResponseDto
                {
                    Reference = reference,
                    AccountNumber = accountNumber,
                    PhoneNumber = phoneNumber,
                    Network = network,
                    Amount = amount,
                    Status = "Successful",
                    Message = "Airtime purchase recorded successfully."
                };
            }
            catch
            {
                try
                {
                    transaction.Rollback();
                }
                catch
                {
                    
                }

                throw;
            }
        }
    }
}
