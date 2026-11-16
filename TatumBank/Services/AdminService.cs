using TatumBank.DTOs;
using TatumBank.Infrastructure.Repositories;

namespace TatumBank.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _repository;
        private readonly IAuditService _audit;

        public AdminService(IAdminRepository repository, IAuditService audit)
        {
            _repository = repository;
            _audit = audit;
        }

        public Task<PagedResult<AdminAccountDto>> SearchAccountsAsync(AdminAccountFilterDto filter)
        {
            filter.Page = Math.Max(filter.Page, 1);
            filter.PageSize = Math.Clamp(filter.PageSize, 1, 100);

            if (filter.MinBalance.HasValue && filter.MaxBalance.HasValue && filter.MinBalance > filter.MaxBalance)
                throw new ArgumentException("Minimum balance cannot be greater than maximum balance.");

            return _repository.SearchAccountsAsync(filter);
        }

        public Task<AdminAccountDto?> GetAccountAsync(string accountNumber)
            => _repository.GetAccountAsync(accountNumber);

        public async Task SetFrozenAsync(int? adminId, string adminName, string accountNumber, bool frozen)
        {
            var account = await _repository.GetAccountAsync(accountNumber)
                ?? throw new KeyNotFoundException("Account not found.");

            if (account.IsFrozen == frozen)
                throw new InvalidOperationException(frozen ? "Account is already frozen." : "Account is not frozen.");

            if (!await _repository.SetFrozenAsync(accountNumber, frozen))
                throw new KeyNotFoundException("Account not found.");

            await _audit.LogAsync(
                adminId, adminName,
                frozen ? "AccountFrozen" : "AccountUnfrozen",
                "Account", accountNumber,
                $"Holder: {account.AccountHolderName}");
        }

        public Task<PagedResult<AdminTransactionDto>> SearchTransactionsAsync(AdminTransactionFilterDto filter)
        {
            filter.Page = Math.Max(filter.Page, 1);
            filter.PageSize = Math.Clamp(filter.PageSize, 1, 100);

            if (filter.StartDate.HasValue && filter.EndDate.HasValue && filter.StartDate > filter.EndDate)
                throw new ArgumentException("Start date cannot be later than end date.");

            if (filter.MinAmount.HasValue && filter.MaxAmount.HasValue && filter.MinAmount > filter.MaxAmount)
                throw new ArgumentException("Minimum amount cannot be greater than maximum amount.");

            return _repository.SearchTransactionsAsync(filter);
        }
    }
}