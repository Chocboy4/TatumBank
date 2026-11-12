using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
    }
}
