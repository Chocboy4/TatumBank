using TatumBank.DTOs;

namespace TatumBank.HttpClients
{
    public interface IBankBClient
    {
        Task<HttpResponseMessage> SendTransferAsync(InterbankTransferRequestDto request, CancellationToken cancellationToken = default);
    }
}
