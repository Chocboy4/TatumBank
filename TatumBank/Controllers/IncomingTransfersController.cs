using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TatumBank.Application.Abstractions;
using TatumBank.DTOs;
using TatumBank.Infrastructure.Repositories;

namespace TatumBank.Controllers;

[Route("api/incoming-transfers")]
[ApiController]
[AllowAnonymous]
[ServiceFilter(typeof(Filters.InterbankApiKeyFilter))]
public sealed class IncomingTransfersController : ControllerBase
{
    private const string SendingBankCode = "BankOfChocboy";
    private readonly IIncomingTransferRepository _repository;

    public IncomingTransfersController(IIncomingTransferRepository repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(
        [FromBody] InterbankTransferRequestDto request,
        CancellationToken cancellationToken)
    {
        request.Reference = request.Reference?.Trim() ?? string.Empty;
        request.SenderAccountNumber = request.SenderAccountNumber?.Trim() ?? string.Empty;
        request.RecipientAccountNumber = request.RecipientAccountNumber?.Trim() ?? string.Empty;
        request.SenderBankCode = request.SenderBankCode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(request.Reference) ||
            string.IsNullOrWhiteSpace(request.SenderAccountNumber) ||
            string.IsNullOrWhiteSpace(request.RecipientAccountNumber))
        {
            return BadRequest(new
            {
                status = "Failed",
                message = "Reference and account numbers are required."
            });
        }

        if (request.Reference.Length > 60 ||
            request.SenderAccountNumber.Length > 20 ||
            request.RecipientAccountNumber.Length > 20)
        {
            return BadRequest(new
            {
                status = "Failed",
                message = "A field is too long."
            });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new
            {
                status = "Failed",
                message = "Amount must be greater than zero."
            });
        }

        if (!string.Equals(
                request.SenderBankCode,
                SendingBankCode,
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                status = "Failed",
                message = "Unknown sender bank."
            });
        }

        var result = await _repository.CreditAsync(request, cancellationToken);

        return result switch
        {
            IIncomingTransferRepository.IncomingTransferResult.Credited =>
                Ok(new
                {
                    status = "Successful",
                    message = "Transfer credited.",
                    reference = request.Reference
                }),

            IIncomingTransferRepository.IncomingTransferResult.Duplicate =>
                Ok(new
                {
                    status = "Successful",
                    message = "Transfer already processed.",
                    reference = request.Reference
                }),

            _ => NotFound(new
            {
                status = "Failed",
                message = "Recipient account was not found."
            })
        };
    }
}
