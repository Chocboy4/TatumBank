namespace TatumBank.DTOs
{
    public class AirtimeVendRequestDto
    {
        public string Reference { get; set; } = string.Empty;
        public string RequesterBankCode { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Network { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}