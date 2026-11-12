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
    public class BankingController : ControllerBase
    {
        private readonly IBankingService _bankingService;
        private readonly ITransactionService _transactionService;

        public BankingController(
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