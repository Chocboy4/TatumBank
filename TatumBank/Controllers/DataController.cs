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
    public class DataController : ControllerBase
    {
        private readonly IDataPurchaseService _dataPurchaseService;

        public DataController(IDataPurchaseService dataPurchaseService)
        {
            _dataPurchaseService = dataPurchaseService;
        }


        [HttpGet("Plans")]
        public ActionResult<IEnumerable<NetworkPlansDto>> GetAvailableDataPlans()
        {
            var plans = _dataPurchaseService.GetAvailableDataPlans();
            return Ok(plans);
        }

        [HttpPost("Purchase")]
        public async Task<ActionResult<DataPurchaseResponseDto>> PurchaseAsync([FromBody] DataPurchaseRequestDto request)
        {
            var accountNumber = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if(string.IsNullOrWhiteSpace(accountNumber))
            {
                return Unauthorized("Account number is required.");
            }

            if (request == null)
            {
                return BadRequest("Purchase request is required.");
            }

            var response = await _dataPurchaseService.PurchaseDataAsync(accountNumber, request);

            if(response.Status != "Success")
            {
                return BadRequest(response);
            }


            return Ok(response);
        }
    }
}
