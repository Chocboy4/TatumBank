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

        public async Task<IEnumerable<TransactionResponseDto>> FilterTransactionsAsync(string accountNumber, TransactionFilterDto filter)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                throw new ArgumentException(
                    "Account number is required.");
            }

            if(filter.StartDate.HasValue && filter.EndDate.HasValue && filter.StartDate > filter.EndDate)
            {
                throw new ArgumentException("Start date cannot be later than end date.");
            }

            if(!string.IsNullOrEmpty(filter.TransactionType))
            {
                var type = filter.TransactionType.ToLower().Trim();

                if (type != "credit" && type != "debit")
                {
                    throw new ArgumentException("Transaction type must be either 'Credit' or 'Debit'.");
                }
                filter.TransactionType = char.ToUpperInvariant(type[0]) + type.Substring(1); // Capitalize the first letter
            }

            return await _transactionRepository.FilterAsync(accountNumber, filter);
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