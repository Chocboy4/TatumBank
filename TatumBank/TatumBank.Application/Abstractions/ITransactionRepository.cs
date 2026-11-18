using TatumBank.DTOs;
using TatumBank.Domain.Entities;

namespace TatumBank.Application.Abstractions
{
    public interface ITransactionRepository
    {
        Task<bool> AddAsync(Transfer transfer);

        Task<List<Transaction>> GetByAccountNumberAsync(
            string accountNumber);

        Task<IEnumerable<TransactionResponseDto>> FilterAsync(string accountNumber, TransactionFilterDto filter);
    }
}