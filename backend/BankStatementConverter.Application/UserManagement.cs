namespace BankStatementConverter.Application;

public static class Permissions
{
    public static readonly string[] All = ["dashboard.read", "clients.read", "clients.write", "clients.delete", "banks.read", "banks.write", "banks.delete", "accounts.read", "accounts.write", "accounts.delete"];
    public static readonly string[] Default = ["dashboard.read", "clients.read", "clients.write", "clients.delete", "banks.read", "accounts.read", "accounts.write", "accounts.delete"];
}
public record ManagedUserDto(Guid Id, string Name, string Email, string Role, bool IsActive, string[] Permissions, DateTime CreatedAt, DateTime UpdatedAt);
public record SaveUserInput(string Name, string Email, string Role, bool IsActive, string[] Permissions, string? Password, DateTime? Version);
public record ResetPasswordInput(string Password);
public interface IUserManagementService
{
    Task<UserDto> GetSessionUserAsync(Guid id, CancellationToken ct);
    Task<PageResult<ManagedUserDto>> ListAsync(int page, int pageSize, string? search, string? role, bool? active, CancellationToken ct);
    Task<ManagedUserDto> SaveAsync(Guid? id, SaveUserInput input, Guid actor, CancellationToken ct);
    Task DeleteAsync(Guid id, Guid actor, CancellationToken ct);
    Task ResetPasswordAsync(Guid id, string password, CancellationToken ct);
    Task RevokeSessionsAsync(Guid id, CancellationToken ct);
}
