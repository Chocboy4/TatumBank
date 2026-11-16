using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TatumBank.DTOs;
using TatumBank.Services;

namespace TatumBank.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IAuditService _auditService;

        public AdminController(IAdminService adminService, IAuditService auditService)
        {
            _adminService = adminService;
            _auditService = auditService;
        }

        private int? CurrentAdminId =>
            int.TryParse(User.FindFirst("AdminId")?.Value, out var id) ? id : null;

        private string CurrentAdminName =>
            User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        [HttpGet("accounts")]
        public Task<IActionResult> GetAccounts([FromQuery] AdminAccountFilterDto filter)
            => RunQuery(() => _adminService.SearchAccountsAsync(filter));

        [HttpGet("accounts/{accountNumber}")]
        public async Task<IActionResult> GetAccount(string accountNumber)
        {
            var account = await _adminService.GetAccountAsync(accountNumber);
            return account == null ? NotFound() : Ok(account);
        }

        [HttpPut("accounts/{accountNumber}/freeze")]
        public Task<IActionResult> Freeze(string accountNumber)
            => RunAction(() => _adminService.SetFrozenAsync(CurrentAdminId, CurrentAdminName, accountNumber, true),
                         "Account frozen.");

        [HttpPut("accounts/{accountNumber}/unfreeze")]
        public Task<IActionResult> Unfreeze(string accountNumber)
            => RunAction(() => _adminService.SetFrozenAsync(CurrentAdminId, CurrentAdminName, accountNumber, false),
                         "Account unfrozen.");

        [HttpGet("transactions")]
        public Task<IActionResult> GetTransactions([FromQuery] AdminTransactionFilterDto filter)
            => RunQuery(() => _adminService.SearchTransactionsAsync(filter));

        [HttpGet("audit-logs")]
        public Task<IActionResult> GetAuditLogs([FromQuery] AuditLogFilterDto filter)
            => RunQuery(() => _auditService.SearchAsync(filter));

        private async Task<IActionResult> RunQuery<T>(Func<Task<T>> query)
        {
            try { return Ok(await query()); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        private async Task<IActionResult> RunAction(Func<Task> action, string successMessage)
        {
            try
            {
                await action();
                return Ok(new { message = successMessage });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}