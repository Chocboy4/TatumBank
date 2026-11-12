namespace TatumBank.DTOs
{
    public class CreateAccountRequest
    {
        public string AccountHolderName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public decimal InitialDeposit { get; set; }
    }
}
