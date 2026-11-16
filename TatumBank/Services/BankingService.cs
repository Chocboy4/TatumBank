using TatumBank.DTOs;
using TatumBank.HttpClients;
using TatumBank.Infrastructure.Repositories;
using TatumBank.Models;

namespace TatumBank.Services
{
    public class BankingService : IBankingService
    {
        private const string OwnBankCode = "TatumBank";

        private static readonly HashSet<string> SupportedBanks =
            new(StringComparer.OrdinalIgnoreCase) { "ChocboyBank" };

        private readonly IAccountRepository _accountRepository;
        private readonly IInterbankTransferRepository _interbankRepository;
        private readonly IChocboyBankClient _chocboyBankClient;
        private readonly ILogger<BankingService> _logger;

        public BankingService(
            IAccountRepository accountRepository,
            IInterbankTransferRepository interbankRepository,
            IChocboyBankClient chocboyBankClient,
            ILogger<BankingService> logger)
        {
            _accountRepository = accountRepository;
            _interbankRepository = interbankRepository;
            _chocboyBankClient = chocboyBankClient;
            _logger = logger;
        }

        public async Task<bool> DepositAsync(
            string accountNumber,
            decimal amount)
        {
            if (amount <= 100)
            {
                throw new ArgumentException(
                    "Deposit amount must be greater than 100.");
            }

            var account =
                await _accountRepository.GetAccountByNumberAsync(
                    accountNumber);

            if (account == null)
            {
                throw new InvalidOperationException("Account not found.");
            }

            var reference = $"DEP-{Guid.NewGuid():N}";
            return await _accountRepository.DepositAsync(accountNumber, amount, reference);

        }

        public async Task<decimal> GetBalanceAsync(string accountNumber)
        {
            var account =
                await _accountRepository.GetAccountByNumberAsync(
                    accountNumber);

            if (account == null)
            {
                throw new InvalidOperationException("Account not found.");
            }

            return account.Balance;
        }

        public async Task<TransferResponseDto> TransferAsync(
            string SenderAccountNumber,
            TransferRequestDto request)
        {
            var response = new TransferResponseDto
            {
                SenderAccountNumber = SenderAccountNumber,
                RecipientAccountNumber = request.RecipientAccountNumber,
                Amount = request.Amount,
                CreatedAt = DateTime.UtcNow
            };

            if (string.IsNullOrWhiteSpace(SenderAccountNumber) ||
                string.IsNullOrWhiteSpace(request.RecipientAccountNumber))
            {
                response.Status = "Failed";
                response.Message = "Account numbers are required.";
                return response;
            }

            if (request.Amount <= 0)
            {
                response.Status = "Failed";
                response.Message = "Transfer amount must be greater than zero.";
                return response;
            }

            var sender =
                await _accountRepository.GetAccountByNumberAsync(
                    SenderAccountNumber);

            if (sender == null)
            {
                response.Status = "Failed";
                response.Message = "Sender account was not found.";
                return response;
            }

            if (sender.IsFrozen)
            {
                response.Status = "Failed";
                response.Message = "Your account is frozen and cannot send money. Please contact support.";
                return response;
            }

            // An empty bank code, or our own code, means a transfer inside TatumBank.
            var bankCode = request.RecipientBankCode?.Trim();
            var isInterbank = !string.IsNullOrEmpty(bankCode) &&
                              !string.Equals(bankCode, OwnBankCode, StringComparison.OrdinalIgnoreCase);

            if (isInterbank)
            {
                return await TransferInterbankAsync(sender, request, bankCode!, response);
            }

            // ---------- Transfer inside TatumBank ----------

            if (SenderAccountNumber == request.RecipientAccountNumber)
            {
                response.Status = "Failed";
                response.Message = "You cannot transfer to the same account.";
                return response;
            }

            var recipient =
                await _accountRepository.GetAccountByNumberAsync(
                    request.RecipientAccountNumber);

            if (recipient == null)
            {
                response.Status = "Failed";
                response.Message = "Recipient account was not found.";
                return response;
            }

            if (sender.Balance < request.Amount)
            {
                response.Status = "Failed";
                response.Message = "Insufficient funds.";
                return response;
            }


            var reference = $"TRF-{Guid.NewGuid():N}";

            var success = await _accountRepository.TransferAsync(
                SenderAccountNumber,
                request.RecipientAccountNumber,
                request.Amount,
                reference);

            if (!success)
            {
                response.Status = "Failed";
                response.Message = "Transfer could not be completed.";
                return response;
            }

            response.Reference = reference;
            response.Status = "Successful";
            response.Message = "Transfer completed successfully.";

            return response;
        }

        public async Task<bool> WithdrawAsync(
            string accountNumber,
            decimal amount)
        {
            if (amount < 500)
            {
                throw new ArgumentException(
                    "Withdrawal amount must be 500 and above.");
            }

            var account =
                await _accountRepository.GetAccountByNumberAsync(
                    accountNumber);

            if (account == null)
            {
                throw new InvalidOperationException("Account not found.");
            }

            if (account.IsFrozen)
            {
                throw new InvalidOperationException(
                    "Your account is frozen and cannot make withdrawals.");
            }

            if (account.Balance < amount)
            {
                throw new InvalidOperationException(
                    "Insufficient funds.");
            }

            var reference = $"WDR-{Guid.NewGuid():N}";

            return await _accountRepository.WithdrawAsync(accountNumber, amount, reference);
        }

        // ---------- Transfer to another bank ----------

        private async Task<TransferResponseDto> TransferInterbankAsync(
            Account sender,
            TransferRequestDto request,
            string bankCode,
            TransferResponseDto response)
        {
            if (!SupportedBanks.TryGetValue(bankCode, out var canonicalBankCode))
            {
                response.Status = "Failed";
                response.Message = $"Bank '{bankCode}' is not supported.";
                return response;
            }

            if (sender.Balance < request.Amount)
            {
                response.Status = "Failed";
                response.Message = "Insufficient funds.";
                return response;
            }

            var lookUp = await _chocboyBankClient.LookupAccountAsync(request.RecipientAccountNumber);

            if (lookUp.Outcome == AccountLookupOutcome.NotFound)
            {
                response.Status = "Failed";
                response.Message = $"Recipient account was not found at {canonicalBankCode}.";
                return response;
            }

            if (lookUp.Outcome == AccountLookupOutcome.Unavailable)
            {
                response.Status = "Failed";
                response.Message = "We could not verify the recipient account right now. Please try again later.";
                return response;
            }

            var reference = $"TRF-{Guid.NewGuid():N}";
            response.Reference = reference;

            // Step 1: take the money and record the transfer as Pending, in one database transaction.
            var held = await _interbankRepository.DebitAndRecordPendingAsync(
                sender.AccountNumber,
                request.RecipientAccountNumber,
                canonicalBankCode,
                request.Amount,
                reference);

            if (!held)
            {
                response.Status = "Failed";
                response.Message = "Insufficient funds, or your account is frozen.";
                return response;
            }

            
            var result = await _chocboyBankClient.SendTransferAsync(new InterbankTransferRequestDto
            {
                Reference = reference,
                SenderAccountNumber = sender.AccountNumber,
                SenderBankCode = OwnBankCode,
                RecipientAccountNumber = request.RecipientAccountNumber,
                Amount = request.Amount
            });

            // Step 3: settle according to what ChocboyBank answered.
            switch (result.Outcome)
            {
                case InterbankOutcome.Accepted:
                    try
                    {
                        await _interbankRepository.MarkSuccessfulAsync(reference);
                    }
                    catch (Exception ex)
                    {
                        // ChocboyBank already credited. The row stays Pending until it is reconciled.
                        _logger.LogError(ex,
                            "ChocboyBank accepted {Reference} but marking it Successful failed.", reference);
                    }

                    response.Status = "Successful";
                    response.Message = "Transfer completed successfully.";
                    return response;

                case InterbankOutcome.Rejected:
                case InterbankOutcome.NotSent:
                    var refunded = await _interbankRepository.RefundAndMarkFailedAsync(reference);
                    if (!refunded)
                    {
                        _logger.LogError(
                            "Could not refund {Reference} after a rejected transfer.", reference);
                    }

                    response.Status = "Failed";
                    response.Message = $"{result.Message} Your money has been returned.";
                    return response;

                default:
                    // Unknown outcome: do NOT refund and do NOT retry. The row stays Pending.
                    _logger.LogWarning(
                        "Transfer {Reference} is Pending: the outcome at ChocboyBank is unknown.", reference);

                    response.Status = "Pending";
                    response.Message =
                        "Your transfer is being processed. We will confirm the final status shortly. Please do not repeat it.";
                    return response;
            }
        }
    }
}