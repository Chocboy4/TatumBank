using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TatumBank.DTOs;
using TatumBank.Services;

namespace TatumBank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpGet("History")]
        public async Task<IActionResult> GetTransactionHistory()
        {
            var accountNumber = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (accountNumber == null)
            {
                return Unauthorized("Account information was not found.");
            }

            var transactions = await _transactionService.GetTransactionHistoryAsync(accountNumber);

            return Ok(transactions);
        }

        [HttpGet("Filter")]
        public async Task<ActionResult<IEnumerable<TransactionResponseDto>>> FilterAsync([FromQuery] TransactionFilterDto filter)
        {
            var accountNumber = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if(string.IsNullOrEmpty(accountNumber))
            {
                return Unauthorized("Account information was not found.");
            }

            try
            {
                var transactions = await _transactionService.FilterTransactionsAsync(accountNumber, filter);
                return Ok(transactions);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
