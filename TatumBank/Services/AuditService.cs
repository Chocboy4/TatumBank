using TatumBank.DTOs;
using TatumBank.Infrastructure.Repositories;

namespace TatumBank.Services
{
    public class AuditService : IAuditService
    {
        private readonly IAuditLogRepository _repository;
        public AuditService(IAuditLogRepository repository)
        {
            _repository = repository;
        }
        public Task LogAsync(int? adminId, string adminName, string action, string? targetType = null, string? targetId = null, string? details = null)
        {
            return _repository.AddAsync(
               adminId,
               Cut(adminName, 100) ?? "Unknown",
               Cut(action, 50)!,
               Cut(targetType, 30),
               Cut(targetId, 50),
               Cut(details, 500),
               DateTime.UtcNow);
        }

        public Task<PagedResult<AuditLogDto>> SearchAsync(AuditLogFilterDto filter)
        {
            filter.Page = Math.Max(filter.Page, 1);
            filter.PageSize = Math.Clamp(filter.PageSize, 1, 100);

            if (filter.StartDate.HasValue && filter.EndDate.HasValue && filter.StartDate > filter.EndDate)
                throw new ArgumentException("Start date cannot be later than end date.");

            return _repository.SearchAsync(filter);
        }

        private static string? Cut(string? value, int max)
            => value == null ? null : (value.Length <= max ? value : value[..max]);
    }
}
