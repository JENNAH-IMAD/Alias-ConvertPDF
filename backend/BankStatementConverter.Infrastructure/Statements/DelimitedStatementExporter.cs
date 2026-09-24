using System.Globalization;
using System.Text;
using BankStatementConverter.Application;
using BankStatementConverter.Domain;
namespace BankStatementConverter.Infrastructure;

public sealed class DelimitedStatementExporter : IStatementExporter
{
    public static readonly string[] Sources = ["Date", "ValueDate", "Reference", "Description", "Debit", "Credit", "Amount", "Balance", "Account", "Bank", "Client", "Currency", "Journal", "Direction", "Analytic"];
    public static readonly string[] DateFormats = ["dd/MM/yyyy", "yyyy-MM-dd", "ddMMyyyy", "dd/MM/yy"];
    public byte[] Export(BankStatement statement, ExportTemplate template, int? limit = null)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = template.Encoding == "Windows-1252" ? Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback) : new UTF8Encoding(false, true);
        var fields = template.Fields.OrderBy(f => f.Position).ToArray();
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone(); culture.NumberFormat.NumberDecimalSeparator = template.DecimalSeparator;
        string Number(decimal? value) => value?.ToString("F" + template.Decimals, culture) ?? "";
        string Cell(string value)
        {
            // Prevent spreadsheet formula execution in textual cells.
            if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('@') || value.TrimStart().StartsWith('-') && !decimal.TryParse(value, NumberStyles.Number, culture, out _)) value = "'" + value;
            return value.Contains(template.Delimiter) || value.Contains('"') || value.Contains('\n') || value.Contains('\r') ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }
        var text = new StringBuilder();
        if (template.IncludeHeader) text.AppendLine(string.Join(template.Delimiter, fields.Select(f => Cell(f.OutputField))));
        foreach (var t in statement.Transactions.OrderBy(t => t.Position).Take(limit ?? int.MaxValue))
        {
            var values = fields.Select(f => {
                var value = f.SourceField switch {
                    "Date" => t.TransactionDate?.ToString(template.DateFormat), "ValueDate" => t.ValueDate?.ToString(template.DateFormat),
                    "Reference" => t.Reference, "Description" => t.Description, "Debit" => Number(t.Debit), "Credit" => Number(t.Credit),
                    "Amount" => Number((t.Credit ?? 0) > 0 ? t.Credit : t.Debit), "Balance" => Number(t.Balance),
                    "Account" => statement.BankAccount.AccountCode, "Journal" => statement.BankAccount.Journal,
                    "Bank" => statement.BankAccount.Bank.Name, "Client" => statement.BankAccount.Client.Name, "Currency" => statement.Currency,
                    "Direction" => (t.Debit ?? 0) > 0 ? "D" : "C", "Analytic" => f.DefaultValue, _ => throw new AppException(422, "Colonne inconnue.")
                };
                if (string.IsNullOrEmpty(value)) value = f.DefaultValue;
                if (f.Required && string.IsNullOrWhiteSpace(value)) throw new AppException(422, $"Champ obligatoire absent : {f.OutputField}, ligne {t.Position}.");
                return Cell(value ?? "");
            });
            text.Append(string.Join(template.Delimiter, values)).Append("\r\n");
        }
        try { return encoding.GetBytes(text.ToString()); }
        catch (EncoderFallbackException) { throw new AppException(422, "Un caractère ne peut pas être encodé en Windows-1252. Choisissez UTF-8 ou corrigez le texte."); }
    }
}
