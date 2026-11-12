using Microsoft.AspNetCore.Identity;
using TatumBank.Models;

namespace TatumBank.Services;

public class PasswordService
{
    private readonly PasswordHasher<Account> _passwordHasher;

    public PasswordService()
    {
        _passwordHasher = new PasswordHasher<Account>();
    }

    public string HashPassword(Account account, string password)
    {
        return _passwordHasher.HashPassword(account, password);
    }

    public bool VerifyPassword(
        Account account,
        string hashedPassword,
        string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(
            account,
            hashedPassword,
            password);

        return result != PasswordVerificationResult.Failed;
    }
}
