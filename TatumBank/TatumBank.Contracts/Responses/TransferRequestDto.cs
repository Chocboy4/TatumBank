namespace TatumBank.DTOs
{
    public class TransferRequestDto
    {
        public required string RecipientAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        public string? RecipientBankCode { get; set; }
    }
}
