using System.Globalization;
using System.Text;
using System.Text.Json;
using BankStatementConverter.Domain;
using BankStatementConverter.Infrastructure;
using BankStatementConverter.Application;
using Microsoft.Extensions.Configuration;
using CsvHelper;
using CsvHelper.Configuration;
using Xunit;
namespace BankStatementConverter.Tests;
public class BmceTests
{
    [Fact]
    public void OverlapPreservesRepeatedFeesAndRejectsMismatch()
    {
        string[] fee=["01/11/2011","01/11/2011","FRAIS\nOrigine : -7,70 MAD","7,70",""];
        string[] a=["01/11/2011","01/11/2011","ACHAT A","10,00",""];
        string[] b=["02/11/2011","02/11/2011","ACHAT B","20,00",""];
        var images=new[]{new{rows=new[]{new[]{"","SOLDE CREDITEUR","","","100,00"},a,b}},new{rows=new[]{a,b,fee,fee,fee,new[]{"Réf","SOLDE CREDITEUR","","","46,90"}}}};
        var json=JsonSerializer.Serialize(images);var rows=new BmceStatementParser().Parse(json,new());Assert.Equal(5,rows.Count);Assert.Equal(3,rows.Count(x=>x.Description=="FRAIS"));Assert.Equal(46.90m,rows.Last().Balance);
        Assert.Throws<AppException>(()=>new BmceStatementParser().Parse(json.Replace("46,90","46,91"),new()));
        Assert.Throws<AppException>(()=>new BmceStatementParser().Parse(json.Replace("01/11/2011","99/11/2011"),new()));
    }
    [Fact]
    [Trait("Category","LocalPdf")]
    public async Task SuppliedBmcePdfReconcilesAndExportsAllFormats()
    {
        var pdf=Environment.GetEnvironmentVariable("BMCE_TEST_PDF");
        if(string.IsNullOrEmpty(pdf))return; // Private sample is never committed to the repository.
        var script=Environment.GetEnvironmentVariable("BMCE_OCR_SCRIPT")!;
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Ocr:Script",script}}).Build();
        using var stream=File.OpenRead(pdf);var text=await new BmceOcrService(config).ExtractAsync(stream,default);var rows=new BmceStatementParser().Parse(text,new());
        Assert.Equal(45,rows.Count);Assert.Equal(39227.31m,rows.Sum(x=>x.Debit));Assert.Equal(31542m,rows.Sum(x=>x.Credit));Assert.Equal(21597.36m,rows.Last().Balance);
        Assert.Equal(3,rows.Count(x=>x.Description=="FRAIS PDL TTC"));Assert.All(rows,x=>Assert.NotNull(x.ValueDate));
        foreach(var row in rows){row.Account="512000";row.Journal="BQ";}
        foreach(ITransactionExporter exporter in new ITransactionExporter[]{new Sage100Exporter(),new SageX3Exporter(),new CustomCsvExporter()}){
            var template=Seed.DefaultExport(exporter.Type);var bytes=exporter.Export(rows,template);var encoding=Encoding.GetEncoding(template.Encoding);
            using var reader=new StringReader(encoding.GetString(bytes));using var csv=new CsvReader(reader,new CsvConfiguration(CultureInfo.GetCultureInfo("fr-FR")){WhiteSpaceChars=[],Delimiter=template.Delimiter});
            Assert.True(csv.Read());csv.ReadHeader();Assert.Equal(template.Fields.Count,csv.HeaderRecord!.Length);int count=0;
            while(csv.Read()){Assert.Equal(template.Fields.Count,csv.Parser.Count);if(exporter.Type=="SAGE100"){Assert.True(csv.GetField<decimal>("Montant")>0);Assert.Contains(csv.GetField("Sens"),new[]{"D","C"});}count++;}Assert.Equal(45,count);
            if(exporter.Type=="SAGE100")Assert.Contains((byte)0xE9,bytes);
        }
    }
}
