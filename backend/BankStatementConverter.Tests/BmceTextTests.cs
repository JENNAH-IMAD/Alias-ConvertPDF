using BankStatementConverter.Infrastructure;
using BankStatementConverter.Application;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace BankStatementConverter.Tests;
public class BmceTextTests
{
 [Fact] public void TextTablePreservesGroupingAndRepeatedHeaders(){var text="BMCE\nDate Libellé Débit (MAD) Crédit (MAD)\n02/09/2026 CLIENT 2026 0,00 125 000,00\nDate Libellé Débit (MAD) Crédit (MAD)\n03/09/2026 ACHAT 18 500,00 0,00";var rows=new BmceAutomaticParser().Parse(text,new());Assert.Equal(2,rows.Count);Assert.Equal(125000m,rows[0].Credit);Assert.Equal("CLIENT 2026",rows[0].Description);Assert.Null(rows[0].Balance);}
 [Theory][InlineData("31/02/2026 ACHAT 10,00 0,00")][InlineData("01/02/2026 ACHAT 10,00 20,00")][InlineData("01/02/2026 ACHAT illisible 0,00")]
 public void InvalidRowsDoNotProducePartialExport(string row)=>Assert.Throws<AppException>(()=>new BmceTextParser().Parse("Date Libellé Débit Crédit\n"+row,new()));
 [Fact][Trait("Category","LocalPdf")]
 public async Task SuppliedTextPdfUsesNoOcrAndExports(){var path=Environment.GetEnvironmentVariable("BMCE_TEXT_TEST_PDF");if(string.IsNullOrEmpty(path))return;using var stream=File.OpenRead(path);var text=await new BmceOcrService(new ConfigurationBuilder().Build()).ExtractAsync(stream,default);var rows=new BmceAutomaticParser().Parse(text,new());Assert.Equal(48,rows.Count);Assert.Equal(6520900m,rows.Sum(x=>x.Debit));Assert.Equal(4225000m,rows.Sum(x=>x.Credit));Assert.All(rows,x=>Assert.Null(x.Balance));foreach(ITransactionExporter exporter in new ITransactionExporter[]{new Sage100Exporter(),new SageX3Exporter(),new CustomCsvExporter()})Assert.NotEmpty(exporter.Export(rows,Seed.DefaultExport(exporter.Type)));}
}
