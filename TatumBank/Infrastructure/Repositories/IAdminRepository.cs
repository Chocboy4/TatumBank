using TatumBank.DTOs;

namespace TatumBank.Infrastructure.Repositories
{
    public interface IAdminRepository
    {
        Task<PagedResult<AdminAccountDto>> SearchAccountsAsync(AdminAccountFilterDto filter);
        Task<AdminAccountDto?> GetAccountAsync(string accountNumber);
        Task<bool> SetFrozenAsync(string accountNumber, bool frozen);
        Task<PagedResult<AdminTransactionDto>> SearchTransactionsAsync(AdminTransactionFilterDto filter);
    }
}