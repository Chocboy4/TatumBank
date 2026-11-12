using TatumBank.DTOs;

namespace TatumBank.HttpClients
{
    public class BankBClient : IBankBClient
    {
        private readonly HttpClient _httpClient;

        public BankBClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> SendTransferAsync(InterbankTransferRequestDto request, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/incoming-transfers", request, cancellationToken);

            return response;
        }
    }
}
