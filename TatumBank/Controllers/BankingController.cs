using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TatumBank.DTOs;
using TatumBank.Services;

namespace TatumBank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BankingServiceController : ControllerBase
    {
        private readonly IBankingService _bankingService;
        private readonly ITransactionService _transactionService;

        public BankingServiceController(
            IBankingService bankingService,
            ITransactionService transactionService)
        {
            _bankingService = bankingService;
            _transactionService = transactionService;
        }

        [HttpPost("Transfer")]
        public async Task<ActionResult<TransferResponseDto>>
            TransferAsync([FromBody] TransferRequestDto request)
        {
            if (request == null)
            {
                return BadRequest(
                    "Transfer request cannot be null.");
            }

            var senderAccountNumber =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(senderAccountNumber))
            {
                return Unauthorized(
                    "Unable to identify the authenticated account.");
            }

            if (request.Amount < 100)
            {
                return BadRequest(
                    "Transfer amount must be at least 100.");
            }

            if (string.IsNullOrWhiteSpace(
                request.RecipientAccountNumber))
            {
                return BadRequest(
                    "Recipient account number is required.");
            }

            if (string.Equals(
                senderAccountNumber,
                request.RecipientAccountNumber,
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "You cannot transfer to your own account."
                });
            }

            var response = await _bankingService.TransferAsync(
                senderAccountNumber,
                request);

            if (response.Status != "Successful")
            {
                return BadRequest(response.Message);
            }

            return Ok(response);
        }

        [HttpPost("Deposit")]
        public async Task<IActionResult> Deposit([FromBody] AmountRequestDto request)
        {
            var accountNumber = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if(string.IsNullOrEmpty(accountNumber))
            {
                return Unauthorized("You are not Logged In");
            }

            try
            {
                var success = await _bankingService.DepositAsync(accountNumber, request.Amount);

                if(!success)
                {
                    return BadRequest("Deposit failed.");
                }

                var balance = await _bankingService.GetBalanceAsync(accountNumber);

                return Ok(new { Message = "Deposit successful.", balance });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("Withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] AmountRequestDto request)
        {
            var accountNumber = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(accountNumber))
            {
                return Unauthorized("You are not Logged In");
            }

            try
            {
                var success = await _bankingService.WithdrawAsync(accountNumber, request.Amount);

                if (!success)
                {
                    return BadRequest("Withdrawal could not be completed.");
                }
                var balance = await _bankingService.GetBalanceAsync(accountNumber);
                return Ok(new { Message = "Withdrawal successful.", balance });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("Transactions")]
        public async Task<ActionResult<List<TransactionHistoryDto>>>
            GetTransactionHistoryAsync()
        {
            var accountNumber =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                return Unauthorized(
                    "Unable to identify the authenticated account.");
            }

            var transactions = await _transactionService
                .GetTransactionHistoryAsync(accountNumber);

            return Ok(transactions);
        }
    }
}