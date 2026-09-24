using BankStatementConverter.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace BankStatementConverter.Infrastructure;

public static class Seed
{
    public static async Task RunAsync(AppDbContext db, IConfiguration config)
    {
        var email = config["Seed:AdminEmail"]?.Trim().ToLowerInvariant(); var password = config["Seed:AdminPassword"];
        if (!string.IsNullOrEmpty(email) && !await db.Users.AnyAsync(x => x.Email == email))
        {
            Validation.Email(email); Validation.Password(password);
            var admin = new User { Username = email, Email = email, Role = "Admin" };
            admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, password!); db.Users.Add(admin);
        }
        await db.SaveChangesAsync();
    }
}
