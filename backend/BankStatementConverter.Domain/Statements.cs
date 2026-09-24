namespace BankStatementConverter.Domain;

public class BankStatement : Entity
{
    public Guid BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = null!;
    public Guid CreatedBy { get; set; }
    public string OriginalFileName { get; set; } = "";
    public string StorageKey { get; set; } = "";
    public string FileHash { get; set; } = "";
    public long FileSize { get; set; }
    public int PageCount { get; set; }
    public string PdfType { get; set; } = "";
    public string Status { get; set; } = "UPLOADED";
    public string Message { get; set; } = "";
    public string RawText { get; set; } = "";
    public string Currency { get; set; } = "";
    public DateOnly? PeriodStart { get; set; }
    public DateOnly? PeriodEnd { get; set; }
    public decimal? OpeningBalance { get; set; }
    public decimal? ClosingBalance { get; set; }
    public Guid? ProfileId { get; set; }
    public Guid? ValidatedBy { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public ICollection<BankTransaction> Transactions { get; set; } = [];
    public ICollection<StatementExport> Exports { get; set; } = [];
    public ICollection<ProcessingHistory> History { get; set; } = [];
}
public class BankTransaction : Entity
{
    public Guid BankStatementId { get; set; }
    public int Position { get; set; }
    public DateOnly? TransactionDate { get; set; }
    public DateOnly? ValueDate { get; set; }
    public string Reference { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal? Debit { get; set; }
    public decimal? Credit { get; set; }
    public decimal? Balance { get; set; }
    public string Currency { get; set; } = "";
    public string RawText { get; set; } = "";
    public decimal? ConfidenceScore { get; set; }
    public bool IsValidated { get; set; }
}
public class BankStatementProfile : Entity
{
    public Guid BankId { get; set; }
    public Bank Bank { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; }
    public string DetectionText { get; set; } = "";
    public string TransactionPattern { get; set; } = "";
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string DecimalSeparator { get; set; } = ",";
    public string ThousandsSeparator { get; set; } = " ";
    public bool OcrRequired { get; set; }
}
public class ExportTemplate : Entity
{
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string Type { get; set; } = "CUSTOM_CSV";
    public string Encoding { get; set; } = "UTF-8";
    public string Delimiter { get; set; } = ";";
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string DecimalSeparator { get; set; } = ",";
    public int Decimals { get; set; } = 2;
    public bool IncludeHeader { get; set; } = true;
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid Version { get; set; } = Guid.NewGuid();
    public ICollection<ExportTemplateField> Fields { get; set; } = [];
}
public class ExportTemplateField : Entity
{
    public Guid ExportTemplateId { get; set; }
    public string SourceField { get; set; } = "";
    public string OutputField { get; set; } = "";
    public int Position { get; set; }
    public bool Required { get; set; }
    public string DefaultValue { get; set; } = "";
}
public class StatementExport : Entity
{
    public Guid BankStatementId { get; set; }
    public Guid ExportTemplateId { get; set; }
    public ExportTemplate ExportTemplate { get; set; } = null!;
    public Guid CreatedBy { get; set; }
    public string FileName { get; set; } = "";
    public string StorageKey { get; set; } = "";
    public string FileHash { get; set; } = "";
    public long FileSize { get; set; }
    public string TemplateSnapshot { get; set; } = "";
    public string Status { get; set; } = "EXPORTED";
}
public class ProcessingHistory : Entity
{
    public Guid BankStatementId { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; } = "";
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
}
