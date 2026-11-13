using TatumBank.DTOs;
using TatumBank.Helpers;
using TatumBank.Infrastructure.Repositories;

namespace TatumBank.Services
{
    public class DataPurchaseService : IDataPurchaseService
    {
        private readonly IDataPurchaseRepository _dataPurchaseRepository;

        public DataPurchaseService(IDataPurchaseRepository dataPurchaseRepository)
        {
            _dataPurchaseRepository = dataPurchaseRepository;
        }

        public IEnumerable<NetworkPlansDto> GetAvailableDataPlans()
        {
            return DataPlanCatalog.All
        .GroupBy(p => p.Network)
        .Select(g => new NetworkPlansDto
        {
            Network = g.Key,
            Plans = g.Select(p => new DataPlanDto
            {
                PlanCode = p.Code,
                Name = p.Name,
                Price = p.Price
            }).ToList()
        });
        }

        public async Task<DataPurchaseResponseDto> PurchaseDataAsync(string accountNumber, DataPurchaseRequestDto request)
        {
            var response = new DataPurchaseResponseDto
            {
                PhoneNumber = request.PhoneNumber,
                Network = request.Network,
                PlanCode = request.PlanCode,
                CreatedAt = DateTime.UtcNow
            };

            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                response.Status = "Failed";
                response.Message = "Account number is required.";
                return response;
            }

            if (!PhoneNumberHelper.TryNormalize(request.PhoneNumber, out var phoneNumber))
            {
                response.Status = "Failed";
                response.Message = "Invalid phone number. Use a Nigerian number like 08031234567.";
                return response;
            }

            response.PhoneNumber = phoneNumber;

            var network = (request.Network ?? string.Empty).Trim().ToUpperInvariant();
            var planCode = (request.PlanCode ?? string.Empty).Trim().ToUpperInvariant();


            if (!DataPlanCatalog.TryGet(planCode, out var plan) || plan.Network != network)
            {
                response.Status = "Failed";
                response.Message = "The selected data plan is invalid for this network.";
                return response;
            }

            response.Network = network;
            response.PlanCode = planCode;
            response.Amount = plan.Price;

            var reference = $"DAT-{Guid.NewGuid():N}";

            var Success = await _dataPurchaseRepository.PurchaseAsync(accountNumber, request.PhoneNumber, network, planCode, plan.Price, reference);

            if (!Success)
            {
                response.Status = "Failed";
                response.Message = "Data purchase failed. Check your balance and try again.";
                return response;
            }

            response.Status = "Success";
            response.Message = "Data purchase successful.";
            response.Reference = reference;


            return response;
        }
    }
}
