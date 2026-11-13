namespace TatumBank.DTOs
{
    public class TransactionResponseDto
    {
        public int Id { get; set; }

        public string Reference { get; set; } = string.Empty;

        public string TransactionType { get; set; } = string.Empty;
        public string? TransactionCategory { get; set; }
        public string? DataPlan { get; set; }

        public string? Description { get; set; }

        public decimal Amount { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string? Network { get; set; }

        public DateTime CreatedAt { get; set; }

    }
}
