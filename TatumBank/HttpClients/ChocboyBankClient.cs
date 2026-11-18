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
                    "api/incoming-transfers",
                    request,
                    cancellationToken);
                var body = await ReadBodyAsync(response, cancellationToken);
                var status = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    return new InterbankResult(InterbankOutcome.Accepted, body?.Message ?? "Transfer Accepted by BankOfChocboy.");
                }
                if (status >= 500 || status == 408 || status == 429)
                {
                    _logger.LogWarning("BankOfChocboy returned {Status} for {Reference}.", status, request.Reference);
                    return new InterbankResult(InterbankOutcome.Unknown,
                        $"BankOfChocboy returned status {status}.");
                }

                return new InterbankResult(InterbankOutcome.Rejected,
                    body?.Message ?? $"BankOfChocboy rejected the transfer (status {status}).");

            }
            catch (HttpRequestException ex) when (ex.HttpRequestError == HttpRequestError.ConnectionError)
            {
                _logger.LogWarning(ex, "Could not connect to BankOfChocboy for {Reference}.", request.Reference);
                return new InterbankResult(InterbankOutcome.NotSent, "BankOfChocboy is currently unavailable.");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "No clear answer from BankOfChocboy for {Reference}.", request.Reference);
                return new InterbankResult(InterbankOutcome.Unknown, "No confirmation received from BankOfChocboy.");
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
                    _logger.LogWarning("BankOfChocboy returned {Status} for account lookup {AccountNumber}.", response.StatusCode, accountNumber);
                    return new AccountLookupResult(AccountLookupOutcome.Unavailable, null, $"BankOfChocboy returned status {response.StatusCode}.");
                }

                var body = await response.Content.ReadFromJsonAsync<InterbankAccountLookupResponseDto>(cancellationToken: cancellationToken);

                return new AccountLookupResult(AccountLookupOutcome.Found, body?.AccountHolderName, "Account found.");
            }
            catch(Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                _logger.LogWarning(ex, "No clear answer from BankOfChocboy for account lookup {AccountNumber}.", accountNumber);
                return new AccountLookupResult(AccountLookupOutcome.Unavailable, null, "No confirmation received from BankOfChocboy.");
            }
            { }
        }

        public async Task<InterbankResult> PurchaseAirtimeAsync(
    AirtimeVendRequestDto request,
    CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _httpClient.PostAsJsonAsync(
                    "/api/airtime/vend", request, cancellationToken);

                var status = (int)response.StatusCode;
                var body = await ReadBodyAsync(response, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return new InterbankResult(InterbankOutcome.Accepted,
                        body?.Message ?? "Airtime delivered by BankOfChocboy.");
                }

                _logger.LogWarning(
                    "BankOfChocboy answered {Status} for airtime {Reference} at {Url}. Message: {Message}",
                    status, request.Reference, response.RequestMessage?.RequestUri, body?.Message);

                if (status >= 500 || status == 408 || status == 429)
                {
                    return new InterbankResult(InterbankOutcome.Unknown,
                        $"BankOfChocboy returned status {status}.");
                }

                return new InterbankResult(InterbankOutcome.Rejected,
                    body?.Message ?? $"BankOfChocboy rejected the airtime request (status {status}).");
            }
            catch (HttpRequestException ex) when (ex.HttpRequestError == HttpRequestError.ConnectionError)
            {
                _logger.LogWarning(ex, "Could not connect to BankOfChocboy for airtime {Reference}.", request.Reference);
                return new InterbankResult(InterbankOutcome.NotSent, "Airtime service is currently unavailable.");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "No clear answer from BankOfChocboy for airtime {Reference}.", request.Reference);
                return new InterbankResult(InterbankOutcome.Unknown, "No confirmation received from the airtime provider.");
            }
        }
    }
}
