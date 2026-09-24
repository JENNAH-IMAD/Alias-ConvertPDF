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
    public DbSet<BankStatement> BankStatements => Set<BankStatement>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
    public DbSet<BankStatementProfile> BankStatementProfiles => Set<BankStatementProfile>();
    public DbSet<ExportTemplate> ExportTemplates => Set<ExportTemplate>();
    public DbSet<ExportTemplateField> ExportTemplateFields => Set<ExportTemplateField>();
    public DbSet<StatementExport> StatementExports => Set<StatementExport>();
    public DbSet<ProcessingHistory> ProcessingHistories => Set<ProcessingHistory>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<User>().HasIndex(x => x.Username).IsUnique();
        b.Entity<Client>().HasIndex(x => x.ICE).IsUnique();
        b.Entity<Bank>().HasIndex(x => x.Code).IsUnique();
        b.Entity<BankAccount>().HasIndex(x => new { x.BankId, x.AccountNumber }).IsUnique();
        b.Entity<BankStatement>().HasMany(x => x.Transactions).WithOne().HasForeignKey(x => x.BankStatementId);
        b.Entity<BankStatement>().HasMany(x => x.Exports).WithOne().HasForeignKey(x => x.BankStatementId);
        b.Entity<BankStatement>().HasMany(x => x.History).WithOne().HasForeignKey(x => x.BankStatementId);
        b.Entity<ExportTemplate>().HasMany(x => x.Fields).WithOne().HasForeignKey(x => x.ExportTemplateId);
        b.Entity<BankStatement>().HasIndex(x => new { x.BankAccountId, x.FileHash }).IsUnique();
        b.Entity<BankStatement>().HasIndex(x => new { x.CreatedBy, x.Status, x.CreatedAt });
        b.Entity<BankStatement>().HasIndex(x => new { x.PeriodStart, x.PeriodEnd });
        b.Entity<BankStatement>().Property(x => x.Version).IsConcurrencyToken();
        b.Entity<ExportTemplate>().Property(x => x.Version).IsConcurrencyToken();
        b.Entity<ExportTemplate>().HasIndex(x => x.Code).IsUnique();
        b.Entity<BankStatementProfile>().HasIndex(x => new { x.Code, x.Version }).IsUnique();
        foreach (var type in new[] { typeof(BankStatement), typeof(BankTransaction), typeof(BankStatementProfile), typeof(ExportTemplate), typeof(ExportTemplateField), typeof(StatementExport), typeof(ProcessingHistory) })
            b.Entity(type).Property(nameof(Entity.Id)).ValueGeneratedNever();
        foreach (var entity in b.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(string))) property.SetMaxLength(2000);
            foreach (var fk in entity.GetForeignKeys()) fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
        b.Entity<BankStatement>().Property(x => x.RawText).HasColumnType("text").Metadata.SetMaxLength(null);
        b.Entity<StatementExport>().Property(x => x.TemplateSnapshot).HasColumnType("text").Metadata.SetMaxLength(null);
        foreach (var property in b.Model.GetEntityTypes().SelectMany(e => e.GetProperties()).Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        { property.SetPrecision(20); property.SetScale(4); }
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

