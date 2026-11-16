using TatumBank.DTOs;

namespace TatumBank.Infrastructure.Repositories
{
    public interface IAuditLogRepository
    {
        Task AddAsync(int? adminId, string adminName, string action,
                      string? targetType, string? targetId, string? details, DateTime createdAtUtc);

        Task<PagedResult<AuditLogDto>> SearchAsync(AuditLogFilterDto filter);   
    }
}
