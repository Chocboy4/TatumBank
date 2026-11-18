
using TatumBank.DTOs;

namespace TatumBank.Services
{
    public interface IAirtimeService
    {
        Task<AirtimePurchaseResponseDto> PurchaseAirtimeAsync(string accountNumber, AirtimePurchaseRequestDto request);
    }
}
