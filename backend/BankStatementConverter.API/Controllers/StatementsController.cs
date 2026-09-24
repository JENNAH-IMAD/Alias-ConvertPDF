using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using BankStatementConverter.Application;
using BankStatementConverter.Domain;
using BankStatementConverter.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace BankStatementConverter.API;

[ApiController, Authorize, EnableRateLimiting("statements"), Route("api/statements")]
public class StatementsController(StatementService statements, PdfProcessingService processor, IStatementValidator validator, StatementExportService exports, IFileStorageService files) : ControllerBase
{
    private StatementActor Actor => new(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), User.IsInRole("Admin"));
    [HttpGet, HttpGet("/api/archives")]
    public Task<PageResult<StatementRow>> List(CancellationToken ct, int page = 1, Guid? clientId = null, Guid? accountId = null, bool archived = false, string? status = null)
        => statements.List(Actor, page, clientId, accountId, archived || Request.Path == "/api/archives", status, ct);
    [HttpGet("{id:guid}")]
    public async Task<object> Get(Guid id, CancellationToken ct)
    {
        var s = await statements.Load(id, Actor, ct);
        var overlapping = s.PeriodStart.HasValue && await statements.Visible(Actor).AnyAsync(x => x.Id != id && x.BankAccountId == s.BankAccountId && x.PeriodStart == s.PeriodStart && x.PeriodEnd == s.PeriodEnd, ct);
        return new { s.Id, s.BankAccountId, ClientId = s.BankAccount.ClientId, Client = s.BankAccount.Client.Name, Bank = s.BankAccount.Bank.Name, BankId = s.BankAccount.BankId, Account = s.BankAccount.AccountNumber,
            s.OriginalFileName, s.FileSize, s.FileHash, s.PageCount, s.PdfType, s.Status, s.Message, s.Currency, s.PeriodStart, s.PeriodEnd, s.OpeningBalance, s.ClosingBalance, s.Version, s.ProfileId, s.CreatedAt, s.ValidatedAt, s.ArchivedAt, OverlappingPeriod = overlapping,
            Transactions = s.Transactions.OrderBy(t => t.Position).Select(t => new { t.Id, t.Position, t.TransactionDate, t.ValueDate, t.Reference, t.Description, t.Debit, t.Credit, t.Balance, t.ConfidenceScore, t.IsValidated }),
            Check = validator.Check(s), History = s.History.OrderByDescending(h => h.CreatedAt).Select(h => new { h.Id, h.Action, h.Status, h.Message, h.UserId, h.CreatedAt }),
            Exports = s.Exports.OrderByDescending(e => e.CreatedAt).Select(e => new { e.Id, e.FileName, e.FileSize, e.Status, e.CreatedAt }),
            s.RawText };
    }
    [HttpPost("/api/bank-accounts/{accountId:guid}/statements/upload"), RequestSizeLimit(21 * 1024 * 1024)]
    public async Task<IActionResult> Upload(Guid accountId, IFormFile file, CancellationToken ct)
    {
        if (file.Length > 20 * 1024 * 1024) throw new AppException(413, "Maximum 20 Mo.");
        using var buffer = new MemoryStream(); await file.CopyToAsync(buffer, ct);
        var id = await statements.Upload(accountId, Actor, file.FileName, file.ContentType, buffer.ToArray(), ct);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }
    [HttpPost("{id:guid}/process")]
    public async Task<IActionResult> Process(Guid id, ProcessInput input, CancellationToken ct) { await processor.Queue(id, input, Actor, ct); return Accepted(new { id, status = "QUEUED" }); }
    [HttpPut("{id:guid}/review")]
    public async Task<IActionResult> Review(Guid id, ReviewInput input, CancellationToken ct) { await statements.Review(id, input, Actor, ct); return NoContent(); }
    [HttpPost("{id:guid}/validate")]
    public async Task<IActionResult> Validate(Guid id, VersionInput input, CancellationToken ct) { await statements.Validate(id, input.Version, Actor, ct); return NoContent(); }
    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, VersionInput input, CancellationToken ct) { await statements.Archive(id, input.Version, Actor, ct); return NoContent(); }
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken ct)
    { var s = await statements.Load(id, Actor, ct); return File(await files.ReadAsync(s.StorageKey, ct), "application/pdf", s.OriginalFileName); }
    [HttpPost("{id:guid}/export-preview")]
    public async Task<object> Preview(Guid id, ExportInput input, CancellationToken ct)
    { var (bytes, template) = await exports.Preview(id, input, Actor, ct); Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); return new { text = Encoding.GetEncoding(template.Encoding).GetString(bytes), limitedTo = 10 }; }
    [HttpPost("{id:guid}/exports")]
    public async Task<object> Export(Guid id, ExportInput input, CancellationToken ct) => new { id = await exports.Generate(id, input, Actor, ct) };
    [HttpGet("/api/statement-exports/{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct) { var f = await exports.Download(id, Actor, ct); return File(f.Bytes, f.MimeType, f.Name); }
}

[ApiController, Authorize, Route("api/export-templates")]
public class ExportTemplatesController(ExportTemplateService service) : ControllerBase
{
    [HttpGet] public Task<List<ExportTemplate>> List(CancellationToken ct) => service.List(ct);
    [HttpGet("fields")] public object Fields() => new { sources = DelimitedStatementExporter.Sources, dateFormats = DelimitedStatementExporter.DateFormats };
    [HttpPost, Authorize(Roles = "Admin")] public Task<ExportTemplate> Create(TemplateInput input, CancellationToken ct) => service.Save(null, input, ct);
    [HttpPut("{id:guid}"), Authorize(Roles = "Admin")] public Task<ExportTemplate> Update(Guid id, TemplateInput input, CancellationToken ct) => service.Save(id, input, ct);
    [HttpDelete("{id:guid}"), Authorize(Roles = "Admin")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await service.Delete(id, ct); return NoContent(); }
    [HttpPost("presets/{type}"), Authorize(Roles = "Admin")] public async Task<IActionResult> Preset(string type, CancellationToken ct) { await service.CreatePreset(type, ct); return NoContent(); }
}

[ApiController, Authorize, Route("api/statement-profiles")]
public class StatementProfilesController(AppDbContext db) : ControllerBase
{
    [HttpGet] public Task<List<BankStatementProfile>> List(CancellationToken ct, Guid? bankId = null) => db.BankStatementProfiles.AsNoTracking().Where(p => !bankId.HasValue || p.BankId == bankId).OrderBy(p => p.Name).ToListAsync(ct);
    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<object> Create(ProfileInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 100 || input.Code.Length > 50 || input.Version < 1 || string.IsNullOrWhiteSpace(input.DetectionText) || input.DetectionText.Length > 200 || input.TransactionPattern.Length > 1500) throw new AppException(400, "Profil incomplet ou trop long.");
        if (!DelimitedStatementExporter.DateFormats.Contains(input.DateFormat) || input.DecimalSeparator is not ("," or ".") || input.ThousandsSeparator is not ("" or " " or "." or ",") || input.ThousandsSeparator == input.DecimalSeparator) throw new AppException(400, "Formats numériques ou date invalides.");
        try { var regex = new Regex(input.TransactionPattern, RegexOptions.None, TimeSpan.FromSeconds(1)); if (!regex.GetGroupNames().Contains("date") || !regex.GetGroupNames().Contains("description")) throw new AppException(400, "Groupes date et description obligatoires."); }
        catch (ArgumentException) { throw new AppException(400, "Expression régulière invalide."); }
        if (!await db.Banks.AnyAsync(b => b.Id == input.BankId, ct)) throw new AppException(404, "Banque introuvable.");
        var p = new BankStatementProfile { BankId = input.BankId, Name = input.Name, Code = input.Code, Version = input.Version, IsActive = input.IsActive, DetectionText = input.DetectionText, TransactionPattern = input.TransactionPattern, DateFormat = input.DateFormat, DecimalSeparator = input.DecimalSeparator, ThousandsSeparator = input.ThousandsSeparator, OcrRequired = input.OcrRequired };
        db.BankStatementProfiles.Add(p); await db.SaveChangesAsync(ct); return new { p.Id };
    }
}


