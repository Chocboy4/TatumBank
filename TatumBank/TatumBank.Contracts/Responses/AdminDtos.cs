namespace TatumBank.DTOs
{
    public class PagedResult<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
        public List<T> Items { get; set; } = new();
    }

    public class AdminAccountDto
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public bool IsFrozen { get; set; }
    }

    public class AdminTransactionDto
    {
        public int Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string? SenderAccountNumber { get; set; }
        public string? RecipientAccountNumber { get; set; }
        public decimal Amount { get; set; }
        public string? TransactionCategory { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Network { get; set; }
        public string? DataPlan { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AuditLogDto
    {
        public long Id { get; set; }
        public int? AdminId { get; set; }
        public string AdminName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? TargetType { get; set; }
        public string? TargetId { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminAccountFilterDto
    {
        public string? Search { get; set; }          // matches account number or holder name
        public bool? IsFrozen { get; set; }
        public decimal? MinBalance { get; set; }
        public decimal? MaxBalance { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AdminTransactionFilterDto
    {
        public string? AccountNumber { get; set; }   // sender OR recipient
        public string? Reference { get; set; }
        public string? Category { get; set; }        // Transfer | Airtime | Data
        public string? Status { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AuditLogFilterDto
    {
        public string? AdminName { get; set; }
        public string? Action { get; set; }
        public string? TargetId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}