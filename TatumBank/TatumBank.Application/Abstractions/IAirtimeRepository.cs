using TatumBank.DTOs;

namespace TatumBank.Application.Abstractions
{
    public interface IAirtimeRepository
    {
        Task<bool> DebitAndRecordPendingAsync(
            string accountNumber,
            string phoneNumber,
            string network,
            decimal amount,
            string reference,
            string description);
    }
}
