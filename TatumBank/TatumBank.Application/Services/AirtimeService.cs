using TatumBank.DTOs;
using Microsoft.Extensions.Logging;
using TatumBank.Helpers;
using TatumBank.HttpClients;
using TatumBank.Domain.Entities;
using TatumBank.Application.Abstractions;

namespace TatumBank.Services
{
    public class AirtimeService : IAirtimeService
    {
        private const string OwnBankCode = "TatumBank";
        private const string ProviderName = "BankOfChocboy";

        private readonly IAirtimeRepository _airtimeRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IInterbankTransferRepository _settlementRepository;
        private readonly IChocboyBankClient _providerClient;
        private readonly ILogger<AirtimeService> _logger;

        public AirtimeService(
            IAirtimeRepository airtimeRepository,
            IAccountRepository accountRepository,
            IInterbankTransferRepository settlementRepository,
            IChocboyBankClient providerClient,
            ILogger<AirtimeService> logger)
        {
            _airtimeRepository = airtimeRepository;
            _accountRepository = accountRepository;
            _settlementRepository = settlementRepository;
            _providerClient = providerClient;
            _logger = logger;
        }

        public async Task<AirtimePurchaseResponseDto> PurchaseAirtimeAsync(
            string accountNumber,
            AirtimePurchaseRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                throw new ArgumentException("Authenticated account is required.");

            if (request.Amount < 50 || request.Amount > 100000)
                throw new ArgumentException("Amount must be between 50 and 100000.");

            var network = NetworkCatalog.Normalize(request.Network);

            if (!NetworkCatalog.IsSupported(network))
                throw new ArgumentException($"Network must be {NetworkCatalog.SupportedList}.");

            if (!PhoneNumberHelper.TryNormalize(request.PhoneNumber, out var phoneNumber))
                throw new ArgumentException("Invalid phone number. Use a Nigerian number like 08031234567.");

            var account = await _accountRepository.GetAccountByNumberAsync(accountNumber)
                ?? throw new InvalidOperationException("Account not found.");

            if (account.IsFrozen)
                throw new InvalidOperationException("Your account is frozen and cannot buy airtime.");

            if (account.Balance < request.Amount)
                throw new InvalidOperationException("Insufficient balance.");

            var reference = $"AIR-{Guid.NewGuid():N}";

            var response = new AirtimePurchaseResponseDto
            {
                Reference = reference,
                AccountNumber = accountNumber,
                PhoneNumber = phoneNumber,
                Network = network,
                Amount = request.Amount
            };

            // Step 1: take the money and record the purchase as Pending, in one database transaction.
            var held = await _airtimeRepository.DebitAndRecordPendingAsync(
                accountNumber,
                phoneNumber,
                network,
                request.Amount,
                reference,
                $"{network} airtime for {phoneNumber} via {ProviderName}");

            if (!held)
            {
                response.Status = "Failed";
                response.Message = "Insufficient balance, or your account is frozen.";
                return response;
            }

            // Step 2: ask the provider (BankOfChocboy) to deliver the airtime.
            var result = await _providerClient.PurchaseAirtimeAsync(new AirtimeVendRequestDto
            {
                Reference = reference,
                RequesterBankCode = OwnBankCode,
                PhoneNumber = phoneNumber,
                Network = network,
                Amount = request.Amount
            });

            // Step 3: settle according to what the provider answered.
            switch (result.Outcome)
            {
                case InterbankOutcome.Accepted:
                    try
                    {
                        await _settlementRepository.MarkSuccessfulAsync(reference);
                    }
                    catch (Exception ex)
                    {
                        // The provider already delivered. The row stays Pending until it is reconciled.
                        _logger.LogError(ex,
                            "Airtime {Reference} was delivered but marking it Successful failed.", reference);
                    }

                    response.Status = "Successful";
                    response.Message = "Airtime purchase successful.";
                    return response;

                case InterbankOutcome.Rejected:
                case InterbankOutcome.NotSent:
                    var refunded = await _settlementRepository.RefundAndMarkFailedAsync(reference);
                    if (!refunded)
                    {
                        _logger.LogError("Could not refund airtime {Reference}.", reference);
                    }

                    response.Status = "Failed";
                    response.Message = $"{result.Message} Your money has been returned.";
                    return response;

                default:
                    // Unknown outcome: do NOT refund and do NOT retry. The row stays Pending.
                    _logger.LogWarning(
                        "Airtime {Reference} is Pending: the outcome at the provider is unknown.", reference);

                    response.Status = "Pending";
                    response.Message =
                        "Your airtime purchase is being processed. We will confirm the final status shortly. Please do not repeat it.";
                    return response;
            }
        }
    }
}