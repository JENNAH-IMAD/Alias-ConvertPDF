using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BankStatementConverter.Application;
using BankStatementConverter.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;
namespace BankStatementConverter.Tests;

public class StatementIntegrationTests
{
    [Fact, Trait("Category", "Integration")]
    public async Task UploadReviewExportsArchivesAndOwnership()
    {
        var connection = Environment.GetEnvironmentVariable("TEST_DATABASE_URL"); Assert.False(string.IsNullOrWhiteSpace(connection));
        var cs = new NpgsqlConnectionStringBuilder(connection!); var name = "statements_test_" + Guid.NewGuid().ToString("N");
        await using var control = new NpgsqlConnection(connection); await control.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", control)) await create.ExecuteNonQueryAsync();
        cs.Database = name;
        var folder = Path.Combine(Path.GetTempPath(), name); var oldStorage = Environment.GetEnvironmentVariable("Statements__StoragePath");
        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__Default", cs.ConnectionString);
            Environment.SetEnvironmentVariable("Statements__StoragePath", folder);
            Environment.SetEnvironmentVariable("Jwt__Key", new string('x', 64)); Environment.SetEnvironmentVariable("Seed__AdminEmail", "admin@tests.local"); Environment.SetEnvironmentVariable("Seed__AdminPassword", "TestPassword123!"); Environment.SetEnvironmentVariable("Database__AutoMigrate", "true");
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.AddDataProtection().UseEphemeralDataProtectionProvider()));
            using var admin = factory.CreateClient();
            async Task Login(HttpClient client, string email) { var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto(email,"TestPassword123!")); response.EnsureSuccessStatusCode(); var auth = (await response.Content.ReadFromJsonAsync<AuthResult>())!; client.DefaultRequestHeaders.Authorization = new("Bearer",auth.Token); }
            await Login(admin,"admin@tests.local");
            async Task<T> Create<T>(string path, object input) { var response = await admin.PostAsJsonAsync(path,input); response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<T>())!; }
            var client = await Create<ClientDto>("/api/clients",new ClientInput("Test","Test SARL","123456789012345","IF1","RC1","test@tests.local","0600000000"));
            var bank = await Create<BankDto>("/api/banks",new BankInput("Test Bank","TEST","Fixture"));
            var account = await Create<BankAccountDto>("/api/bank-accounts",new BankAccountInput(client.Id,bank.Id,"TEST001","Compte test","MAD","BQ","512000"));
            using var owner = factory.CreateClient();using var outsider = factory.CreateClient();
            foreach(var (http,email,user) in new[]{(owner,"owner@tests.local","Owner"),(outsider,"other@tests.local","Other")}) { (await http.PostAsJsonAsync("/api/auth/register",new RegisterDto(user,email,"TestPassword123!"))).EnsureSuccessStatusCode(); await Login(http,email); }
            async Task<HttpResponseMessage> Upload(byte[] bytes) { using var form = new MultipartFormDataContent(); var content = new ByteArrayContent(bytes); content.Headers.ContentType = new("application/pdf"); form.Add(content,"file","releve.pdf");return await owner.PostAsync($"/api/bank-accounts/{account.Id}/statements/upload",form); }
            Assert.Equal(HttpStatusCode.BadRequest,(await Upload("not a pdf"u8.ToArray())).StatusCode);
            var pdfBytes = StatementTests.Pdf(); var uploaded = await Upload(pdfBytes);uploaded.EnsureSuccessStatusCode();var id=(await uploaded.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            Assert.Equal(HttpStatusCode.Conflict,(await Upload(pdfBytes)).StatusCode); // PdfPig metadata is deterministic within this fixture.
            Assert.Equal(HttpStatusCode.NotFound,(await outsider.GetAsync($"/api/statements/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,(await outsider.GetAsync($"/api/statements/{id}/pdf")).StatusCode);
            async Task<JsonElement> Detail() => await owner.GetFromJsonAsync<JsonElement>($"/api/statements/{id}");
            var detail = await Detail();var version = detail.GetProperty("version").GetGuid();
            (await owner.PostAsJsonAsync($"/api/statements/{id}/process",new ProcessInput(version,null))).EnsureSuccessStatusCode();
            for (var retry=0;retry<60;retry++) { await Task.Delay(200);detail=await Detail();if(detail.GetProperty("status").GetString()=="UNKNOWN_FORMAT")break; }
            Assert.Equal("UNKNOWN_FORMAT",detail.GetProperty("status").GetString());version=detail.GetProperty("version").GetGuid();
            var review = new ReviewInput(version,new(2026,9,1),new(2026,9,30),100,125,[new(new(2026,9,2),null,"REF","Virement",null,25,null)]);
            (await owner.PutAsJsonAsync($"/api/statements/{id}/review",review)).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Conflict,(await owner.PutAsJsonAsync($"/api/statements/{id}/review",review)).StatusCode);
            detail=await Detail();version=detail.GetProperty("version").GetGuid();
            (await owner.PostAsJsonAsync($"/api/statements/{id}/validate",new VersionInput(version))).EnsureSuccessStatusCode();
            foreach(var type in new[]{"SAGE100_STANDARD","SAGE_X3","CUSTOM_CSV"}) (await admin.PostAsync("/api/export-templates/presets/"+type,null)).EnsureSuccessStatusCode();
            var templates=await admin.GetFromJsonAsync<JsonElement>("/api/export-templates");
            foreach(var template in templates.EnumerateArray()) { detail=await Detail();version=detail.GetProperty("version").GetGuid(); (await owner.PostAsJsonAsync($"/api/statements/{id}/exports",new ExportInput(version,template.GetProperty("id").GetGuid()))).EnsureSuccessStatusCode(); }
            detail=await Detail();Assert.Equal(3,detail.GetProperty("exports").GetArrayLength());version=detail.GetProperty("version").GetGuid();
            (await owner.PostAsJsonAsync($"/api/statements/{id}/archive",new VersionInput(version))).EnsureSuccessStatusCode();
            foreach(var export in detail.GetProperty("exports").EnumerateArray()) { var path=$"/api/statement-exports/{export.GetProperty("id").GetGuid()}/download"; (await owner.GetAsync(path)).EnsureSuccessStatusCode();Assert.Equal(HttpStatusCode.NotFound,(await outsider.GetAsync(path)).StatusCode); }
            Assert.Equal(HttpStatusCode.Conflict,(await admin.DeleteAsync($"/api/bank-accounts/{account.Id}")).StatusCode);
            Assert.Equal(1,(await owner.GetFromJsonAsync<JsonElement>("/api/archives")).GetProperty("total").GetInt32());
            Assert.Equal(0,(await outsider.GetFromJsonAsync<JsonElement>("/api/statements")).GetProperty("total").GetInt32());
        }
        finally { Environment.SetEnvironmentVariable("Statements__StoragePath",oldStorage); NpgsqlConnection.ClearAllPools(); await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)",control);await drop.ExecuteNonQueryAsync();if(Directory.Exists(folder))Directory.Delete(folder,true); }
    }
}


