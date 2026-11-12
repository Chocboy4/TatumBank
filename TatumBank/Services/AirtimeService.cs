
using System.Text.RegularExpressions;
using TatumBank.DTOs;
using TatumBank.Infrastructure.Repositories;

namespace TatumBank.Services
{
    public class AirtimeService : IAirtimeService
    {
        private readonly IAirtimeRepository _airtimeRepository;

        private static readonly string[] AllowedNetworks =
        {
            "MTN", "AIRTEL", "GLO", "ETISALAT"
        };

        public AirtimeService(IAirtimeRepository airtimeRepository)
        {
            _airtimeRepository = airtimeRepository;
        }

        public async Task<AirtimePurchaseResponseDto> PurchaseAirtimeAsync(
            string accountNumber,
            AirtimePurchaseRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                throw new ArgumentException("Authenticated account is required.");

            if (request.Amount < 50 || request.Amount > 100000)
                throw new ArgumentException(
                    "Amount must be between 50 and 100000.");

            var network = request.Network.Trim().ToUpperInvariant();

            if (!AllowedNetworks.Contains(network))
                throw new ArgumentException(
                    "Network must be MTN, AIRTEL, GLO, or ETISALAT.");

            
            var phoneNumber = request.PhoneNumber.Trim();

            if (!Regex.IsMatch(phoneNumber, @"^(0[789][01]\d{8}|\+234[789][01]\d{8})$"))
                throw new ArgumentException(
                    "Enter a valid Nigerian mobile phone number.");

            return await _airtimeRepository.PurchaseAsync(
                accountNumber,
                phoneNumber,
                network,
                request.Amount);
        }
    }
}
