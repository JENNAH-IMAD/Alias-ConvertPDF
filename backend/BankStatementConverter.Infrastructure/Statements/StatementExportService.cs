using System.Text.Json;
using BankStatementConverter.Application;
using BankStatementConverter.Domain;
using Microsoft.EntityFrameworkCore;
namespace BankStatementConverter.Infrastructure;

public sealed class StatementExportService(AppDbContext db, StatementService statements, ExportTemplateService templates, IStatementExporter exporter, IFileStorageService files)
{
    public async Task<DownloadFile> Converted(Guid id, StatementActor actor, CancellationToken ct, int? limit = null)
    {
        var s = await statements.Load(id, actor, ct);
        StatementService.Guard(s, s.Version, false);
        if (s.Transactions.Count == 0) throw new AppException(422, "Aucune opération convertie disponible. Analysez le PDF ou enregistrez la revue.");
        var template = new ExportTemplate { Encoding = "UTF-8", Delimiter = ";", DateFormat = "yyyy-MM-dd", DecimalSeparator = ",", Decimals = 4,
            Fields = [new() { SourceField="Date", OutputField="Date", Position=0 }, new() { SourceField="ValueDate", OutputField="Date de valeur", Position=1 }, new() { SourceField="Reference", OutputField="Référence", Position=2 }, new() { SourceField="Description", OutputField="Libellé", Position=3 }, new() { SourceField="Debit", OutputField="Débit", Position=4 }, new() { SourceField="Credit", OutputField="Crédit", Position=5 }, new() { SourceField="Balance", OutputField="Solde", Position=6 }, new() { SourceField="Currency", OutputField="Devise", Position=7 }] };
        return new(exporter.Export(s, template, limit), $"Releve_converti_{s.Id:N}_{(s.ValidatedAt.HasValue ? "valide" : "a_verifier")}.csv", "text/csv; charset=utf-8");
    }
    public async Task<object> StoredPreview(Guid exportId, StatementActor actor, CancellationToken ct)
    {
        var export = await db.StatementExports.AsNoTracking().SingleOrDefaultAsync(e => e.Id == exportId, ct) ?? throw new AppException(404, "Export introuvable.");
        if (!await statements.Visible(actor).AnyAsync(s => s.Id == export.BankStatementId, ct)) throw new AppException(404, "Export introuvable ou non autorisé.");
        using var snapshot = JsonDocument.Parse(export.TemplateSnapshot);
        var encodingName = snapshot.RootElement.GetProperty("Encoding").GetString();
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var encoding = System.Text.Encoding.GetEncoding(encodingName == "Windows-1252" ? "Windows-1252" : "UTF-8");
        var text = encoding.GetString(await files.ReadAsync(export.StorageKey, ct));
        return new { text = text[..Math.Min(text.Length, 32000)], truncated = text.Length > 32000, name = export.FileName };
    }
    public async Task<(byte[] Bytes, ExportTemplate Template)> Preview(Guid id, ExportInput input, StatementActor actor, CancellationToken ct)
    {
        var s = await statements.Load(id, actor, ct); StatementService.Guard(s, input.Version, false);
        var template = await templates.Get(input.TemplateId, ct);
        if (!template.IsActive) throw new AppException(422, "Modèle désactivé.");
        return (exporter.Export(s, template, 10), template);
    }
    public async Task<Guid> Generate(Guid id, ExportInput input, StatementActor actor, CancellationToken ct)
    {
        var s = await statements.Load(id, actor, ct); StatementService.Guard(s, input.Version, false);
        if (s.ValidatedAt is null) throw new AppException(422, "La validation finale est obligatoire avant export.");
        var template = await templates.Get(input.TemplateId, ct);
        if (!template.IsActive) throw new AppException(422, "Modèle désactivé.");
        StoredFile? stored = null;
        try
        {
            var bytes = exporter.Export(s, template); stored = await files.SaveAsync(bytes, ct);
            var export = new StatementExport { ExportTemplateId = template.Id, CreatedBy = actor.Id, FileName = $"{template.Type}_{s.Id:N}_{DateTime.UtcNow:yyyyMMddHHmmss}.{(template.Type == "SAGE100_STANDARD" ? "txt" : "csv")}", StorageKey = stored.Key, FileHash = stored.Hash, FileSize = stored.Size, TemplateSnapshot = JsonSerializer.Serialize(template) };
            s.Exports.Add(export); s.Status = s.ArchivedAt.HasValue ? "ARCHIVED" : "EXPORTED";
            StatementService.History(s, actor.Id, "EXPORT_COMPLETED", $"Export {template.Name} conservé.");
            await db.SaveChangesAsync(ct); return export.Id;
        }
        catch (Exception error)
        {
            if (stored is not null) await files.DeleteAsync(stored.Key, CancellationToken.None);
            if (error is AppException && !ct.IsCancellationRequested)
            {
                db.ChangeTracker.Clear();
                var current = await statements.Load(id, actor, ct);
                if (current.Version == input.Version)
                {
                    current.Status = current.ArchivedAt.HasValue ? "ARCHIVED" : "EXPORT_FAILED";
                    current.Message = error.Message;
                    StatementService.History(current, actor.Id, "EXPORT_FAILED", error.Message);
                    try { await db.SaveChangesAsync(ct); }
                    catch (DbUpdateConcurrencyException) { /* A newer user action takes precedence. */ }
                }
            }
            throw;
        }
    }
    public async Task<DownloadFile> Download(Guid exportId, StatementActor actor, CancellationToken ct)
    {
        var export = await db.StatementExports.AsNoTracking().SingleOrDefaultAsync(e => e.Id == exportId, ct) ?? throw new AppException(404, "Export introuvable.");
        if (!await statements.Visible(actor).AnyAsync(s => s.Id == export.BankStatementId, ct)) throw new AppException(404, "Export introuvable ou non autorisé.");
        return new(await files.ReadAsync(export.StorageKey, ct), export.FileName, "application/octet-stream");
    }
}
