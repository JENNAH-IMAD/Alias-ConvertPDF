namespace BankStatementConverter.Domain;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
public class User : Entity
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;
    public string? Permissions { get; set; }
    public int SessionVersion { get; set; }
}
public class Client : Entity
{
    public byte[]? Photo { get; set; }
    public string? PhotoContentType { get; set; }
    public Guid? PhotoVersion { get; set; }
    public string Name { get; set; } = "";
    public string LegalName { get; set; } = "";
    public string ICE { get; set; } = "";
    public string IF { get; set; } = "";
    public string RC { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Country { get; set; } = "Maroc";
    public ICollection<BankAccount> Accounts { get; set; } = [];
}
public class Bank : Entity
{
    public byte[]? Logo { get; set; }
    public string? LogoContentType { get; set; }
    public Guid? LogoVersion { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public ICollection<BankAccount> Accounts { get; set; } = [];
}
public class BankAccount : Entity
{
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public Guid BankId { get; set; }
    public Bank Bank { get; set; } = null!;
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string Currency { get; set; } = "MAD";
    public string Journal { get; set; } = "BQ";
    public string AccountCode { get; set; } = "512000";
}
