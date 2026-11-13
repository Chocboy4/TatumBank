namespace TatumBank.DTOs
{
    public class DataPlanDto
    {
        public string PlanCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class NetworkPlansDto
    {
        public string Network { get; set; } = string.Empty;
        public List<DataPlanDto> Plans { get; set; } = new();
    }
}
