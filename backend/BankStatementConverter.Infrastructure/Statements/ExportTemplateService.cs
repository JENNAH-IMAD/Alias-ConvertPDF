using BankStatementConverter.Application;
using BankStatementConverter.Domain;
using Microsoft.EntityFrameworkCore;
namespace BankStatementConverter.Infrastructure;

public sealed class ExportTemplateService(AppDbContext db)
{
    public Task<List<ExportTemplate>> List(CancellationToken ct) => db.ExportTemplates.AsNoTracking().Include(t => t.Fields).OrderBy(t => t.Name).ToListAsync(ct);
    public async Task<ExportTemplate> Get(Guid id, CancellationToken ct) => await db.ExportTemplates.Include(t => t.Fields).SingleOrDefaultAsync(t => t.Id == id, ct) ?? throw new AppException(404, "Modèle introuvable.");
    public async Task<ExportTemplate> Save(Guid? id, TemplateInput input, CancellationToken ct)
    {
        Validation.Text(input.Name, "Nom", 100); Validation.Text(input.Code, "Code", 50);
        Validation.Require(input.Type is "CUSTOM_CSV" or "SAGE100_STANDARD" or "SAGE_X3", "Type de modèle invalide.");
        Validation.Require(input.Encoding is "UTF-8" or "Windows-1252", "Encodage invalide.");
        Validation.Require(input.Delimiter is ";" or "," or "\t" or "|", "Séparateur invalide.");
        Validation.Require(DelimitedStatementExporter.DateFormats.Contains(input.DateFormat) && input.DecimalSeparator is "." or "," && input.Decimals is >= 0 and <= 4, "Format date ou montant invalide.");
        Validation.Require(input.Fields is { Count: > 0 and <= 30 }, "Choisissez entre 1 et 30 colonnes.");
        foreach (var field in input.Fields)
        {
            Validation.Require(DelimitedStatementExporter.Sources.Contains(field.SourceField), "Colonne source inconnue.");
            Validation.Text(field.OutputField, "Nom de colonne", 100);
            Validation.Require(field.DefaultValue is not null && field.DefaultValue.Length <= 200, "Valeur par défaut trop longue.");
        }
        var template = id.HasValue ? await Get(id.Value, ct) : new ExportTemplate();
        if (id.HasValue && template.Version != input.Version) throw new AppException(409, "Ce modèle a changé. Actualisez avant de modifier.");
        db.ExportTemplateFields.RemoveRange(template.Fields); template.Fields.Clear();
        template.Name = input.Name.Trim(); template.Code = input.Code.Trim().ToUpperInvariant(); template.Type = input.Type;
        template.Encoding = input.Encoding; template.Delimiter = input.Delimiter; template.DateFormat = input.DateFormat;
        template.DecimalSeparator = input.DecimalSeparator; template.Decimals = input.Decimals; template.IncludeHeader = input.IncludeHeader; template.IsActive = input.IsActive; template.Version = Guid.NewGuid();
        foreach (var (field, index) in input.Fields.OrderBy(f => f.Position).Select((f, i) => (f, i))) template.Fields.Add(new() { SourceField = field.SourceField, OutputField = field.OutputField, DefaultValue = field.DefaultValue, Required = field.Required, Position = index });
        if (!id.HasValue) db.ExportTemplates.Add(template);
        await db.SaveChangesAsync(ct); return template;
    }
    public async Task Delete(Guid id, CancellationToken ct)
    {
        var template = await Get(id, ct);
        if (await db.StatementExports.AnyAsync(e => e.ExportTemplateId == id, ct)) throw new AppException(409, "Ce modèle possède des exports archivés. Désactivez-le pour préserver l'historique.");
        db.ExportTemplateFields.RemoveRange(template.Fields); db.ExportTemplates.Remove(template); await db.SaveChangesAsync(ct);
    }
    public async Task CreatePreset(string type, CancellationToken ct)
    {
        var fields = type switch {
            "SAGE100_STANDARD" => new[] { "Date", "Account", "Description", "Amount", "Direction", "Analytic" },
            "SAGE_X3" => new[] { "Date", "Journal", "Account", "Description", "Debit", "Credit", "Reference" },
            "CUSTOM_CSV" => new[] { "Date", "Description", "Debit", "Credit", "Balance" },
            _ => throw new AppException(400, "Type inconnu.")
        };
        if (await db.ExportTemplates.AnyAsync(t => t.Code == type, ct)) return;
        var labels = new Dictionary<string, string> { ["Date"] = "Date", ["Account"] = "Compte", ["Description"] = "Libellé", ["Amount"] = "Montant", ["Direction"] = "Sens", ["Analytic"] = "Analytique", ["Journal"] = "Journal", ["Debit"] = "Débit", ["Credit"] = "Crédit", ["Reference"] = "Référence", ["Balance"] = "Solde" };
        await Save(null, new(type == "SAGE100_STANDARD" ? "Sage 100 – Standard" : type == "SAGE_X3" ? "Sage X3 – Import Banque" : "CSV personnalisé", type, type, type == "SAGE100_STANDARD" ? "Windows-1252" : "UTF-8", type == "CUSTOM_CSV" ? "," : ";", "dd/MM/yyyy", ",", 2, true, true, null,
            fields.Select((f, i) => new TemplateFieldInput(f, labels[f], i, f is "Date" or "Description" or "Account", "")).ToList()), ct);
    }
}
