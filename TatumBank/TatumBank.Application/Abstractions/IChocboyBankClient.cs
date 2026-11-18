using TatumBank.DTOs;

namespace TatumBank.HttpClients
{
    public enum InterbankOutcome
    {
        Accepted,
        Rejected,
        NotSent,
        Unknown
    }

    public record InterbankResult(InterbankOutcome Outcome, string Message);

    public enum AccountLookupOutcome
    {
        Found,
        NotFound,
        Unavailable
    }

    public record AccountLookupResult(AccountLookupOutcome Outcome, string? AccountHolderName, string? Message);
    public interface IChocboyBankClient
    {
        Task<InterbankResult> SendTransferAsync(InterbankTransferRequestDto request, CancellationToken cancellationToken = default);

        Task<AccountLookupResult> LookupAccountAsync(string accountNumber, CancellationToken cancellationToken = default);
        Task<InterbankResult> PurchaseAirtimeAsync(AirtimeVendRequestDto request, CancellationToken cancellationToken = default);
    }
}
