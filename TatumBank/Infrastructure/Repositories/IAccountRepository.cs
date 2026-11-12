using TatumBank.Models;

namespace TatumBank.Infrastructure.Repositories
{
    public interface IAccountRepository
    {
        Task<bool> AddAsync(Account account);
        Task<Account?> GetAccountByNumberAsync(string accountNumber);
        Task<List<Account>> GetAllAsync();
        Task<bool> UpdateBalance(string accountNumber, decimal balance);

        Task<bool> TransferAsync(string senderAccountNumber, string recipientAccountNumber, decimal amount, string reference);

        Task<List<Transaction>> GetTransactionHistoryAsync(string accountNumber);
    }
}
