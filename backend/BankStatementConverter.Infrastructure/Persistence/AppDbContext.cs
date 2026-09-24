using BankStatementConverter.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BankStatementConverter.Infrastructure;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Bank> Banks => Set<Bank>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<User>().HasIndex(x => x.Username).IsUnique();
        b.Entity<Client>().HasIndex(x => x.ICE).IsUnique();
        b.Entity<Bank>().HasIndex(x => x.Code).IsUnique();
        b.Entity<BankAccount>().HasIndex(x => new { x.BankId, x.AccountNumber }).IsUnique();
        foreach (var entity in b.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(string))) property.SetMaxLength(2000);
            foreach (var fk in entity.GetForeignKeys()) fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var e in ChangeTracker.Entries<Entity>().Where(x => x.State == EntityState.Modified)) e.Entity.UpdatedAt = DateTime.UtcNow;
        return base.SaveChangesAsync(ct);
    }
}
public class DesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? "Host=localhost;Database=bankconverter").Options);
}
