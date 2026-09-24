using System.Diagnostics;
using System.Globalization;
using System.Text;
using BankStatementConverter.Application;
using Microsoft.Extensions.Configuration;
namespace BankStatementConverter.Infrastructure;

public sealed class TesseractOcrService(IConfiguration config) : IOcrService
{
    public async Task<OcrResult> ReadAsync(byte[] pdf, CancellationToken ct)
    {
        var renderer = config["Statements:PdfToPpm"];
        var tesseract = config["Statements:Tesseract"];
        if (string.IsNullOrWhiteSpace(renderer) || string.IsNullOrWhiteSpace(tesseract)) throw new AppException(422, "OCR non configuré. Configurez Statements:PdfToPpm et Statements:Tesseract, puis relancez le traitement.");
        var folder = Path.Combine(Path.GetTempPath(), "releveflow-ocr-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var source = Path.Combine(folder, "source.pdf");
            await File.WriteAllBytesAsync(source, pdf, ct);
            await Run(renderer, ["-r", "150", "-scale-to", "2400", "-png", source, Path.Combine(folder, "page")], ct);
            var text = new StringBuilder(); var confidence = new List<decimal>();
            foreach (var page in Directory.GetFiles(folder, "page-*.png").OrderBy(p => p.Length).ThenBy(p => p))
            {
                var tsv = await Run(tesseract, [page, "stdout", "-l", config["Statements:OcrLanguage"] ?? "fra+eng", "tsv"], ct);
                string? lineId = null;
                foreach (var line in tsv.Split('\n').Skip(1))
                {
                    var c = line.TrimEnd('\r').Split('\t');
                    if (c.Length < 12 || c[0] != "5" || string.IsNullOrWhiteSpace(c[11])) continue;
                    var id = string.Join('-', c.Take(5));
                    if (id != lineId) text.AppendLine(); else text.Append(' ');
                    lineId = id; text.Append(c[11]);
                    if (decimal.TryParse(c[10], CultureInfo.InvariantCulture, out var score) && score >= 0) confidence.Add(score);
                }
                text.AppendLine();
                if (text.Length > 2_000_000) throw new AppException(422, "Texte OCR trop volumineux.");
            }
            return new(text.ToString(), confidence.Count == 0 ? null : confidence.Average());
        }
        finally { Directory.Delete(folder, true); }
    }
    private static async Task<string> Run(string executable, string[] arguments, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromMinutes(3));
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = start };
        try
        {
            process.Start();
            var output = process.StandardOutput.ReadToEndAsync(timeout.Token); var error = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            await error;
            if (process.ExitCode != 0) throw new AppException(422, "Le moteur OCR a échoué. Vérifiez les exécutables et les langues installées.");
            return await output;
        }
        catch (OperationCanceledException) { if (!process.HasExited) process.Kill(true); throw; }
        catch (System.ComponentModel.Win32Exception) { throw new AppException(422, "Exécutable OCR introuvable ou inaccessible."); }
    }
}
