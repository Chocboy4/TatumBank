using Dapper;
using TatumBank.Application.Abstractions;
using TatumBank.DTOs;

namespace TatumBank.Infrastructure.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        public AuditLogRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }
        public async Task AddAsync(int? adminId, string adminName, string action, string? targetType, string? targetId, string? details, DateTime createdAtUtc)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                @"INSERT INTO AdminAuditLogs
                    (AdminId, AdminName, Action, TargetType, TargetId, Details, CreatedAt)
                  VALUES
                    (@AdminId, @AdminName, @Action, @TargetType, @TargetId, @Details, @CreatedAt);",
                new
                {
                    AdminId = adminId,
                    AdminName = adminName,
                    Action = action,
                    TargetType = targetType,
                    TargetId = targetId,
                    Details = details,
                    CreatedAt = createdAtUtc
                });
        }

        public async Task<PagedResult<AuditLogDto>> SearchAsync(AuditLogFilterDto filter)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            const string where = @"
                WHERE (@AdminPattern IS NULL OR AdminName LIKE @AdminPattern)
                  AND (@Action IS NULL OR Action = @Action)
                  AND (@TargetId IS NULL OR TargetId = @TargetId)
                  AND (@StartDate IS NULL OR CreatedAt >= @StartDate)
                  AND (@EndDateExclusive IS NULL OR CreatedAt < @EndDateExclusive)";
            var parameters = new
            {
                AdminPattern = string.IsNullOrWhiteSpace(filter.AdminName) ? null : $"%{filter.AdminName.Trim()}%",
                Action = string.IsNullOrWhiteSpace(filter.Action) ? null : filter.Action.Trim(),
                TargetId = string.IsNullOrWhiteSpace(filter.TargetId) ? null : filter.TargetId.Trim(),
                StartDate = filter.StartDate?.Date,
                EndDateExclusive = filter.EndDate?.Date.AddDays(1),
                PageSize = filter.PageSize,
                Offset = (filter.Page - 1) * filter.PageSize
            };

            var total = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM AdminAuditLogs " + where, parameters);

            var items = await connection.QueryAsync<AuditLogDto>(
                @"SELECT Id, AdminId, AdminName, Action, TargetType, TargetId, Details, CreatedAt
                  FROM AdminAuditLogs " + where + @"
                  ORDER BY CreatedAt DESC, Id DESC
                  LIMIT @PageSize OFFSET @Offset;", parameters);

            return new PagedResult<AuditLogDto>
            {
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = total,
                Items = items.ToList()
            };

        }
    }
}
