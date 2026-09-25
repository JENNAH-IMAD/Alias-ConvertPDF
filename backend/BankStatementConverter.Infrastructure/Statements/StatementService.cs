using System.Security.Cryptography;
using BankStatementConverter.Application;
using BankStatementConverter.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace BankStatementConverter.Infrastructure;

public sealed class StatementService(AppDbContext db, IFileStorageService files, IStatementValidator validator, ILogger<StatementService> logger)
{
    public const int ArchiveLimit = 5;
    // Dans l'espace client, chaque relevé importé constitue une entrée de l'archive,
    // quel que soit son état de traitement.
    public Task<int> ArchiveCount(Guid clientId, CancellationToken ct) => db.BankStatements.CountAsync(s => s.BankAccount.ClientId == clientId, ct);
    public async Task<object> ArchiveUsage(Guid clientId, CancellationToken ct)
    {
        if (!await db.Clients.AnyAsync(c => c.Id == clientId, ct)) throw new AppException(404, "Client introuvable.");
        return new { count = await ArchiveCount(clientId, ct), limit = ArchiveLimit };
    }
    public IQueryable<BankStatement> Visible(StatementActor actor) => db.BankStatements.Where(s => actor.Admin || s.CreatedBy == actor.Id);
    public async Task<BankStatement> Load(Guid id, StatementActor actor, CancellationToken ct) => await Visible(actor)
        .Include(s => s.BankAccount).ThenInclude(a => a.Bank).Include(s => s.BankAccount).ThenInclude(a => a.Client)
        .Include(s => s.Transactions).Include(s => s.Exports).Include(s => s.History).AsSplitQuery().SingleOrDefaultAsync(s => s.Id == id, ct) ?? throw new AppException(404, "Relevé introuvable ou non autorisé.");
    public static void Guard(BankStatement s, Guid version, bool editable = true)
    {
        if (s.Version != version) throw new AppException(409, "Le relevé a changé. Actualisez avant de continuer.");
        if (s.Status is "QUEUED" or "ANALYZING" or "OCR_PROCESSING" or "EXPORTING") throw new AppException(409, "Un traitement est déjà en cours.");
        if (editable && s.ArchivedAt is not null) throw new AppException(409, "Ce relevé est archivé et ne peut plus être modifié.");
    }
    public static void History(BankStatement s, Guid actor, string action, string message)
    {
        s.History.Add(new() { UserId = actor, Action = action, Status = s.Status, Message = message }); s.Version = Guid.NewGuid();
    }
    public async Task<PageResult<StatementRow>> List(StatementActor actor, int page, int pageSize, Guid? clientId, Guid? accountId, bool archived, string? status, CancellationToken ct, Guid? bankId = null, DateOnly? from = null, DateOnly? to = null, bool? exported = null)
    {
        Validation.Require(page is >= 1 and <= 100000, "Page invalide.");
        Validation.Require(pageSize is >= 1 and <= 20, "Taille de page invalide.");
        Validation.Require(!from.HasValue || !to.HasValue || from <= to, "La date de début doit précéder la date de fin.");
        var query = Visible(actor).AsNoTracking();
        if (clientId.HasValue) query = query.Where(s => s.BankAccount.ClientId == clientId);
        if (accountId.HasValue) query = query.Where(s => s.BankAccountId == accountId);
        if (bankId.HasValue) query = query.Where(s => s.BankAccount.BankId == bankId);
        if (from.HasValue) query = query.Where(s => s.PeriodEnd >= from);
        if (to.HasValue) query = query.Where(s => s.PeriodStart <= to);
        if (exported.HasValue) query = query.Where(s => s.Exports.Any() == exported.Value);
        if (archived) query = query.Where(s => s.ArchivedAt != null);
        if (!string.IsNullOrEmpty(status)) query = query.Where(s => s.Status == status);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(s => s.ArchivedAt ?? s.CreatedAt).ThenBy(s => s.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(s => new StatementRow(s.Id, s.BankAccountId, s.BankAccount.ClientId, s.BankAccount.Client.Name, s.BankAccount.Bank.Name, s.BankAccount.AccountNumber, s.OriginalFileName, s.PeriodStart, s.PeriodEnd, s.Currency, s.Status, s.Transactions.Count, s.Exports.Count, s.CreatedAt, s.ArchivedAt, s.Version)).ToListAsync(ct);
        return new(rows, total, page, pageSize);
    }
    public async Task<Guid> Upload(Guid accountId, StatementActor actor, string name, string mime, byte[] bytes, CancellationToken ct)
    {
        Validation.Require(Path.GetExtension(name).Equals(".pdf", StringComparison.OrdinalIgnoreCase) && mime == "application/pdf", "Seuls les fichiers PDF sont acceptés.");
        if (bytes.Length is < 5 or > 20 * 1024 * 1024) throw new AppException(413, "PDF vide ou supérieur à 20 Mo.");
        Validation.Require(bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8), "Signature PDF invalide.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var account = await db.BankAccounts.FindAsync([accountId], ct) ?? throw new AppException(404, "Compte introuvable.");
        // Verrouille le dossier client afin que deux imports simultanés ne dépassent pas la limite de cinq relevés.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Clients\" WHERE \"Id\" = {account.ClientId} FOR UPDATE", ct);
        if (await ArchiveCount(account.ClientId, ct) >= ArchiveLimit)
            throw new AppException(409, "Limite atteinte : l'archive de ce client contient déjà 5 relevés. Supprimez un relevé avant un nouvel import.");
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        if (await db.BankStatements.AnyAsync(s => s.BankAccountId == accountId && s.FileHash == hash, ct)) throw new AppException(409, "Ce PDF est déjà importé pour ce compte. Retrouvez le relevé existant dans l'espace client ou demandez à un administrateur.");
        var stored = await files.SaveAsync(bytes, ct);
        var statement = new BankStatement { BankAccountId = accountId, CreatedBy = actor.Id, Currency = account.Currency, OriginalFileName = Path.GetFileName(name.Replace('\\', '/'))[..Math.Min(Path.GetFileName(name.Replace('\\', '/')).Length, 200)], StorageKey = stored.Key, FileHash = stored.Hash, FileSize = stored.Size };
        History(statement, actor.Id, "PDF_UPLOADED", "PDF original conservé dans le stockage privé.");
        db.BankStatements.Add(statement);
        try { await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); } catch { await files.DeleteAsync(stored.Key, CancellationToken.None); throw; }
        return statement.Id;
    }
    public async Task Review(Guid id, ReviewInput input, StatementActor actor, CancellationToken ct)
    {
        var s = await Load(id, actor, ct); Guard(s, input.Version);
        if (s.PageCount == 0) throw new AppException(422, "Analysez le PDF avant de saisir la revue.");
        Validation.Require(input.Transactions is { Count: <= 10000 }, "Maximum 10 000 opérations.");
        foreach (var row in input.Transactions) {
            Validation.Require(row.Reference is not null && row.Reference.Length <= 200 && row.Description is not null && row.Description.Length <= 2000, "Référence ou libellé trop long.");
            foreach (var amount in new[] { row.Debit, row.Credit, row.Balance }) ValidateAmount(amount);
        }
        ValidateAmount(input.OpeningBalance); ValidateAmount(input.ClosingBalance);
        var existing = s.Transactions.ToDictionary(t => t.Id);
        var retainedIds = input.Transactions.Where(t => t.Id.HasValue).Select(t => t.Id!.Value).ToList();
        Validation.Require(retainedIds.Distinct().Count() == retainedIds.Count && retainedIds.All(existing.ContainsKey), "Identifiant d'opération invalide ou répété.");
        foreach (var removed in s.Transactions.Where(t => !retainedIds.Contains(t.Id)).ToList())
        { db.BankTransactions.Remove(removed); s.Transactions.Remove(removed); }
        foreach (var (row, i) in input.Transactions.Select((r, i) => (r, i)))
        {
            var transaction = row.Id.HasValue ? existing[row.Id.Value] : new BankTransaction();
            transaction.Position = i + 1; transaction.TransactionDate = row.TransactionDate; transaction.ValueDate = row.ValueDate;
            transaction.Reference = row.Reference; transaction.Description = row.Description;
            transaction.Debit = row.Debit; transaction.Credit = row.Credit; transaction.Balance = row.Balance;
            transaction.Currency = s.Currency; transaction.IsValidated = false;
            if (!row.Id.HasValue) s.Transactions.Add(transaction);
        }
        s.PeriodStart = input.PeriodStart; s.PeriodEnd = input.PeriodEnd; s.OpeningBalance = input.OpeningBalance; s.ClosingBalance = input.ClosingBalance;
        s.ValidatedAt = null; s.ValidatedBy = null; s.Status = "REVIEW_REQUIRED"; s.Message = "Corrections enregistrées. Une validation finale est nécessaire.";
        History(s, actor.Id, "TRANSACTIONS_UPDATED", $"Revue manuelle : {s.Transactions.Count} opérations.");
        await db.SaveChangesAsync(ct);
    }
    public async Task Validate(Guid id, Guid version, StatementActor actor, CancellationToken ct)
    {
        var s = await Load(id, actor, ct); Guard(s, version);
        if (s.PageCount == 0) throw new AppException(422, "Analysez le PDF avant validation.");
        var check = validator.Check(s);
        if (check.Issues.Count > 0)
        {
            s.Status = "VALIDATION_FAILED"; s.ValidatedAt = null; s.ValidatedBy = null;
            s.Message = string.Join(" ", check.Issues.Take(5).Select(i => i.Message));
            History(s, actor.Id, "VALIDATION_FAILED", s.Message);
            await db.SaveChangesAsync(ct);
            throw new AppException(422, s.Message);
        }
        s.Status = "VALIDATED"; s.ValidatedAt = DateTime.UtcNow; s.ValidatedBy = actor.Id;
        foreach (var t in s.Transactions) t.IsValidated = true;
        History(s, actor.Id, "STATEMENT_VALIDATED", "Relevé contrôlé et validé par l'utilisateur."); await db.SaveChangesAsync(ct);
    }
    private static void ValidateAmount(decimal? amount)
    {
        if (amount.HasValue && (amount.Value <= -1_000_000_000_000_000m || amount.Value >= 1_000_000_000_000_000m || decimal.Round(amount.Value, 4) != amount.Value))
            throw new AppException(422, "Montant hors limites ou supérieur à 4 décimales.");
    }
    public async Task Archive(Guid id, Guid version, StatementActor actor, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var s = await Load(id, actor, ct); Guard(s, version);
        if (s.ValidatedAt is null) throw new AppException(422, "Validez le relevé avant archivage.");
        s.Status = "ARCHIVED"; s.ArchivedAt = DateTime.UtcNow; History(s, actor.Id, "STATEMENT_ARCHIVED", "Relevé archivé, original et exports conservés."); await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task Reopen(Guid id, Guid version, StatementActor actor, CancellationToken ct)
    {
        var s = await Load(id, actor, ct); Guard(s, version, editable: false);
        if (s.ArchivedAt is null) throw new AppException(409, "Ce relevé n'est pas archivé.");
        s.ArchivedAt = null;
        s.Status = s.ValidatedAt is null ? "REVIEW_REQUIRED" : "VALIDATED";
        s.Message = "Archive rouverte pour consultation et modification.";
        History(s, actor.Id, "STATEMENT_REOPENED", "Relevé retiré des archives pour modification.");
        await db.SaveChangesAsync(ct);
    }

    public async Task Delete(Guid id, Guid version, StatementActor actor, CancellationToken ct)
    {
        var s = await Load(id, actor, ct); Guard(s, version, editable: false);
        var storageKeys = s.Exports.Select(e => e.StorageKey).Append(s.StorageKey).Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToArray();

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        db.BankTransactions.RemoveRange(s.Transactions);
        db.StatementExports.RemoveRange(s.Exports);
        db.ProcessingHistories.RemoveRange(s.History);
        db.BankStatements.Remove(s);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        foreach (var storageKey in storageKeys)
        {
            try { await files.DeleteAsync(storageKey, ct); }
            catch (Exception error) { logger.LogWarning(error, "Le fichier privé {StorageKey} du relevé supprimé n'a pas pu être effacé.", storageKey); }
        }
    }
}
