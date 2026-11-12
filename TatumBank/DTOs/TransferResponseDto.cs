namespace TatumBank.DTOs
{
    public class TransferResponseDto
    {
        public string Reference { get; set; } = string.Empty;

        public string SenderAccountNumber { get; set; } = string.Empty;

        public string RecipientAccountNumber { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
