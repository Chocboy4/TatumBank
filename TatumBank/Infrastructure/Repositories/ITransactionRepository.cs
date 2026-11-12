using TatumBank.Models;

namespace TatumBank.Infrastructure.Repositories
{
    public interface ITransactionRepository
    {
        Task<bool> AddAsync(Transfer transfer);

        Task<List<Transaction>> GetByAccountNumberAsync(
            string accountNumber);
    }
}