using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using BankStatementConverter.Application;
using BankStatementConverter.Domain;
using Microsoft.Extensions.Configuration;
using UglyToad.PdfPig;
namespace BankStatementConverter.Infrastructure;

public class BmceOcrService(IConfiguration config)
{
    public async Task<string> ExtractAsync(Stream stream,CancellationToken ct)
    {
        using(var probe=PdfDocument.Open(stream)){
            Validation.Require(probe.NumberOfPages<=200,"Maximum 200 pages par PDF.");
            var pages=probe.GetPages().Select(p=>UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor.ContentOrderTextExtractor.GetText(p)).ToList();
            if(pages.All(p=>p.Count(char.IsLetterOrDigit)>20))return string.Join("\n",pages);
            Validation.Require(pages.All(p=>p.Count(char.IsLetterOrDigit)<=20),"PDF mixte texte/scan : séparez les relevés ou utilisez un profil adapté.");
        }
        stream.Position=0;
        var script=Path.GetFullPath(config["Ocr:Script"]??Path.Combine(AppContext.BaseDirectory,"ocr","bmce.cjs"));
        if(!File.Exists(script))throw new AppException(400,"Moteur OCR BMCE absent. Configurez Ocr:Script et installez backend/ocr.");
        var directory=Path.Combine(Path.GetTempPath(),"releveflow-ocr",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try {
            using var doc=PdfDocument.Open(stream);Validation.Require(doc.NumberOfPages<=20,"OCR BMCE limité à 20 pages.");
            var start=new ProcessStartInfo(config["Ocr:Node"]??"node"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};start.ArgumentList.Add(script);
            var index=0;
            foreach(var page in doc.GetPages()){
                var images=page.GetImages().Where(i=>i.WidthInSamples>=300&&i.HeightInSamples>=150).OrderByDescending(i=>i.Bounds.Top).ToList();
                Validation.Require(images.Count>0,"Le profil BMCE scanné nécessite des tableaux en images sur chaque page.");
                foreach(var image in images){Validation.Require(++index<=40&&((long)image.WidthInSamples*image.HeightInSamples)<=30_000_000,"Image trop volumineuse pour le profil OCR.");if(!image.TryGetPng(out var png))throw new AppException(400,"Image PDF non prise en charge.");var file=Path.Combine(directory,$"{index:D3}.png");await File.WriteAllBytesAsync(file,png,ct);start.ArgumentList.Add(file);}
            }
            using var process=Process.Start(start)??throw new AppException(500,"Impossible de démarrer l’OCR local.");
            using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromMinutes(3));
            var output=process.StandardOutput.ReadToEndAsync(ct);var error=process.StandardError.ReadToEndAsync(ct);
            try{await process.WaitForExitAsync(timeout.Token);}catch{if(!process.HasExited)process.Kill(true);await process.WaitForExitAsync(CancellationToken.None);throw;}
            var text=await output;var diagnostic=await error;
            if(process.ExitCode!=0)throw new AppException(400,"OCR BMCE indisponible ou image illisible. Vérifiez Node.js, les dépendances et tessdata.");
            Validation.Require(text.Length<=2_000_000,"Résultat OCR trop volumineux.");return text;
        }finally{Directory.Delete(directory,true);}
    }
}
public class BmceStatementParser : IBankStatementParser
{
    public string Key=>"bmce-scan";
    private static string Normalize(string text)=>Regex.Replace(text.ToUpperInvariant(),@"[^A-Z0-9]","");
    private static decimal? Money(string cell){var matches=Regex.Matches(cell,@"\d[\d., ]*[.,]\d{2}");Validation.Require(matches.Count<=1,"Plusieurs montants détectés dans une cellule OCR.");return matches.Count==0?null:AmountNormalizer.Parse(matches[0].Value,"fr-FR");}
    public IReadOnlyList<BankTransaction> Parse(string text,BankStatementTemplate template)
    {
        if(!text.TrimStart().StartsWith('['))return new BmceTextParser().Parse(text,template);
        using var doc=JsonDocument.Parse(text);var result=new List<BankTransaction>();decimal? opening=null,closing=null;
        foreach(var image in doc.RootElement.EnumerateArray()){
            Validation.Require(!image.TryGetProperty("error",out _),"Grille du relevé non reconnue. Aucun export partiel généré.");
            var chunk=new List<BankTransaction>();
            foreach(var row in image.GetProperty("rows").EnumerateArray()){
                var cells=row.EnumerateArray().Select(x=>x.GetString()??"").ToArray();Validation.Require(cells.Length==5,"Structure BMCE incorrecte.");
                var joined=Normalize(string.Join("",cells.Take(3)));
                if(joined.Contains("SOLDE")||joined.Contains("CREDITEUR")){
                    var balance=Money(cells[4])??-(Money(cells[3])??throw new AppException(400,"Solde OCR illisible."));
                    if(opening==null)opening=balance;else if(result.Count>0||chunk.Count>0)closing=balance;continue;
                }
                if(cells[0].Contains("Date",StringComparison.OrdinalIgnoreCase))continue;
                var dateText=Regex.Match(cells[0],@"\d{2}/\d{2}/\d{4}").Value;
                Validation.Require(DateOnly.TryParseExact(dateText,"dd/MM/yyyy",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date),"Date OCR illisible. Aucun export partiel généré.");
                Validation.Require(DateOnly.TryParseExact(cells[1].Trim(),"dd/MM/yyyy",CultureInfo.InvariantCulture,DateTimeStyles.None,out var valueDate),"Date de valeur OCR illisible.");
                var debit=Money(cells[3])??0;var credit=Money(cells[4])??0;
                Validation.Require((debit>0) != (credit>0),"Débit/crédit OCR ambigu.");
                var origin=Regex.Match(cells[2],@"(?is)Origine.*?(-?\s*\d[\d., ]*[.,]\d{2})\s*MAD");
                if(origin.Success){var raw=origin.Groups[1].Value;var digits=Regex.Replace(raw,@"[^0-9]","");var originAmount=decimal.Parse(digits,CultureInfo.InvariantCulture)/100*(raw.TrimStart().StartsWith('-')?-1:1);Validation.Require(originAmount==credit-debit,"Montant de la colonne diff�rent du montant Origine : v�rifiez le scan.");}
                var description=Regex.Split(cells[2],@"(?i)Origine")[0].Trim().Trim('|').Trim();Validation.Text(description,"Libellé OCR",1000);
                chunk.Add(new(){Date=date,ValueDate=valueDate,Description=description,Debit=debit,Credit=credit});
            }
            // Only remove an exact contiguous overlap at an image boundary, never global duplicates.
            int overlap=0;for(int count=Math.Min(result.Count,chunk.Count);count>=2;count--){if(result.TakeLast(count).Zip(chunk.Take(count)).All(pair=>pair.First.Date==pair.Second.Date&&pair.First.ValueDate==pair.Second.ValueDate&&pair.First.Debit==pair.Second.Debit&&pair.First.Credit==pair.Second.Credit&&Normalize(pair.First.Description)==Normalize(pair.Second.Description))){overlap=count;break;}}
            result.AddRange(chunk.Skip(overlap));
        }
        Validation.Require(result.Count>0&&opening!=null&&closing!=null,"Solde initial ou final absent : vérification impossible.");
        Validation.Require(opening+result.Sum(x=>x.Credit-x.Debit)==closing,"Écart de rapprochement OCR : solde initial + crédits − débits différent du solde final. Aucun export généré.");
        decimal running=opening!.Value;foreach(var transaction in result){running+=transaction.Credit-transaction.Debit;transaction.Balance=running;}
        return result;
    }
}
