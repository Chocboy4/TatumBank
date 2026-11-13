using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using TatumBank.DTOs;
using TatumBank.Infrastructure.Repositories;
using TatumBank.Models;
using TatumBank.Services;

namespace TatumBank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountRepository _accountRepository;
        private readonly AccountNumberGenerator _accountNumberGenerator;
        private readonly PasswordService _passwordService;

        public AccountsController(IAccountRepository accountRepository, AccountNumberGenerator accountNumberGenerator,
        PasswordService passwordService)
        {
            _accountRepository = accountRepository;
            _accountNumberGenerator = accountNumberGenerator;
            _passwordService = passwordService;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> CreateAccount(CreateAccountRequest request)
        {
            if(string.IsNullOrWhiteSpace(request.AccountHolderName))
            {
                return BadRequest("Account holder name is required.");
            }

            if(string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            {
                return BadRequest("Password is required and must be at least 8 characters long.");
            }

            if (request.InitialDeposit < 2000)
            {
                return BadRequest(
                    "Initial Deposit must be from 2000.");
            }

            var account = new Account
            {
                AccountHolderName = request.AccountHolderName,
                AccountNumber = _accountNumberGenerator.Generate(),
                Balance = request.InitialDeposit
            };

            account.PasswordHash = _passwordService.HashPassword(account, request.Password);

            try
            {
                var cerated = await _accountRepository.AddAsync(account);
                if (!cerated)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the account.");
                }

                var response = new
                {
                    account.AccountNumber,
                    account.AccountHolderName,
                    account.Balance,
                    account.CreatedAt
                };
                return Created(
                $"/api/accounts/{account.AccountNumber}",
                response);
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return Conflict(
                    "The account number already exists. Please try again.");
            }
        }

        [HttpGet]
        
        public async Task<IActionResult > GetAllAccounts()
        {
            var accounts = await _accountRepository.GetAllAsync();
            return Ok(accounts);
        }

        [HttpGet("{accountNumber}")]
        public async Task<IActionResult> GetAccountByNumber(string accountNumber)
        {
            var account = await _accountRepository.GetAccountByNumberAsync(accountNumber);
            if(account == null)
            {
                return NotFound($"Account with number {accountNumber} not found.");
            }

            return Ok(account);
        }

        [HttpGet("balance")]
        [Authorize]
        public async Task<IActionResult> CheckBalance()
        {
           
            var accountNumber = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(accountNumber))
                return Unauthorized("User account not found in token.");

            var account = await _accountRepository
                .GetAccountByNumberAsync(accountNumber);

            if (account == null)
                return NotFound("Account not found.");

            return Ok(new
            {
                Balance = account.Balance
            });
        }
    }
}
