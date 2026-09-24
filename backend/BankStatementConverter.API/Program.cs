using System.Text;
using System.Threading.RateLimiting;
using BankStatementConverter.API;
using BankStatementConverter.Application;
using BankStatementConverter.Domain;
using BankStatementConverter.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
var key = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32) throw new InvalidOperationException("Configurez Jwt__Key (32 octets minimum) via variable d'environnement.");
var connection = builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("Configurez ConnectionStrings__Default.");
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connection));
builder.Services.AddControllers(o => o.Filters.Add<CatalogPermissionFilter>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => {
    o.SwaggerDoc("v1", new() { Title = "ReleveFlow - Référentiels", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new() { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
    o.AddSecurityRequirement(new() { [new() { Reference = new() { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>() });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.TokenValidationParameters = new() {
    ValidateIssuer = true, ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"],
    ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(15)
});
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options => {
    options.Events = new JwtBearerEvents { OnTokenValidated = async context => {
        var id = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(id, out var userId)) { context.Fail("Session invalide."); return; }
        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, context.HttpContext.RequestAborted);
        var version = context.Principal?.FindFirst("session_version")?.Value;
        // Old sessions without a version are accepted only until the first account change.
        if (user is null || !user.IsActive || (version ?? "0") != user.SessionVersion.ToString()) { context.Fail("Session révoquée."); return; }
        var identity = (System.Security.Claims.ClaimsIdentity)context.Principal!.Identity!;
        foreach (var claim in identity.FindAll(System.Security.Claims.ClaimTypes.Role).Concat(identity.FindAll("permission")).ToList()) identity.RemoveClaim(claim);
        identity.AddClaim(new(System.Security.Claims.ClaimTypes.Role, user.Role));
        foreach (var permission in UserManagementService.EffectivePermissions(user)) identity.AddClaim(new("permission", permission));
    }};
});
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:3001"]).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddRateLimiter(o => {
    o.RejectionStatusCode = 429;
    o.AddPolicy("statements", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new() { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new() { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
});
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
builder.Services.AddSingleton<IPdfAnalyzer, PdfAnalyzer>();
builder.Services.AddSingleton<IOcrService, TesseractOcrService>();
builder.Services.AddSingleton<IStatementValidator, StatementValidator>();
builder.Services.AddSingleton<IStatementExporter, DelimitedStatementExporter>();
builder.Services.AddScoped<StatementService>();
builder.Services.AddScoped<ExportTemplateService>();
builder.Services.AddScoped<StatementExportService>();
builder.Services.AddScoped<ProfileTransactionExtractor>();
builder.Services.AddScoped<PdfProcessingService>();
builder.Services.AddHostedService<StatementWorker>();
builder.Services.AddScoped<IBankService, BankService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICatalogDeletionService, CatalogDeletionService>();
builder.Services.AddScoped<ICatalogImageService, CatalogImageService>();
builder.Services.AddScoped<ICatalogService<ClientInput,ClientDto>, CatalogService<Client,ClientInput,ClientDto>>();
builder.Services.AddScoped<ICatalogService<BankAccountInput,BankAccountDto>, CatalogService<BankAccount,BankAccountInput,BankAccountDto>>();
builder.Services.AddScoped<ICatalogDefinition<Client,ClientInput,ClientDto>,ClientDefinition>();
builder.Services.AddScoped<ICatalogDefinition<BankAccount,BankAccountInput,BankAccountDto>,AccountDefinition>();
var app = builder.Build();
app.Use(async (ctx, next) => {
    try { await next(); }
    catch (Exception ex) {
        var status = ex switch { AppException a => a.Status, DbUpdateConcurrencyException => 409, DbUpdateException { InnerException: PostgresException { SqlState: "23505" or "23503" or "23001" } } => 409, BadHttpRequestException b => b.StatusCode, OperationCanceledException => 499, _ => 500 };
        var message = ex is AppException ? ex.Message : status == 409 ? "Doublon ou élément encore utilisé : modification impossible." : status == 413 ? "Fichier trop volumineux." : "Erreur serveur. Consultez les journaux.";
        app.Logger.Log(status >= 500 ? LogLevel.Error : LogLevel.Warning, ex, "Requête échouée : {Status}", status);
        if (!ctx.Response.HasStarted) { ctx.Response.StatusCode = status; await ctx.Response.WriteAsJsonAsync(new { title = message, status, traceId = ctx.TraceIdentifier }); }
    }
});
app.UseCors(); app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter();
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled")) { app.UseSwagger(); app.UseSwaggerUI(); }
app.MapControllers();
app.MapGet("/health", async (AppDbContext db) => await db.Database.CanConnectAsync() ? Results.Ok(new { status = "ok" }) : Results.StatusCode(503));
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (builder.Configuration.GetValue<bool>("Database:AutoMigrate")) await db.Database.MigrateAsync();
    await Seed.RunAsync(db, builder.Configuration);
}
app.Run();
public partial class Program;


