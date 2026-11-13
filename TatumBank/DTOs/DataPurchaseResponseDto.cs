namespace TatumBank.DTOs
{
    public class DataPurchaseResponseDto
    {
        public string Status { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Network { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PlanCode { get; set; } = string.Empty;
        public decimal Amount { get; set; } 
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
