namespace TatumBank.Models
{
    public class Transfer
    {
        public int Id { get; set; }

        public string Reference { get; set; } = string.Empty;

        public string SenderAccountNumber { get; set; } = string.Empty;

        public string RecipientAccountNumber { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string TransactionType { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
