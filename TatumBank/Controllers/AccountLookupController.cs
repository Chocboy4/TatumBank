using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TatumBank.Application.Abstractions;
using TatumBank.Infrastructure.Repositories;

namespace TatumBank.Controllers;

[Route("api/accounts/lookup")]
[ApiController]
[AllowAnonymous]
[ServiceFilter(typeof(Filters.InterbankApiKeyFilter))]
public sealed class AccountLookupController : ControllerBase
{
    private readonly IAccountRepository _accountRepository;

    public AccountLookupController(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    [HttpGet("{accountNumber}")]
    public async Task<IActionResult> LookUp(string accountNumber)
    {
        var account = await _accountRepository.GetAccountByNumberAsync(
            accountNumber.Trim());

        if (account is null)
        {
            return NotFound(new
            {
                status = "Failed",
                message = "Account not found."
            });
        }

        return Ok(new
        {
            accountHolderName = account.AccountHolderName,
            accountNumber = account.AccountNumber
        });
    }
}
