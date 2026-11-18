namespace TatumBank.Helpers
{
    public static class NetworkCatalog
    {
        public const string SupportedList =
            "MTN, Airtel, Glo, or 9mobile";

        public static string Normalize(string? network)
        {
            return network?.Trim().ToUpperInvariant() switch
            {
                "MTN" => "MTN",
                "AIRTEL" => "Airtel",
                "GLO" => "Glo",
                "9MOBILE" => "9mobile",
                _ => network?.Trim() ?? string.Empty
            };
        }

        public static bool IsSupported(string network)
        {
            return network is "MTN"
                or "Airtel"
                or "Glo"
                or "9mobile";
        }
    }
}