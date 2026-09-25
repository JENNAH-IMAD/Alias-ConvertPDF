using System.Text;
using BankStatementConverter.Application;
using BankStatementConverter.Domain;
using BankStatementConverter.Infrastructure;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Core;
using Xunit;
namespace BankStatementConverter.Tests;

public class StatementTests
{
    public static byte[] Pdf(string text = "TEST BANK 2026 statement sample for validation")
    {
        var builder = new PdfDocumentBuilder(); var page = builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica); page.AddText(text, 12, new PdfPoint(40, 700), font); return builder.Build();
    }
    public static BankStatement Statement() => new() {
        BankAccount = new() { AccountCode = "512000", Journal = "BQ", Bank = new() { Name = "Test bank" }, Client = new() { Name = "Test client" } }, Currency = "MAD", PeriodStart = new(2026, 9, 1), PeriodEnd = new(2026, 9, 30), OpeningBalance = 100, ClosingBalance = 125,
        Transactions = [new() { Position = 1, TransactionDate = new(2026, 9, 2), Description = "Virement été", Credit = 25, Currency = "MAD" }]
    };
    [Fact] public void PdfAnalysisRejectsInvalidAndExtractsText()
    {
        var analyzer = new PdfAnalyzer(); Assert.Throws<AppException>(() => analyzer.Analyze([])); Assert.Throws<AppException>(() => analyzer.Analyze("%PDF-broken"u8.ToArray()));
        var result = analyzer.Analyze(Pdf()); Assert.Equal("TEXT_PDF", result.Type); Assert.Equal(1, result.PageCount); Assert.Contains("TEST BANK", result.Pages[0]);
    }
    [Fact] public void BalancesDatesAndAmountsMustBeValid()
    {
        var s = Statement(); var validator = new StatementValidator(); Assert.Empty(validator.Check(s).Issues);
        s.ClosingBalance = 999; Assert.Contains(validator.Check(s).Issues, i => i.Code == "BALANCE_MISMATCH");
        s.Transactions.First().Debit = 10; Assert.Contains(validator.Check(s).Issues, i => i.Code == "AMOUNT");
        s.Transactions.First().TransactionDate = null; Assert.Contains(validator.Check(s).Issues, i => i.Code == "DATE");
    }
    [Fact] public void ProfileNormalizesOnlyMatchingRows()
    {
        var p = new BankStatementProfile { TransactionPattern = @"^(?<date>\d{2}/\d{2}/\d{4});(?<description>[^;]+);(?<credit>[^;]+)$" };
        var rows = new ProfileTransactionExtractor().Extract("header\n02/09/2026;Virement;1 250,50\ninvalid line", p, "MAD", 90);
        Assert.Single(rows); Assert.Equal(1250.50m, rows[0].Credit); Assert.Equal(new DateOnly(2026, 9, 2), rows[0].TransactionDate);
        Assert.Null(ProfileTransactionExtractor.Amount("ambiguous", p));
    }
    [Fact] public void IntermediateBalancesAreCheckedEvenWhenFinalBalanceMatches()
    {
        var s = Statement(); s.Transactions.First().Balance = 120;
        var validator = new StatementValidator();
        Assert.Contains(validator.Check(s).Issues, i => i.Code == "ROW_BALANCE_MISMATCH" && i.Row == 1);
        s.Transactions.First().Balance = 125;
        Assert.Empty(validator.Check(s).Issues);
    }
    [Theory]
    [InlineData("UTF-8", "SAGE_X3")]
    [InlineData("Windows-1252", "SAGE100_STANDARD")]
    public void ExportsUseConfiguredEncodingColumnsAndEscapeText(string encoding, string type)
    {
        var s = Statement(); s.Transactions.First().Description = "=FORMULE;\"été\"";
        var template = new ExportTemplate { Encoding = encoding, Type = type, Fields = [new(){SourceField="Description",OutputField="Libellé",Position=0},new(){SourceField="Amount",OutputField="Montant",Position=1},new(){SourceField="Direction",OutputField="Sens",Position=2}] };
        var bytes = new DelimitedStatementExporter().Export(s, template);
        var result = Encoding.GetEncoding(encoding).GetString(bytes);
        Assert.Contains("\"'=FORMULE;\"\"été\"\"\";25,00;C", result); Assert.Contains("Libellé;Montant;Sens", result);
    }
    [Fact] public void StaleVersionAndArchivedEditsAreRejected()
    {
        var s = Statement(); Assert.Equal(409, Assert.Throws<AppException>(()=>StatementService.Guard(s,Guid.NewGuid())).Status);
        s.ArchivedAt = DateTime.UtcNow; Assert.Throws<AppException>(()=>StatementService.Guard(s,s.Version));
        StatementService.Guard(s,s.Version,false);
    }
}
