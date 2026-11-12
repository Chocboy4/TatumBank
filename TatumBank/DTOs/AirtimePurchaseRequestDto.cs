
using System.ComponentModel.DataAnnotations;

namespace TatumBank.DTOs
{
    public class AirtimePurchaseRequestDto
    {
        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [RegularExpression(
            "^(MTN|AIRTEL|GLO|ETISALAT)$",
            ErrorMessage = "Network must be MTN, AIRTEL, GLO, or ETISALAT."
        )]
        public string Network { get; set; } = string.Empty;

        [Range(50, 100000,
            ErrorMessage = "Airtime amount must be between 50 and 100000.")]
        public decimal Amount { get; set; }
    }
}
