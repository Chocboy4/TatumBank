namespace TatumBank.DTOs
{
    public class AirtimePurchaseResponseDto
    {
        public string Reference { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Network { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
