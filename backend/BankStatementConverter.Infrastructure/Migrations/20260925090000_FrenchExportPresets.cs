using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BankStatementConverter.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260925090000_FrenchExportPresets")]
public sealed class FrenchExportPresets : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Data-only migration: existing templates and catalog records are left intact.
        var presets = new[] {
            (Id: "ebdc1250-445f-4700-8d77-000000000001", Code: "SAGE100_STANDARD", Name: "Sage 100 – Standard", Encoding: "Windows-1252", Delimiter: ";", Fields: new[] { ("Date", "Date"), ("Account", "Compte"), ("Description", "Libellé"), ("Amount", "Montant"), ("Direction", "Sens"), ("Analytic", "Analytique") }),
            (Id: "ebdc1250-445f-4700-8d77-000000000002", Code: "SAGE_X3", Name: "Sage X3 – Import Banque", Encoding: "UTF-8", Delimiter: ";", Fields: new[] { ("Date", "Date"), ("Journal", "Journal"), ("Account", "Compte"), ("Description", "Libellé"), ("Debit", "Débit"), ("Credit", "Crédit"), ("Reference", "Référence") }),
            (Id: "ebdc1250-445f-4700-8d77-000000000003", Code: "CUSTOM_CSV", Name: "CSV personnalisé", Encoding: "UTF-8", Delimiter: ",", Fields: new[] { ("Date", "Date"), ("Description", "Libellé"), ("Debit", "Débit"), ("Credit", "Crédit"), ("Balance", "Solde") })
        };
        foreach (var p in presets)
        {
            migrationBuilder.Sql($$"""
                INSERT INTO "ExportTemplates" ("Id", "Name", "Code", "Type", "Encoding", "Delimiter", "DateFormat", "DecimalSeparator", "Decimals", "IncludeHeader", "IsSystem", "IsActive", "Version", "CreatedAt", "UpdatedAt")
                VALUES ('{{p.Id}}', '{{p.Name}}', '{{p.Code}}', '{{p.Code}}', '{{p.Encoding}}', '{{p.Delimiter}}', 'dd/MM/yyyy', ',', 2, true, false, true, '{{p.Id}}', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
                ON CONFLICT DO NOTHING;
                """);
            for (var i = 0; i < p.Fields.Length; i++)
            {
                var (source, label) = p.Fields[i];
                var fieldId = $"ebdc1250-445f-4700-8d77-{(int.Parse(p.Id[^1..]) * 100 + i):D12}";
                var required = source is "Date" or "Description" or "Account" ? "true" : "false";
                migrationBuilder.Sql($$"""
                    INSERT INTO "ExportTemplateFields" ("Id", "ExportTemplateId", "SourceField", "OutputField", "Position", "Required", "DefaultValue", "CreatedAt", "UpdatedAt")
                    SELECT '{{fieldId}}', '{{p.Id}}', '{{source}}', '{{label}}', {{i}}, {{required}}, '', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                    WHERE EXISTS (SELECT 1 FROM "ExportTemplates" WHERE "Id" = '{{p.Id}}')
                    ON CONFLICT DO NOTHING;
                    """);
            }
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Preserve user configuration and exports on rollback; no schema was added.
    }
}
