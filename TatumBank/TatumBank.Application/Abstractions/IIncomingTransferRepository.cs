using TatumBank.DTOs;

namespace TatumBank.Application.Abstractions;

public interface IIncomingTransferRepository
{
    Task<IncomingTransferResult> CreditAsync(
        InterbankTransferRequestDto request,
        CancellationToken cancellationToken = default);

    enum IncomingTransferResult
    {
        Credited,
        Duplicate,
        AccountNotFound
    }
}
