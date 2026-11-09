using NewBanking.BLM;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace NewBanking.Infrastructure
{
    public enum BillSendOutcome
    {
        Delivered,      
        Rejected,       
        NotDelivered,  
        Unknown        
    }

    public record BillSendResult(BillSendOutcome Outcome, string Message);

    public static class BillClient
    {
        private static readonly HttpClient Http = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

       
        public static async Task<List<DataPlan>?> GetPlansAsync()
        {
            try
            {
                using var message = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{InterBankSettings.PeerUrl}/api/bills/data-plans");

                message.Headers.Add("X-Api-Key", InterBankSettings.ApiKey);

                using HttpResponseMessage response = await Http.SendAsync(message);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<List<DataPlan>>(InterBankSettings.Json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<BillSendResult> PurchaseAsync(BillPurchaseRequest request)
        {
            try
            {
                using var message = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{InterBankSettings.PeerUrl}/api/bills/purchase");

                message.Headers.Add("X-Api-Key", InterBankSettings.ApiKey);
                message.Content = JsonContent.Create(request, options: InterBankSettings.Json);

                using HttpResponseMessage response = await Http.SendAsync(message);

                string body = await response.Content.ReadAsStringAsync();
                TransferResponse? parsed = TryParse(body);
                int code = (int)response.StatusCode;

                if (code < 500)
                {
                    if (parsed is not null)
                    {
                        bool ok = response.IsSuccessStatusCode && parsed.Success;
                        return new BillSendResult(
                            ok ? BillSendOutcome.Delivered : BillSendOutcome.Rejected,
                            parsed.Message);
                    }

                    if (code >= 400)
                    {
                        return new BillSendResult(
                            BillSendOutcome.Rejected,
                            $"ChocboyBank rejected the request (HTTP {code}).");
                    }
                }

                return new BillSendResult(
                    BillSendOutcome.Unknown,
                    $"Unexpected response from ChocboyBank (HTTP {code}).");
            }
            catch (HttpRequestException ex) when (
                ex.InnerException is SocketException { SocketErrorCode: SocketError.ConnectionRefused })
            {
                return new BillSendResult(BillSendOutcome.NotDelivered, "Could not reach Biller. Is it running?");
            }
            catch (TaskCanceledException)
            {
                return new BillSendResult(BillSendOutcome.Unknown, "Biller did not answer in time.");
            }
            catch (HttpRequestException ex)
            {
                return new BillSendResult(BillSendOutcome.Unknown, $"Connection problem: {ex.Message}");
            }
        }

        private static TransferResponse? TryParse(string body)
        {
            try
            {
                return JsonSerializer.Deserialize<TransferResponse>(body, InterBankSettings.Json);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}