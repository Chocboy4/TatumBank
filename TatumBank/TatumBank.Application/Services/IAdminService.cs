using TatumBank.DTOs;

namespace TatumBank.Services
{
    public interface IAdminService
    {
        Task<PagedResult<AdminAccountDto>> SearchAccountsAsync(AdminAccountFilterDto filter);
        Task<AdminAccountDto?> GetAccountAsync(string accountNumber);
        Task SetFrozenAsync(int? adminId, string adminName, string accountNumber, bool frozen);
        Task<PagedResult<AdminTransactionDto>> SearchTransactionsAsync(AdminTransactionFilterDto filter);
    }
}