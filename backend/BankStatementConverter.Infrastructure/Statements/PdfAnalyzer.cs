using BankStatementConverter.Application;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
namespace BankStatementConverter.Infrastructure;

public sealed class PdfAnalyzer : IPdfAnalyzer
{
    public PdfAnalysis Analyze(byte[] bytes)
    {
        if (bytes.Length < 5 || !bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8)) throw new AppException(422, "Signature PDF invalide.");
        try
        {
            using var pdf = PdfDocument.Open(bytes);
            if (pdf.NumberOfPages is < 1 or > 100) throw new AppException(422, "Le PDF doit contenir entre 1 et 100 pages.");
            var pages = pdf.GetPages().Select(p => ContentOrderTextExtractor.GetText(p)).ToArray();
            if (pages.Sum(p => p.Length) > 2_000_000) throw new AppException(422, "Le texte du PDF dépasse la limite de traitement.");
            var textPages = pages.Count(p => p.Count(char.IsLetterOrDigit) >= 20);
            return new(pdf.NumberOfPages, textPages == 0 ? "SCANNED_PDF" : textPages == pages.Length ? "TEXT_PDF" : "MIXED_PDF", pages);
        }
        catch (AppException) { throw; }
        catch (Exception e) when (e is not OutOfMemoryException) { throw new AppException(422, "PDF corrompu ou protégé. Fournissez un PDF lisible sans mot de passe."); }
    }
}

