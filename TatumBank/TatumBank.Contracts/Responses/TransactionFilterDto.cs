namespace TatumBank.DTOs
{
    public class TransactionFilterDto
    {
        public string? Reference { get; set; }

        public string? TransactionType { get; set; }
        public string? TransactionCategory { get; set; }

        public string? Description { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
