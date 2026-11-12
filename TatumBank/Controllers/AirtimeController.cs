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
    public class AirtimeController : ControllerBase
    {
        private readonly IAirtimeService _airtimeService;

        public AirtimeController(IAirtimeService airtimeService)
        {
            _airtimeService = airtimeService;
        }

        [HttpPost("Purchase")]

        public async Task<IActionResult> PurchaseAirtime([FromBody] AirtimePurchaseRequestDto request)
        {
            var accountNumber = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(accountNumber))
            {
                return Unauthorized("You are not Logged In");
            }

            try
            {
                var result = await _airtimeService.PurchaseAirtimeAsync(accountNumber, request);
                return Ok(result);
            }

            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
