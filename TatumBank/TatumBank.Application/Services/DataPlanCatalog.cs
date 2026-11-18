namespace TatumBank.Services
{

    public record DataPlan(string Code, string Network, string Name, decimal Price);
    public static class DataPlanCatalog
    {
        public static readonly IReadOnlyList<DataPlan> All = new List<DataPlan>
        {
            new DataPlan("MTN-1GB", "MTN", "1GB Data Plan", 1000m),
            new DataPlan("MTN-2GB", "MTN", "2GB Data Plan", 2000m),
            new DataPlan("MTN-5GB", "MTN", "5GB Data Plan", 5000m),
            new DataPlan("GLO-1GB", "GLO", "1GB Data Plan", 900m),
            new DataPlan("GLO-2GB", "GLO", "2GB Data Plan", 1800m),
            new DataPlan("GLO-5GB", "GLO", "5GB Data Plan", 4500m),
            new DataPlan("AIRTEL-1GB", "AIRTEL", "1GB Data Plan", 950m),
            new DataPlan("AIRTEL-2GB", "AIRTEL", "2GB Data Plan", 1900m),
            new DataPlan("AIRTEL-5GB", "AIRTEL", "5GB Data Plan", 4750m),
            new DataPlan("9MOBILE-1GB", "9MOBILE", "1GB Data Plan", 1100m),
            new DataPlan("9MOBILE-2GB", "9MOBILE", "2GB Data Plan", 2200m),
            new DataPlan("9MOBILE-5GB", "9MOBILE", "5GB Data Plan", 5500m)
        };

        public static bool TryGet(string code, out DataPlan plan)
        {
            plan = All.FirstOrDefault(p => p.Code == code)!;
            return plan is not null;
        }
    }
}
