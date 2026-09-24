using System.Security.Cryptography;
using System.Text;
using BankStatementConverter.Application;
using BankStatementConverter.Domain;
using Microsoft.EntityFrameworkCore;
namespace BankStatementConverter.Infrastructure;

public class CatalogDeletionService(AppDbContext db) : ICatalogDeletionService
{
    private async Task<(Entity Parent, List<BankAccount> Accounts)> Load(string resource, Guid id, CancellationToken ct)
    {
        Entity parent = resource switch {
            "banks" => await db.Banks.FindAsync([id],ct) ?? throw new AppException(404,"Banque introuvable."),
            "clients" => await db.Clients.FindAsync([id],ct) ?? throw new AppException(404,"Client introuvable."),
            _ => throw new AppException(400,"Type de suppression inconnu.")
        };
        var accounts = await db.BankAccounts.Where(a=>resource=="banks"?a.BankId==id:a.ClientId==id).ToListAsync(ct);
        return (parent,accounts);
    }
    private static DeletionPreview Describe((Entity Parent,List<BankAccount> Accounts) plan)
    {
        var entities=plan.Accounts.Cast<Entity>().Append(plan.Parent).OrderBy(e=>e.Id);
        var stamp=string.Join("|",entities.Select(e=>$"{e.Id}:{e.UpdatedAt.Ticks}"));
        return new(plan.Parent is Bank b?b.Name:((Client)plan.Parent).Name,plan.Accounts.Count,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(stamp))));
    }
    public async Task<DeletionPreview> PreviewAsync(string resource,Guid id,CancellationToken ct)=>Describe(await Load(resource,id,ct));
    public async Task DeleteAsync(string resource,Guid id,string version,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("LOCK TABLE \"BankAccounts\", \"Clients\", \"Banks\" IN SHARE ROW EXCLUSIVE MODE",ct);
        var plan=await Load(resource,id,ct);
        if(Describe(plan).Version!=version)throw new AppException(409,"Les éléments liés ont changé. Rouvrez la confirmation pour actualiser le récapitulatif.");
        db.BankAccounts.RemoveRange(plan.Accounts);await db.SaveChangesAsync(ct);
        db.Remove(plan.Parent);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
}
