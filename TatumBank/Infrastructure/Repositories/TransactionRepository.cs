using Dapper;
using System.Security.Cryptography.Xml;
using TatumBank.DTOs;
using TatumBank.Models;

namespace TatumBank.Infrastructure.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public TransactionRepository(
            IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> AddAsync(Transfer transfer)
        {
            using var connection =
                _connectionFactory.CreateConnection();

            const string sql = """
                INSERT INTO Transactions
                (
                    Reference, SenderAccountNumber, RecipientAccountNumber, Amount,
                    TransactionType, TransactionCategory, Description, Status
                )
                VALUES
                (
                    @Reference, @SenderAccountNumber, @RecipientAccountNumber, @Amount,
                    @TransactionType, 'Transfer', @Description, @Status
                );
                """;

            var rowsAffected = await connection.ExecuteAsync(
                sql,
                new
                {
                    transfer.Reference,
                    transfer.SenderAccountNumber,
                    transfer.RecipientAccountNumber,
                    transfer.Amount,
                    transfer.TransactionType,
                    transfer.Status,
                    Description = $"Transfer to {transfer.RecipientAccountNumber}"
                });

            return rowsAffected > 0;
        }


        public async Task<IEnumerable<TransactionResponseDto>> FilterAsync(
            string accountNumber,
            TransactionFilterDto filter)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = @"
                SELECT
        Id,
        Reference,
        TransactionCategory,
        CASE WHEN SenderAccountNumber = @AccountNumber
             THEN 'Debit' ELSE 'Credit' END AS TransactionType,
        Description,
        Amount,
        Status,
        PhoneNumber,
        Network,
        DataPlan,
        CreatedAt
    FROM Transactions
        WHERE (SenderAccountNumber = @AccountNumber
           OR RecipientAccountNumber = @AccountNumber)
      AND (@Reference IS NULL OR Reference = @Reference)
      AND (@TransactionCategory IS NULL OR TransactionCategory = @TransactionCategory)
      AND (@TransactionType IS NULL OR
           (CASE WHEN SenderAccountNumber = @AccountNumber
                 THEN 'Debit' ELSE 'Credit' END) = @TransactionType)
      AND (@Description IS NULL OR Description LIKE @DescriptionPattern)
      AND (@StartDate IS NULL OR CreatedAt >= @StartDate)
      AND (@EndDate IS NULL OR CreatedAt < @EndDateExclusive)
    ORDER BY CreatedAt DESC;";

            var parameters = new
            {
                AccountNumber = accountNumber,

                Reference = string.IsNullOrWhiteSpace(filter.Reference)
                    ? null
                    : filter.Reference.Trim(),

                TransactionType = string.IsNullOrWhiteSpace(filter.TransactionType)
                    ? null
                    : filter.TransactionType.Trim(),

                Description = string.IsNullOrWhiteSpace(filter.Description)
                    ? null
                    : filter.Description.Trim(),

                TransactionCategory = string.IsNullOrEmpty(filter.TransactionCategory)
                    ? null
                    : filter.TransactionCategory.Trim(),

                DescriptionPattern = string.IsNullOrWhiteSpace(filter.Description)
                    ? null
                    : $"%{filter.Description.Trim()}%",

                StartDate = filter.StartDate?.Date,

                EndDate = filter.EndDate?.Date.AddDays(1),

                EndDateExclusive = filter.EndDate?.Date.AddDays(1)
            };

            return await connection.QueryAsync<TransactionResponseDto>(
                sql,
                parameters);
        }


        public async Task<List<Transaction>> GetByAccountNumberAsync(
            string accountNumber)
        {
            using var connection =
                _connectionFactory.CreateConnection();

            const string sql = """
                SELECT
                    Id,
                    Reference,
                    SenderAccountNumber,
                    RecipientAccountNumber,
                    Amount,
                    TransactionType,
                    Status,
                    CreatedAt
                FROM Transactions
                WHERE SenderAccountNumber = @AccountNumber
                   OR RecipientAccountNumber = @AccountNumber
                ORDER BY CreatedAt DESC, Id DESC;
                """;

            var transactions = await connection.QueryAsync<Transaction>(
                sql,
                new { AccountNumber = accountNumber });

            return transactions.ToList();
        }
    }
}