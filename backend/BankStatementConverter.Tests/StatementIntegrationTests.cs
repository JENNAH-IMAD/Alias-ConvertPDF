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
            Assert.Equal(HttpStatusCode.NotFound,(await outsider.GetAsync($"/api/statements/{id}/converted/download")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,(await outsider.GetAsync($"/api/statements/{id}/converted/preview")).StatusCode);
            Assert.Equal(HttpStatusCode.UnprocessableEntity,(await owner.GetAsync($"/api/statements/{id}/converted/download")).StatusCode);
            async Task<JsonElement> Detail() => await owner.GetFromJsonAsync<JsonElement>($"/api/statements/{id}");
            var detail = await Detail();var version = detail.GetProperty("version").GetGuid();
            (await owner.PostAsJsonAsync($"/api/statements/{id}/process",new ProcessInput(version,null))).EnsureSuccessStatusCode();
            for (var retry=0;retry<30;retry++) { await Task.Delay(500);detail=await Detail();if(detail.GetProperty("status").GetString()=="UNKNOWN_FORMAT")break; }
            Assert.Equal("UNKNOWN_FORMAT",detail.GetProperty("status").GetString());version=detail.GetProperty("version").GetGuid();
            Assert.Equal(HttpStatusCode.UnprocessableEntity,(await owner.PostAsJsonAsync($"/api/statements/{id}/validate",new VersionInput(version))).StatusCode);
            detail=await Detail();Assert.Equal("VALIDATION_FAILED",detail.GetProperty("status").GetString());version=detail.GetProperty("version").GetGuid();
            var review = new ReviewInput(version,new(2026,9,1),new(2026,9,30),100,125,[new(new(2026,9,2),null,"REF","Virement",null,25,null)]);
            (await owner.PutAsJsonAsync($"/api/statements/{id}/review",review)).EnsureSuccessStatusCode();
            var converted=await owner.GetAsync($"/api/statements/{id}/converted/download");converted.EnsureSuccessStatusCode();
            Assert.Equal("text/csv",converted.Content.Headers.ContentType!.MediaType);
            Assert.Contains("a_verifier",converted.Content.Headers.ContentDisposition!.ToString());
            Assert.Contains("25,0000",await converted.Content.ReadAsStringAsync());
            var convertedPreview=await admin.GetFromJsonAsync<JsonElement>($"/api/statements/{id}/converted/preview");
            Assert.Contains("Date de valeur",convertedPreview.GetProperty("text").GetString());
            Assert.Equal(HttpStatusCode.Conflict,(await owner.PutAsJsonAsync($"/api/statements/{id}/review",review)).StatusCode);
            detail=await Detail();version=detail.GetProperty("version").GetGuid();
            (await owner.PostAsJsonAsync($"/api/statements/{id}/validate",new VersionInput(version))).EnsureSuccessStatusCode();
            var invalidTemplate=await Create<JsonElement>("/api/export-templates",new TemplateInput("Analytique obligatoire","REQUIRED_FIELD_TEST","CUSTOM_CSV","UTF-8",";","dd/MM/yyyy",",",2,true,true,null,[new("Analytic","Analytique",0,true,"")]));
            detail=await Detail();version=detail.GetProperty("version").GetGuid();
            Assert.Equal(HttpStatusCode.UnprocessableEntity,(await owner.PostAsJsonAsync($"/api/statements/{id}/exports",new ExportInput(version,invalidTemplate.GetProperty("id").GetGuid()))).StatusCode);
            detail=await Detail();Assert.Equal("EXPORT_FAILED",detail.GetProperty("status").GetString());
            Assert.Contains(detail.GetProperty("history").EnumerateArray(), h=>h.GetProperty("action").GetString()=="EXPORT_FAILED");
            (await admin.DeleteAsync("/api/export-templates/"+invalidTemplate.GetProperty("id").GetGuid())).EnsureSuccessStatusCode();
            foreach(var type in new[]{"SAGE100_STANDARD","SAGE_X3","CUSTOM_CSV"}) (await admin.PostAsync("/api/export-templates/presets/"+type,null)).EnsureSuccessStatusCode();
            var templates=await admin.GetFromJsonAsync<JsonElement>("/api/export-templates");
            Assert.Equal(3, templates.GetArrayLength()); // Presets are installed once and creation is idempotent.
            foreach (var template in templates.EnumerateArray())
            {
                Assert.Contains(template.GetProperty("fields").EnumerateArray(), f => f.GetProperty("outputField").GetString() == "Libellé");
                if (template.GetProperty("type").GetString() == "CUSTOM_CSV") Assert.Equal(",", template.GetProperty("delimiter").GetString());
            }
            foreach(var template in templates.EnumerateArray()) { detail=await Detail();version=detail.GetProperty("version").GetGuid(); (await owner.PostAsJsonAsync($"/api/statements/{id}/exports",new ExportInput(version,template.GetProperty("id").GetGuid()))).EnsureSuccessStatusCode(); }
            detail=await Detail();Assert.Equal(3,detail.GetProperty("exports").GetArrayLength());version=detail.GetProperty("version").GetGuid();
            (await owner.PostAsJsonAsync($"/api/statements/{id}/archive",new VersionInput(version))).EnsureSuccessStatusCode();
            detail=await Detail();version=detail.GetProperty("version").GetGuid();
            Assert.Equal(HttpStatusCode.NotFound,(await outsider.PostAsJsonAsync($"/api/statements/{id}/reopen",new VersionInput(version))).StatusCode);
            (await owner.PostAsJsonAsync($"/api/statements/{id}/reopen",new VersionInput(version))).EnsureSuccessStatusCode();
            detail=await Detail();Assert.Equal("VALIDATED",detail.GetProperty("status").GetString());Assert.Equal(JsonValueKind.Null,detail.GetProperty("archivedAt").ValueKind);
            (await owner.PostAsJsonAsync($"/api/statements/{id}/archive",new VersionInput(detail.GetProperty("version").GetGuid()))).EnsureSuccessStatusCode();
            foreach(var export in detail.GetProperty("exports").EnumerateArray()) { var path=$"/api/statement-exports/{export.GetProperty("id").GetGuid()}/download"; (await owner.GetAsync(path)).EnsureSuccessStatusCode();Assert.Equal(HttpStatusCode.NotFound,(await outsider.GetAsync(path)).StatusCode); }
            foreach(var export in detail.GetProperty("exports").EnumerateArray())
            {
                var previewPath=$"/api/statement-exports/{export.GetProperty("id").GetGuid()}/preview";
                var content=await admin.GetFromJsonAsync<JsonElement>(previewPath);
                Assert.Contains("Libellé",content.GetProperty("text").GetString());
                Assert.Contains("Virement",content.GetProperty("text").GetString());
                Assert.Equal(HttpStatusCode.NotFound,(await outsider.GetAsync(previewPath)).StatusCode);
            }
            converted=await admin.GetAsync($"/api/statements/{id}/converted/download");converted.EnsureSuccessStatusCode();
            Assert.Contains("valide",converted.Content.Headers.ContentDisposition!.ToString());
            Assert.Equal(HttpStatusCode.Conflict,(await admin.DeleteAsync($"/api/bank-accounts/{account.Id}")).StatusCode);
            var archiveList=await owner.GetFromJsonAsync<JsonElement>("/api/archives");
            Assert.Equal(1,archiveList.GetProperty("total").GetInt32());
            Assert.Equal(3,archiveList.GetProperty("items")[0].GetProperty("exportCount").GetInt32());
            Assert.Equal(1,archiveList.GetProperty("items")[0].GetProperty("transactionCount").GetInt32());
            Assert.Equal(0,(await outsider.GetFromJsonAsync<JsonElement>("/api/statements")).GetProperty("total").GetInt32());
            Assert.Equal(1,(await owner.GetFromJsonAsync<JsonElement>($"/api/archives?bankId={bank.Id}&from=2026-09-01&to=2026-09-30&exported=true")).GetProperty("total").GetInt32());
            Assert.Equal(0,(await owner.GetFromJsonAsync<JsonElement>("/api/archives?from=2026-10-01&exported=false")).GetProperty("total").GetInt32());
            Assert.Equal(HttpStatusCode.BadRequest,(await owner.GetAsync("/api/archives?from=2026-10-01&to=2026-09-01")).StatusCode);
            detail=await Detail();
            Assert.Equal(HttpStatusCode.Conflict,(await owner.PutAsJsonAsync($"/api/statements/{id}/review",review with {Version=detail.GetProperty("version").GetGuid()})).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict,(await admin.PutAsJsonAsync($"/api/bank-accounts/{account.Id}",new BankAccountInput(client.Id,bank.Id,"CHANGED","Compte test","MAD","BQ","512000"))).StatusCode);

            var profileInput = new ProfileInput(bank.Id,"Fixture seulement","FIXTURE",1,true,"TEST BANK",@"TEST BANK (?<date>\d{2}/\d{2}/\d{4});(?<description>[^;]+);(?<credit>\d+,\d+)","dd/MM/yyyy",","," ",false);
            Assert.Equal(HttpStatusCode.Forbidden,(await owner.PostAsJsonAsync("/api/statement-profiles",profileInput)).StatusCode);
            var profile = await Create<JsonElement>("/api/statement-profiles",profileInput);
            uploaded=await Upload(StatementTests.Pdf("TEST BANK 02/09/2026;Virement fixture;25,00"));uploaded.EnsureSuccessStatusCode();id=(await uploaded.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            detail=await Detail();version=detail.GetProperty("version").GetGuid();
            (await owner.PostAsJsonAsync($"/api/statements/{id}/process",new ProcessInput(version,profile.GetProperty("id").GetGuid()))).EnsureSuccessStatusCode();
            for(var retry=0;retry<30;retry++){await Task.Delay(300);detail=await Detail();if(detail.GetProperty("status").GetString()=="REVIEW_REQUIRED")break;}
            Assert.Equal("REVIEW_REQUIRED",detail.GetProperty("status").GetString());
            var transaction=detail.GetProperty("transactions")[0];var transactionId=transaction.GetProperty("id").GetGuid();
            Assert.Equal(25,transaction.GetProperty("credit").GetDecimal());
            version=detail.GetProperty("version").GetGuid();
            (await owner.PutAsJsonAsync($"/api/statements/{id}/review",new ReviewInput(version,new(2026,9,1),new(2026,9,30),100,125,[new(new(2026,9,2),null,"REF","Virement corrigé",null,25,125,transactionId)]))).EnsureSuccessStatusCode();
            detail=await Detail();Assert.Equal(transactionId,detail.GetProperty("transactions")[0].GetProperty("id").GetGuid());
            using var scope=factory.Services.CreateScope();
            var saved=await scope.ServiceProvider.GetRequiredService<AppDbContext>().BankTransactions.FindAsync(transactionId);
            Assert.Contains("Virement fixture",saved!.RawText);
            async Task<HttpResponseMessage> DeleteStatement(HttpClient http, Guid statementId, Guid statementVersion)
            {
                using var request = new HttpRequestMessage(HttpMethod.Delete,$"/api/statements/{statementId}") { Content=JsonContent.Create(new VersionInput(statementVersion)) };
                return await http.SendAsync(request);
            }
            Assert.Equal(HttpStatusCode.NotFound,(await DeleteStatement(outsider,id,detail.GetProperty("version").GetGuid())).StatusCode);
            (await DeleteStatement(owner,id,detail.GetProperty("version").GetGuid())).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NotFound,(await owner.GetAsync($"/api/statements/{id}")).StatusCode);

            // Chaque relevé importé occupe une place dans l'archive du client, quel que soit son statut.
            var secondAccount = await Create<BankAccountDto>("/api/bank-accounts",new BankAccountInput(client.Id,bank.Id,"TEST002","Second compte","MAD","BQ","512000"));
            async Task<HttpResponseMessage> UploadTo(Guid targetAccount, string text, string fileName)
            {
                using var form = new MultipartFormDataContent(); var content = new ByteArrayContent(StatementTests.Pdf(text)); content.Headers.ContentType = new("application/pdf"); form.Add(content,"file",fileName);
                return await owner.PostAsync($"/api/bank-accounts/{targetAccount}/statements/upload",form);
            }
            for(var index=0;index<3;index++) (await UploadTo(index%2==0?account.Id:secondAccount.Id,$"Relevé capacité {index}",$"capacite-{index}.pdf")).EnsureSuccessStatusCode();
            var competingUploads=await Task.WhenAll(UploadTo(account.Id,"Cinquième relevé A","cinquieme-a.pdf"),UploadTo(secondAccount.Id,"Cinquième relevé B","cinquieme-b.pdf"));
            Assert.Equal(new[]{HttpStatusCode.Created,HttpStatusCode.Conflict},competingUploads.Select(response=>response.StatusCode).OrderBy(code=>code).ToArray());
            Assert.Equal(5,await scope.ServiceProvider.GetRequiredService<StatementService>().ArchiveCount(client.Id,CancellationToken.None));
            var usage=await owner.GetFromJsonAsync<JsonElement>($"/api/clients/{client.Id}/archive-usage");
            Assert.Equal(5,usage.GetProperty("count").GetInt32());Assert.Equal(5,usage.GetProperty("limit").GetInt32());
            var otherClient=await Create<ClientDto>("/api/clients",new ClientInput("Autre client","Autre SARL","123456789012346","IF2","RC2","autre@tests.local","0600000001"));
            var otherAccount=await Create<BankAccountDto>("/api/bank-accounts",new BankAccountInput(otherClient.Id,bank.Id,"OTHER001","Autre compte","MAD","BQ","512000"));
            (await UploadTo(otherAccount.Id,"Archive indépendante","independant.pdf")).EnsureSuccessStatusCode();
            var otherUsage=await owner.GetFromJsonAsync<JsonElement>($"/api/clients/{otherClient.Id}/archive-usage");
            Assert.Equal(1,otherUsage.GetProperty("count").GetInt32());
        }
        finally { Environment.SetEnvironmentVariable("Statements__StoragePath",oldStorage); NpgsqlConnection.ClearAllPools(); await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)",control);await drop.ExecuteNonQueryAsync();if(Directory.Exists(folder))Directory.Delete(folder,true); }
    }
}


