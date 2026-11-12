using TatumBank.DTOs;
using TatumBank.Infrastructure.Repositories;

namespace TatumBank.Services
{
    public class BankingService : IBankingService
    {
        private readonly IAccountRepository _accountRepository;

        public BankingService(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
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

            decimal balance = account.Balance + amount;

            return await _accountRepository.UpdateBalance(
                accountNumber,
                balance);
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
                response.Message =
                    "Transfer amount must be greater than zero.";
                return response;
            }

            if (SenderAccountNumber ==
                request.RecipientAccountNumber)
            {
                response.Status = "Failed";
                response.Message =
                    "You cannot transfer to the same account.";
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

            // Generate one reference for both transaction records.
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

            if (account.Balance < amount)
            {
                throw new InvalidOperationException(
                    "Insufficient funds.");
            }

            decimal balance = account.Balance - amount;

            return await _accountRepository.UpdateBalance(
                accountNumber,
                balance);
        }
    }
}