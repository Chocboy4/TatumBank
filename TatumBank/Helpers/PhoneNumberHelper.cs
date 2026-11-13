using System.Text.RegularExpressions;

namespace TatumBank.Helpers
{
    public static class PhoneNumberHelper
    {
        private static readonly Regex NigerianMobile =
            new(@"^0[789][01]\d{8}$", RegexOptions.Compiled);

        public static bool TryNormalize(string? input, out string normalized)
        {
            normalized = string.Empty;
            if (string.IsNullOrWhiteSpace(input)) return false;

            var cleaned = input.Trim().Replace(" ", "").Replace("-", "");

            if (cleaned.StartsWith("+234") && cleaned.Length == 14)
                cleaned = "0" + cleaned[4..];
            else if (cleaned.StartsWith("234") && cleaned.Length == 13)
                cleaned = "0" + cleaned[3..];

            if (!NigerianMobile.IsMatch(cleaned)) return false;

            normalized = cleaned;
            return true;
        }
    }
}