namespace TatumBank.DTOs
{
    public class InterbankTransferRequestDto
    {
        public string Reference { get; set; } = string.Empty;
        public string SenderAccountNumber { get; set; } = string.Empty;

        public string SenderBankCode { get; set; } = "TatumBank";

        public string RecipientAccountNumber { get; set; }
            = string.Empty;

        public decimal Amount { get; set; }
    }
}
