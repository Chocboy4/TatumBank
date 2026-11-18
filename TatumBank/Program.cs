using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using TatumBank.Application.Abstractions;
using TatumBank.Filters;
using TatumBank.HttpClients;
using TatumBank.Infrastructure;
using TatumBank.Infrastructure.Repositories;
using TatumBank.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger with the "Authorize" button for Bearer tokens
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste your JWT token here (no need to type 'Bearer ')."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

// JWT authentication
var jwt = builder.Configuration.GetSection("Jwt");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    jwt["Key"] ?? throw new InvalidOperationException("JWT signing key is not configured.")))
        };
    });

builder.Services.AddAuthorization();

// Application services
builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IBankingService, BankingService>();
builder.Services.AddScoped<AccountNumberGenerator>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IAirtimeRepository, AirtimeRepository>();
builder.Services.AddScoped<IAirtimeService, AirtimeService>();
builder.Services.AddScoped<IDataPurchaseRepository, DataPurchaseRepository>();
builder.Services.AddScoped<IDataPurchaseService, DataPurchaseService>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<AdminSeeder>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<AdminSeeder>();
builder.Services.AddScoped<IInterbankTransferRepository, InterbankTransferRepository>();
builder.Services.AddScoped<IIncomingTransferRepository, IncomingTransferRepository>();
builder.Services.AddScoped<InterbankApiKeyFilter>();

builder.Services.AddHttpClient<IChocboyBankClient, ChocboyBankClient>(
    client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["BankOfChocboy:BaseUrl"]
            ?? throw new InvalidOperationException("BankOfChocboy BaseUrl is not configured."));
        client.Timeout = TimeSpan.FromSeconds(15);

        var apiKey = builder.Configuration["BankOfChocboy:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    });


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<AdminSeeder>().SeedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();