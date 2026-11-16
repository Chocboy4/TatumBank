using Dapper;
using System.Data;

namespace TatumBank.Infrastructure.Repositories
{
    public class InterbankTransferRepository : IInterbankTransferRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public InterbankTransferRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        
        public async Task<bool> DebitAndRecordPendingAsync(
            string senderAccountNumber, string recipientAccountNumber,
            string recipientBankCode, decimal amount, string reference)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != ConnectionState.Open) connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var rows = await connection.ExecuteAsync(
                    @"UPDATE Accounts
                      SET Balance = Balance - @Amount
                      WHERE AccountNumber = @Sender
                        AND Balance >= @Amount
                        AND IsFrozen = 0;",
                    new { Amount = amount, Sender = senderAccountNumber },
                    transaction);

                if (rows != 1)
                {
                    transaction.Rollback();
                    return false;
                }

                await connection.ExecuteAsync(
                    @"INSERT INTO Transactions
                      (Reference, SenderAccountNumber, RecipientAccountNumber, RecipientBankCode,
                       Amount, TransactionType, TransactionCategory, Description, Status, AccountNumber)
                      VALUES
                      (@Reference, @Sender, @Recipient, @BankCode,
                       @Amount, 'Debit', 'Transfer', @Description, 'Pending', @Sender);",
                    new
                    {
                        Reference = reference,
                        Sender = senderAccountNumber,
                        Recipient = recipientAccountNumber,
                        BankCode = recipientBankCode,
                        Amount = amount,
                        Description = $"Transfer to {recipientAccountNumber} at {recipientBankCode}"
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

        public async Task MarkSuccessfulAsync(string reference)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                "UPDATE Transactions SET Status = 'Successful' WHERE Reference = @Reference AND Status = 'Pending';",
                new { Reference = reference });
        }

        // Returns the money and marks the row Failed. Only works while the row is still Pending,
        // so calling it twice can never refund twice.
        public async Task<bool> RefundAndMarkFailedAsync(string reference)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (connection.State != ConnectionState.Open) connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var pending = await connection.QuerySingleOrDefaultAsync<PendingRow>(
                    @"SELECT SenderAccountNumber, Amount
                      FROM Transactions
                      WHERE Reference = @Reference AND Status = 'Pending'
                      FOR UPDATE;",
                    new { Reference = reference },
                    transaction);

                if (pending == null)
                {
                    transaction.Rollback();
                    return false;
                }

                await connection.ExecuteAsync(
                    "UPDATE Accounts SET Balance = Balance + @Amount WHERE AccountNumber = @Sender;",
                    new { Amount = pending.Amount, Sender = pending.SenderAccountNumber },
                    transaction);

                await connection.ExecuteAsync(
                    "UPDATE Transactions SET Status = 'Failed' WHERE Reference = @Reference AND Status = 'Pending';",
                    new { Reference = reference },
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

        private class PendingRow
        {
            public string SenderAccountNumber { get; set; } = string.Empty;
            public decimal Amount { get; set; }
        }
    }
}