using TatumBank.DTOs;
using System.Net.Http.Json;

namespace TatumBank.HttpClients
{
    public class ChocboyBankClient : IChocboyBankClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChocboyBankClient> _logger;

        public ChocboyBankClient(HttpClient httpClient, ILogger<ChocboyBankClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<InterbankResult> SendTransferAsync(InterbankTransferRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "api/interbank/incoming-transfers",
                    request,
                    cancellationToken);
                var body = await ReadBodyAsync(response, cancellationToken);
                var status = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    return new InterbankResult(InterbankOutcome.Accepted, body?.Message ?? "Transfer Accepted by ChocboyBank.");
                }
                if (status >= 500 || status == 408 || status == 429)
                {
                    _logger.LogWarning("ChocboyBank returned {Status} for {Reference}.", status, request.Reference);
                    return new InterbankResult(InterbankOutcome.Unknown,
                        $"ChocboyBank returned status {status}.");
                }

                return new InterbankResult(InterbankOutcome.Rejected,
                    body?.Message ?? $"ChocboyBank rejected the transfer (status {status}).");

            }
            catch (HttpRequestException ex) when (ex.HttpRequestError == HttpRequestError.ConnectionError)
            {
                _logger.LogWarning(ex, "Could not connect to ChocboyBank for {Reference}.", request.Reference);
                return new InterbankResult(InterbankOutcome.NotSent, "ChocboyBank is currently unavailable.");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "No clear answer from ChocboyBank for {Reference}.", request.Reference);
                return new InterbankResult(InterbankOutcome.Unknown, "No confirmation received from ChocboyBank.");
            }
        }

        private static async Task<InterbankTransferResponseDto?> ReadBodyAsync(
            HttpResponseMessage response, CancellationToken cancellationToken)
        {
            try
            {
                return await response.Content.ReadFromJsonAsync<InterbankTransferResponseDto>(
                    cancellationToken: cancellationToken);
            }
            catch
            {
                return null;
            }
        }

        public async Task<AccountLookupResult> LookupAccountAsync(string accountNumber, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/accounts/lookup/{Uri.EscapeDataString(accountNumber)}", cancellationToken);
                if(response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new AccountLookupResult(AccountLookupOutcome.NotFound, null, "Account not found.");
                }
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("ChocboyBank returned {Status} for account lookup {AccountNumber}.", response.StatusCode, accountNumber);
                    return new AccountLookupResult(AccountLookupOutcome.Unavailable, null, $"ChocboyBank returned status {response.StatusCode}.");
                }

                var body = await response.Content.ReadFromJsonAsync<InterbankAccountLookupResponseDto>(cancellationToken: cancellationToken);

                return new AccountLookupResult(AccountLookupOutcome.Found, body?.AccountHolderName, "Account found.");
            }
            catch(Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                _logger.LogWarning(ex, "No clear answer from ChocboyBank for account lookup {AccountNumber}.", accountNumber);
                return new AccountLookupResult(AccountLookupOutcome.Unavailable, null, "No confirmation received from ChocboyBank.");
            }
            { }
        }
    }
}
