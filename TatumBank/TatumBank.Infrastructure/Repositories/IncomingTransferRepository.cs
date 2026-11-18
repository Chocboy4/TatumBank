using System.Data;
using Dapper;
using TatumBank.Application.Abstractions;
using TatumBank.DTOs;

namespace TatumBank.Infrastructure.Repositories;

public sealed class IncomingTransferRepository : IIncomingTransferRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public IncomingTransferRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IIncomingTransferRepository.IncomingTransferResult> CreditAsync(
        InterbankTransferRequestDto request,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        if (connection.State != ConnectionState.Open)
            connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            var accountExists = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Accounts WHERE AccountNumber = @AccountNumber;",
                new { AccountNumber = request.RecipientAccountNumber }, transaction);

            if (accountExists == 0)
            {
                transaction.Rollback();
                return IIncomingTransferRepository.IncomingTransferResult.AccountNotFound;
            }

            var inserted = await connection.ExecuteAsync(
                "INSERT INTO Transactions (Reference, RecipientAccountNumber, Amount, TransactionType, TransactionCategory, Description, Status, AccountNumber) " +
                "VALUES (@Reference, @Recipient, @Amount, 'Credit', 'Transfer', @Description, 'Successful', @Recipient);",
                new
                {
                    request.Reference,
                    Recipient = request.RecipientAccountNumber,
                    request.Amount,
                    Description = $"Incoming transfer from {request.SenderAccountNumber} at {request.SenderBankCode}"
                }, transaction);

            if (inserted != 1)
            {
                transaction.Rollback();
                return IIncomingTransferRepository.IncomingTransferResult.Duplicate;
            }

            await connection.ExecuteAsync(
                "UPDATE Accounts SET Balance = Balance + @Amount WHERE AccountNumber = @Recipient;",
                new { request.Amount, Recipient = request.RecipientAccountNumber }, transaction);

            transaction.Commit();
            return IIncomingTransferRepository.IncomingTransferResult.Credited;
        }
        catch (Exception ex) when (ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                                  ex.Message.Contains("unique", StringComparison.OrdinalIgnoreCase))
        {
            try { transaction.Rollback(); } catch { }
            return IIncomingTransferRepository.IncomingTransferResult.Duplicate;
        }
        catch
        {
            try { transaction.Rollback(); } catch { }
            throw;
        }
    }
}
