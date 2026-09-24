using BankStatementConverter.Application;
using BankStatementConverter.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace BankStatementConverter.Infrastructure;

// A durable database queue. Atomic claims allow multiple instances without duplicate processing.
public sealed class StatementWorker(IServiceScopeFactory scopes, ILogger<StatementWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var stale = DateTime.UtcNow.AddMinutes(-20);
                await db.BankStatements.Where(s => (s.Status == "ANALYZING" || s.Status == "OCR_PROCESSING") && s.ProcessingStartedAt < stale)
                    .ExecuteUpdateAsync(u => u.SetProperty(s => s.Status, "EXTRACTION_FAILED").SetProperty(s => s.Message, "Traitement interrompu. Vous pouvez le relancer.").SetProperty(s => s.Version, Guid.NewGuid()), stoppingToken);
                var id = await db.BankStatements.Where(s => s.Status == "QUEUED").OrderBy(s => s.CreatedAt).Select(s => (Guid?)s.Id).FirstOrDefaultAsync(stoppingToken);
                if (id is null) { await Task.Delay(2000, stoppingToken); continue; }
                var claimed = await db.BankStatements.Where(s => s.Id == id && s.Status == "QUEUED").ExecuteUpdateAsync(u => u.SetProperty(s => s.Status, "ANALYZING").SetProperty(s => s.ProcessingStartedAt, DateTime.UtcNow).SetProperty(s => s.Version, Guid.NewGuid()), stoppingToken);
                if (claimed == 0) continue;
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                deadline.CancelAfter(TimeSpan.FromMinutes(10));
                await scope.ServiceProvider.GetRequiredService<PdfProcessingService>().Process(id.Value, deadline.Token);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError("File de traitement indisponible : {Type}", ex.GetType().Name); await Task.Delay(5000, stoppingToken); }
        }
    }
}

public sealed class PdfProcessingService(AppDbContext db, StatementService statements, IFileStorageService files, IPdfAnalyzer analyzer, IOcrService ocr, ProfileTransactionExtractor extractor, ILogger<PdfProcessingService> logger)
{
    public async Task Queue(Guid id, ProcessInput input, StatementActor actor, CancellationToken ct)
    {
        var s = await statements.Load(id, actor, ct); StatementService.Guard(s, input.Version);
        if (s.Transactions.Count > 0 || s.ValidatedAt is not null) throw new AppException(409, "Des opérations existent déjà. Utilisez la revue pour conserver vos corrections.");
        if (input.ProfileId.HasValue && !await db.BankStatementProfiles.AnyAsync(p => p.Id == input.ProfileId && p.IsActive && p.BankId == s.BankAccount.BankId, ct)) throw new AppException(422, "Profil inactif ou incompatible avec la banque du compte.");
        s.ProfileId = input.ProfileId; s.Status = "QUEUED"; s.Message = "Traitement en attente.";
        StatementService.History(s, actor.Id, "PROCESS_QUEUED", "Analyse demandée."); await db.SaveChangesAsync(ct);
    }
    public async Task Process(Guid id, CancellationToken ct)
    {
        var s = await statements.Load(id, new(Guid.Empty, true), ct);
        try
        {
            var bytes = await files.ReadAsync(s.StorageKey, ct); var analysis = analyzer.Analyze(bytes);
            s.PageCount = analysis.PageCount; s.PdfType = analysis.Type;
            StatementService.History(s, s.CreatedBy, "PDF_ANALYZED", $"{analysis.PageCount} pages ; {analysis.Type}.");
            var text = string.Join('\n', analysis.Pages); decimal? confidence = null;
            var profiles = await db.BankStatementProfiles.Where(p => p.BankId == s.BankAccount.BankId && p.IsActive).ToListAsync(ct);
            var profile = s.ProfileId.HasValue ? profiles.SingleOrDefault(p => p.Id == s.ProfileId) : null;
            if (analysis.Type != "TEXT_PDF" || profile?.OcrRequired == true)
            {
                s.Status = "OCR_PROCESSING"; StatementService.History(s, s.CreatedBy, "OCR_STARTED", "Lecture OCR du document."); await db.SaveChangesAsync(ct);
                var result = await ocr.ReadAsync(bytes, ct); text = result.Text; confidence = result.Confidence;
                StatementService.History(s, s.CreatedBy, "OCR_COMPLETED", "Lecture OCR terminée ; revue humaine obligatoire.");
            }
            s.RawText = text;
            var candidates = profiles.Where(p => text.Contains(p.DetectionText, StringComparison.OrdinalIgnoreCase)).ToList();
            profile ??= candidates.Count == 1 ? candidates[0] : null;
            if (profile is null || !text.Contains(profile.DetectionText, StringComparison.OrdinalIgnoreCase))
            { s.Status = "UNKNOWN_FORMAT"; s.Message = "Aucun profil compatible identifié. Sélectionnez un profil vérifié ou saisissez les opérations dans la revue."; }
            else
            {
                s.ProfileId = profile.Id;
                StatementService.History(s, s.CreatedBy, "BANK_DETECTED", $"Profil {profile.Code}, version {profile.Version}.");
                s.Transactions = extractor.Extract(text, profile, s.Currency, confidence);
                s.Status = "REVIEW_REQUIRED"; s.Message = $"{s.Transactions.Count} opérations proposées. Vérifiez toutes les lignes, la période et les soldes avant validation.";
                StatementService.History(s, s.CreatedBy, "TRANSACTIONS_EXTRACTED", s.Message);
            }
            StatementService.History(s, s.CreatedBy, "PROCESS_COMPLETED", s.Message); await db.SaveChangesAsync(ct);
            logger.LogInformation("Traitement du relevé {Id} terminé : {Status}", id, s.Status);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            // Reload tracked state so a failed save cannot accidentally persist partial transactions.
            db.ChangeTracker.Clear(); s = await statements.Load(id, new(Guid.Empty, true), CancellationToken.None);
            s.Status = s.Status == "OCR_PROCESSING" ? "OCR_FAILED" : "EXTRACTION_FAILED";
            s.Message = ex is AppException ? ex.Message : "Extraction impossible. Vérifiez le PDF ou utilisez la revue manuelle.";
            StatementService.History(s, s.CreatedBy, s.Status, s.Message); await db.SaveChangesAsync(CancellationToken.None);
            logger.LogWarning("Traitement {Id} échoué : {Type}", id, ex.GetType().Name);
        }
    }
}
