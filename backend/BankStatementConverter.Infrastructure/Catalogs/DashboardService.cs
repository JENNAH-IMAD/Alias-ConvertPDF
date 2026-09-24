using Microsoft.EntityFrameworkCore;
using BankStatementConverter.Application;
namespace BankStatementConverter.Infrastructure;
public class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<DashboardDto> GetAsync(CancellationToken ct) => new(
        await db.Clients.CountAsync(ct), await db.Banks.CountAsync(ct), await db.BankAccounts.CountAsync(ct));
}
