namespace TatumBank.DTOs
{
    public class BalanceResponseDto
    {
        public string AccountNumber { get; set; } = string.Empty;

        public string AccountHolderName { get; set; } = string.Empty;

        public decimal Balance { get; set; }

        public string Currency { get; set; } = "NGN";
    }
}