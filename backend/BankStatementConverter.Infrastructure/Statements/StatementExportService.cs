using System.Text.Json;
using BankStatementConverter.Application;
using BankStatementConverter.Domain;
using Microsoft.EntityFrameworkCore;
namespace BankStatementConverter.Infrastructure;

public sealed class StatementExportService(AppDbContext db, StatementService statements, ExportTemplateService templates, IStatementExporter exporter, IFileStorageService files)
{
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
        catch
        {
            if (stored is not null) await files.DeleteAsync(stored.Key, CancellationToken.None);
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
