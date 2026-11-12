using TatumBank.DTOs;

namespace TatumBank.Infrastructure.Repositories
{
    public interface IAirtimeRepository
    {
        Task<AirtimePurchaseResponseDto> PurchaseAsync(string accountNumber,
            string phoneNumber,
            string network,
            decimal amount);
    }
}
