using TatumBank.DTOs;

namespace TatumBank.Services
{
    public interface IBankingService
    {
        Task<Decimal> GetBalanceAsync(string accountNumber);
        Task<bool> DepositAsync(string accountNumber, decimal amount);
        Task<bool> WithdrawAsync(string accountNumber, decimal amount);
        Task<TransferResponseDto> TransferAsync(string senderAccountNumber, TransferRequestDto transfer);
    }
}
