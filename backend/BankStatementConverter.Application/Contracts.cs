using BankStatementConverter.Domain;
namespace BankStatementConverter.Application;

public class AppException(int status, string message) : Exception(message) { public int Status { get; } = status; }
public record PageResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
public record UserDto(Guid Id, string Name, string Email, string Role, string[]? Permissions = null);
public record AuthResult(string Token, DateTime ExpiresAt, UserDto User);
public record LoginDto(string Email, string Password);
public record RegisterDto(string Username, string Email, string Password);
public record ClientDto(Guid Id, string Name, string LegalName, string ICE, string IF, string RC, string Email, string Phone, string Country, string[] Banks, string Status = "Actif", Guid? PhotoVersion = null);
public record ClientInput(string Name, string LegalName, string ICE, string IF, string RC, string Email, string Phone, string Country = "Maroc");
public record BankDto(Guid Id, string Name, string Code, string Description, int Clients, int Accounts = 0, Guid? LogoVersion = null);
public record BankAccountSummary(Guid Id, string Client, string AccountName, string AccountNumber, string Currency);
public record BankInput(string Name, string Code, string Description);
public record BankAccountDto(Guid Id, Guid ClientId, Guid BankId, string Bank, string AccountNumber, string AccountName, string Currency, string Journal, string AccountCode);
public record BankAccountInput(Guid ClientId, Guid BankId, string AccountNumber, string AccountName, string Currency, string Journal, string AccountCode);
public interface IAuthenticationService
{
    Task<AuthResult> LoginAsync(LoginDto input, CancellationToken ct);
    Task<UserDto> RegisterAsync(RegisterDto input, CancellationToken ct);
}
