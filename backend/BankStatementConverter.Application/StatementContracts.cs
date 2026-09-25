using BankStatementConverter.Domain;
namespace BankStatementConverter.Application;

public record StatementActor(Guid Id, bool Admin);
public record StatementRow(Guid Id, Guid BankAccountId, Guid ClientId, string Client, string Bank, string Account, string OriginalFileName, DateOnly? PeriodStart, DateOnly? PeriodEnd, string Currency, string Status, int TransactionCount, int ExportCount, DateTime CreatedAt, DateTime? ArchivedAt, Guid Version);
public record TransactionInput(DateOnly? TransactionDate, DateOnly? ValueDate, string Reference, string Description, decimal? Debit, decimal? Credit, decimal? Balance, Guid? Id = null);
public record ReviewInput(Guid Version, DateOnly? PeriodStart, DateOnly? PeriodEnd, decimal? OpeningBalance, decimal? ClosingBalance, List<TransactionInput> Transactions);
public record VersionInput(Guid Version);
public record ProcessInput(Guid Version, Guid? ProfileId);
public record ExportInput(Guid Version, Guid TemplateId);
public record ValidationIssue(string Code, string Message, int? Row = null);
public record StatementCheck(decimal TotalDebit, decimal TotalCredit, decimal? CalculatedBalance, decimal? Difference, IReadOnlyList<ValidationIssue> Issues);
public record StoredFile(string Key, string Hash, long Size);
public record DownloadFile(byte[] Bytes, string Name, string MimeType);
public record PdfAnalysis(int PageCount, string Type, IReadOnlyList<string> Pages);
public record OcrResult(string Text, decimal? Confidence);
public record TemplateFieldInput(string SourceField, string OutputField, int Position, bool Required, string DefaultValue);
public record TemplateInput(string Name, string Code, string Type, string Encoding, string Delimiter, string DateFormat, string DecimalSeparator, int Decimals, bool IncludeHeader, bool IsActive, Guid? Version, List<TemplateFieldInput> Fields);
public record ProfileInput(Guid BankId, string Name, string Code, int Version, bool IsActive, string DetectionText, string TransactionPattern, string DateFormat, string DecimalSeparator, string ThousandsSeparator, bool OcrRequired);
public interface IFileStorageService
{
    Task<StoredFile> SaveAsync(byte[] bytes, CancellationToken ct);
    Task<byte[]> ReadAsync(string key, CancellationToken ct);
    Task DeleteAsync(string key, CancellationToken ct);
}
public interface IPdfAnalyzer { PdfAnalysis Analyze(byte[] bytes); }
public interface IOcrService { Task<OcrResult> ReadAsync(byte[] pdf, CancellationToken ct); }
public interface IStatementValidator { StatementCheck Check(BankStatement statement); }
public interface IStatementExporter { byte[] Export(BankStatement statement, ExportTemplate template, int? limit = null); }
