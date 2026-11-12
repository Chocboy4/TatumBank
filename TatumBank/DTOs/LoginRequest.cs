namespace TatumBank.DTOs
{
    public class LoginRequest
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
