using BankStatementConverter.Application;
using BankStatementConverter.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankStatementConverter.Infrastructure;

public class UserManagementService(AppDbContext db) : IUserManagementService
{
    public async Task<UserDto> GetSessionUserAsync(Guid id, CancellationToken ct) => SessionUser(await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id && u.IsActive, ct) ?? throw new AppException(401, "Session invalide."));
    public static string[] EffectivePermissions(User user) => user.Role == "Admin" ? Permissions.All :
        user.Permissions is null ? Permissions.Default : user.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries);
    public static UserDto SessionUser(User user) => new(user.Id, user.Username, user.Email, user.Role, EffectivePermissions(user));
    // PostgreSQL timestamps preserve microseconds; return the same precision for concurrency checks.
    private static ManagedUserDto Project(User user) => new(user.Id, user.Username, user.Email, user.Role, user.IsActive, EffectivePermissions(user), user.CreatedAt, new DateTime(user.UpdatedAt.Ticks - user.UpdatedAt.Ticks % 10, DateTimeKind.Utc));

    public async Task<PageResult<ManagedUserDto>> ListAsync(int page, int pageSize, string? search, string? role, bool? active, CancellationToken ct)
    {
        Validation.Require(page >= 1 && pageSize is >= 1 and <= 100, "Pagination invalide.");
        Validation.Require(search is null || search.Length <= 200, "Recherche trop longue.");
        Validation.Require(role is null or "" or "Admin" or "User", "Rôle invalide.");
        var query = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim().ToLowerInvariant(); query = query.Where(u => u.Username.ToLower().Contains(term) || u.Email.ToLower().Contains(term)); }
        if (!string.IsNullOrEmpty(role)) query = query.Where(u => u.Role == role);
        if (active.HasValue) query = query.Where(u => u.IsActive == active);
        var total = await query.CountAsync(ct);
        var users = await query.OrderBy(u => u.Username).ThenBy(u => u.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(users.Select(Project).ToArray(), total, page, pageSize);
    }

    private async Task LockAsync(CancellationToken ct) => await db.Database.ExecuteSqlRawAsync("LOCK TABLE \"Users\" IN SHARE ROW EXCLUSIVE MODE", ct);
    private async Task ProtectAdminAsync(User user, Guid actor, CancellationToken ct)
    {
        if (user.Id == actor) throw new AppException(409, "Vous ne pouvez pas supprimer, désactiver ou rétrograder votre propre compte.");
        if (user.Role == "Admin" && user.IsActive && !await db.Users.AnyAsync(u => u.Id != user.Id && u.Role == "Admin" && u.IsActive, ct))
            throw new AppException(409, "Au moins un administrateur actif doit être conservé.");
    }

    public async Task<ManagedUserDto> SaveAsync(Guid? id, SaveUserInput input, Guid actor, CancellationToken ct)
    {
        Validation.Text(input.Name, "Nom", 100); Validation.Email(input.Email);
        Validation.Require(input.Role is "Admin" or "User", "Rôle invalide.");
        Validation.Require(input.Permissions is not null && input.Permissions.All(Permissions.All.Contains), "Permission inconnue.");
        var permissions = input.Permissions!.Distinct().Order().ToArray();
        Validation.Require(!permissions.Any(p => p.StartsWith("statements.")) || permissions.Contains("clients.read") && permissions.Contains("accounts.read"), "Les relevés nécessitent la consultation des clients et comptes.");
        foreach (var module in new[] { "clients", "banks", "accounts", "statements" })
            Validation.Require(!permissions.Any(p => p == module + ".write" || p == module + ".delete") || permissions.Contains(module + ".read"), "La modification ou suppression nécessite le droit de consultation.");
        Validation.Require(!permissions.Contains("accounts.write") || (permissions.Contains("clients.read") && permissions.Contains("banks.read")), "La gestion des comptes nécessite la consultation des clients et banques.");
        Validation.Require(!permissions.Contains("clients.delete") && !permissions.Contains("banks.delete") || permissions.Contains("accounts.delete"), "La suppression avec comptes associés nécessite le droit de supprimer les comptes.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(ct);
        var user = id.HasValue ? await db.Users.FindAsync([id.Value], ct) ?? throw new AppException(404, "Utilisateur introuvable.") : new User();
        if (id.HasValue && input.Version != user.UpdatedAt) throw new AppException(409, "Ce compte a changé. Actualisez la liste avant de modifier.");
        if (id.HasValue && (!input.IsActive || input.Role != "Admin" && user.Role == "Admin")) await ProtectAdminAsync(user, actor, ct);
        var name = input.Name.Trim(); var email = input.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Id != user.Id && (u.Email.ToLower() == email || u.Username.ToLower() == name.ToLower()), ct)) throw new AppException(409, "Nom ou adresse e-mail déjà utilisé.");
        if (!id.HasValue) { Validation.Password(input.Password); user.PasswordHash = new PasswordHasher<User>().HashPassword(user, input.Password!); db.Users.Add(user); }
        user.Username = name; user.Email = email; user.Role = input.Role; user.IsActive = input.IsActive;
        user.Permissions = string.Join(',', permissions); user.SessionVersion++;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return Project(user);
    }
    public async Task DeleteAsync(Guid id, Guid actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct); await LockAsync(ct);
        var user = await db.Users.FindAsync([id], ct) ?? throw new AppException(404, "Utilisateur introuvable.");
        await ProtectAdminAsync(user, actor, ct);
        db.Users.Remove(user); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task ResetPasswordAsync(Guid id, string password, CancellationToken ct)
    {
        Validation.Password(password);
        await using var tx = await db.Database.BeginTransactionAsync(ct); await LockAsync(ct);
        var user = await db.Users.FindAsync([id], ct) ?? throw new AppException(404, "Utilisateur introuvable.");
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password); user.SessionVersion++;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task RevokeSessionsAsync(Guid id, CancellationToken ct)
    {
        var changed = await db.Users.Where(u => u.Id == id).ExecuteUpdateAsync(s => s.SetProperty(u => u.SessionVersion, u => u.SessionVersion + 1).SetProperty(u => u.UpdatedAt, DateTime.UtcNow), ct);
        if (changed == 0) throw new AppException(404, "Utilisateur introuvable.");
    }
}

