using TatumBank.DTOs;
using TatumBank.Infrastructure.Repositories;

namespace TatumBank.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;

        public TransactionService(
            ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<List<TransactionHistoryDto>>
            GetTransactionHistoryAsync(string accountNumber)
        {
            var transactions = await _transactionRepository
                .GetByAccountNumberAsync(accountNumber);

            return transactions.Select(transaction =>
                new TransactionHistoryDto
                {
                    Id = transaction.Id,
                    Reference = transaction.Reference,
                    SenderAccountNumber =
                        transaction.SenderAccountNumber,
                    RecipientAccountNumber =
                        transaction.RecipientAccountNumber,
                    Amount = transaction.Amount,

                    TransactionType =
                        string.Equals(
                            transaction.SenderAccountNumber,
                            accountNumber,
                            StringComparison.OrdinalIgnoreCase)
                            ? "Debit"
                            : "Credit",

                    Status = transaction.Status,
                    CreatedAt = transaction.CreatedAt
                })
                .ToList();
        }
    }
}