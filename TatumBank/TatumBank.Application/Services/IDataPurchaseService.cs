using TatumBank.DTOs;

namespace TatumBank.Services
{
    public interface IDataPurchaseService
    {
        Task<DataPurchaseResponseDto> PurchaseDataAsync(string accountNumber, DataPurchaseRequestDto request);
        IEnumerable<NetworkPlansDto> GetAvailableDataPlans();
    }
}
