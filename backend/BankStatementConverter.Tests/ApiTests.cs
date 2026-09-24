using System.Net;
using Microsoft.EntityFrameworkCore;
using BankStatementConverter.Infrastructure;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using BankStatementConverter.Application;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace BankStatementConverter.Tests;

public class ApiTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task PostgreSqlCatalogsImagesPermissionsAndDeletion()
    {
        var connection = Environment.GetEnvironmentVariable("TEST_DATABASE_URL");
        Assert.False(string.IsNullOrWhiteSpace(connection), "Définir TEST_DATABASE_URL vers une base PostgreSQL de test dédiée.");
        var suffix = Guid.NewGuid().ToString("N"); var password = "TestA1" + Convert.ToHexString(RandomNumberGenerator.GetBytes(18));
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", connection);
        Environment.SetEnvironmentVariable("Jwt__Key", Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
        Environment.SetEnvironmentVariable("Seed__AdminEmail", $"admin-{suffix}@example.test");
        Environment.SetEnvironmentVariable("Seed__AdminPassword", password);
        Environment.SetEnvironmentVariable("Database__AutoMigrate", "true");
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddDataProtection().UseEphemeralDataProtectionProvider()));
        using var http = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/api/clients")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.PostAsJsonAsync("/api/auth/login",new LoginDto($"admin-{suffix}@example.test","WrongPassword12"))).StatusCode);
        var login = await http.PostAsJsonAsync("/api/auth/login", new LoginDto($"admin-{suffix}@example.test",password)); login.EnsureSuccessStatusCode();
        var session = (await login.Content.ReadFromJsonAsync<AuthResult>())!; Assert.Equal("Admin",session.User.Role);
        http.DefaultRequestHeaders.Authorization = new("Bearer", session.Token);
        var invalid = await http.PostAsJsonAsync("/api/clients",new ClientInput("", "", "123", "", "", "invalid", "")); Assert.Equal(HttpStatusCode.BadRequest,invalid.StatusCode);
        var ice = DateTime.UtcNow.Ticks.ToString()[^15..];
        var clientInput = new ClientInput("Test", "Fictif", ice,"IFTEST","RCTEST","client@example.test","+212600000000");
        var clientResponse = await http.PostAsJsonAsync("/api/clients",clientInput); clientResponse.EnsureSuccessStatusCode();
        var client = (await clientResponse.Content.ReadFromJsonAsync<ClientDto>())!;
        Assert.Equal(HttpStatusCode.Conflict,(await http.PostAsJsonAsync("/api/clients",clientInput)).StatusCode);
        var updated = await http.PutAsJsonAsync($"/api/clients/{client.Id}",clientInput with {Name="Modifié"}); updated.EnsureSuccessStatusCode(); Assert.Equal("Modifié",(await updated.Content.ReadFromJsonAsync<ClientDto>())!.Name);
        var photoPath = $"/api/clients/{client.Id}/photo";
        using (var anonymous = factory.CreateClient()) Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(photoPath)).StatusCode);
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aOZkAAAAASUVORK5CYII=");
        async Task<HttpResponseMessage> UploadPhoto(byte[] bytes) { using var form = new MultipartFormDataContent(); var content = new ByteArrayContent(bytes); content.Headers.ContentType = new("image/png"); form.Add(content,"file","logo.png"); return await http.PutAsync(photoPath,form); }
        (await UploadPhoto(png)).EnsureSuccessStatusCode();
        var withPhoto = (await http.GetFromJsonAsync<ClientDto>($"/api/clients/{client.Id}"))!;
        Assert.NotNull(withPhoto.PhotoVersion);
        using (var photo = System.Text.Json.JsonDocument.Parse(await http.GetStringAsync(photoPath))) Assert.Equal("data:image/png;base64,"+Convert.ToBase64String(png), photo.RootElement.GetProperty("dataUrl").GetString());
        (await UploadPhoto(png)).EnsureSuccessStatusCode();
        Assert.NotEqual(withPhoto.PhotoVersion,(await http.GetFromJsonAsync<ClientDto>($"/api/clients/{client.Id}"))!.PhotoVersion);
        Assert.Equal(HttpStatusCode.BadRequest,(await UploadPhoto(System.Text.Encoding.UTF8.GetBytes("<svg></svg>"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await UploadPhoto(new byte[2*1024*1024+1])).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await http.DeleteAsync(photoPath)).StatusCode);
        Assert.Null((await http.GetFromJsonAsync<ClientDto>($"/api/clients/{client.Id}"))!.PhotoVersion);
        using (var photo = System.Text.Json.JsonDocument.Parse(await http.GetStringAsync(photoPath))) Assert.Equal(System.Text.Json.JsonValueKind.Null,photo.RootElement.GetProperty("dataUrl").ValueKind);
        var bankResponse = await http.PostAsJsonAsync("/api/banks",new BankInput("Banque fictive test",suffix[..24],"Fixture")); bankResponse.EnsureSuccessStatusCode(); var bank=(await bankResponse.Content.ReadFromJsonAsync<BankDto>())!;
        photoPath = $"/api/banks/{bank.Id}/logo";
        using (var anonymous = factory.CreateClient()) Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(photoPath)).StatusCode);
        (await UploadPhoto(png)).EnsureSuccessStatusCode();
        var withLogo = (await http.GetFromJsonAsync<BankDto>($"/api/banks/{bank.Id}"))!;
        Assert.NotNull(withLogo.LogoVersion);
        using (var photo = System.Text.Json.JsonDocument.Parse(await http.GetStringAsync(photoPath))) Assert.Equal("data:image/png;base64,"+Convert.ToBase64String(png), photo.RootElement.GetProperty("dataUrl").GetString());
        (await UploadPhoto(png)).EnsureSuccessStatusCode();
        Assert.NotEqual(withLogo.LogoVersion,(await http.GetFromJsonAsync<BankDto>($"/api/banks/{bank.Id}"))!.LogoVersion);
        Assert.Equal(HttpStatusCode.BadRequest,(await UploadPhoto(System.Text.Encoding.UTF8.GetBytes("<svg></svg>"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await UploadPhoto(new byte[2*1024*1024+1])).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await http.DeleteAsync(photoPath)).StatusCode);
        Assert.Null((await http.GetFromJsonAsync<BankDto>($"/api/banks/{bank.Id}"))!.LogoVersion);
        using (var photo = System.Text.Json.JsonDocument.Parse(await http.GetStringAsync(photoPath))) Assert.Equal(System.Text.Json.JsonValueKind.Null,photo.RootElement.GetProperty("dataUrl").ValueKind);
        var accountResponse = await http.PostAsJsonAsync("/api/bank-accounts", new BankAccountInput(client.Id,bank.Id,"TEST","Compte test","MAD","BQ","512000")); accountResponse.EnsureSuccessStatusCode(); var account=(await accountResponse.Content.ReadFromJsonAsync<BankAccountDto>())!;
        var bankDetails = (await http.GetFromJsonAsync<BankDto>($"/api/banks/{bank.Id}"))!;
        Assert.Equal(1, bankDetails.Accounts); Assert.Equal(1, bankDetails.Clients);
        Assert.Single((await http.GetFromJsonAsync<PageResult<BankAccountSummary>>($"/api/banks/{bank.Id}/accounts"))!.Items);
        var filtered = await http.GetFromJsonAsync<PageResult<BankDto>>($"/api/banks?search={bank.Code.ToLowerInvariant()}&sort=code&pageSize=1");
        Assert.Single(filtered!.Items);
        Assert.Equal(HttpStatusCode.Conflict, (await http.DeleteAsync($"/api/banks/{bank.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await http.DeleteAsync($"/api/clients/{client.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await http.PostAsJsonAsync("/api/banks", new BankInput("Doublon", bank.Code.ToLowerInvariant(), ""))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.GetAsync("/api/banks?page=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.GetAsync("/api/banks?sort=invalid")).StatusCode);
        (await http.PutAsJsonAsync($"/api/bank-accounts/{account.Id}", new BankAccountInput(client.Id,bank.Id,"TEST","Compte modifié","EUR","BQ2","512100"))).EnsureSuccessStatusCode();
        Assert.Equal("EUR", (await http.GetFromJsonAsync<BankAccountDto>($"/api/bank-accounts/{account.Id}"))!.Currency);
        var dashboard = (await http.GetFromJsonAsync<DashboardDto>("/api/dashboard"))!;
        Assert.True(dashboard.Accounts > 0 && dashboard.Clients > 0 && dashboard.Banks > 0);

        using var other = factory.CreateClient();
        var email=$"user-{suffix}@example.test";
        (await other.PostAsJsonAsync("/api/auth/register",new RegisterDto(suffix,email,password))).EnsureSuccessStatusCode();
        var otherLogin=await other.PostAsJsonAsync("/api/auth/login",new LoginDto(email,password));
        var otherSession=(await otherLogin.Content.ReadFromJsonAsync<AuthResult>())!;
        Assert.Equal("User",otherSession.User.Role);
        other.DefaultRequestHeaders.Authorization=new("Bearer",otherSession.Token);
        Assert.Equal(HttpStatusCode.Forbidden,(await other.PostAsJsonAsync("/api/banks",new BankInput("Unauthorized","BAD",""))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await other.PutAsJsonAsync($"/api/banks/{bank.Id}",new BankInput("Unauthorized","BAD",""))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await other.DeleteAsync($"/api/banks/{bank.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await other.DeleteAsync($"/api/banks/{bank.Id}/logo")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await other.GetAsync($"/api/catalog-deletions/banks/{bank.Id}")).StatusCode);
        (await other.GetAsync("/api/clients")).EnsureSuccessStatusCode();
        (await other.PutAsJsonAsync($"/api/clients/{client.Id}",clientInput with {Name="Utilisateur autorisé"})).EnsureSuccessStatusCode();

        var path=$"/api/catalog-deletions/clients/{client.Id}";
        var preview=(await other.GetFromJsonAsync<DeletionPreview>(path))!;
        Assert.Equal(1,preview.Accounts);
        (await http.PutAsJsonAsync($"/api/bank-accounts/{account.Id}",new BankAccountInput(client.Id,bank.Id,"TEST","Changement concurrent","MAD","BQ","512000"))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict,(await other.PostAsJsonAsync(path,new {preview.Version})).StatusCode);
        preview=(await other.GetFromJsonAsync<DeletionPreview>(path))!;
        (await other.PostAsJsonAsync(path,new {preview.Version})).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound,(await http.GetAsync($"/api/clients/{client.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await http.GetAsync($"/api/bank-accounts/{account.Id}")).StatusCode);
        (await http.GetAsync($"/api/banks/{bank.Id}")).EnsureSuccessStatusCode();

        var secondResponse=await other.PostAsJsonAsync("/api/clients",clientInput with {ICE=(long.Parse(ice)+1).ToString("D15")});
        secondResponse.EnsureSuccessStatusCode();var second=(await secondResponse.Content.ReadFromJsonAsync<ClientDto>())!;
        (await other.PostAsJsonAsync("/api/bank-accounts",new BankAccountInput(second.Id,bank.Id,"SECOND","Second compte","MAD","BQ","512000"))).EnsureSuccessStatusCode();
        path=$"/api/catalog-deletions/banks/{bank.Id}";
        preview=(await http.GetFromJsonAsync<DeletionPreview>(path))!;
        Assert.Equal(1,preview.Accounts);
        (await http.PostAsJsonAsync(path,new {preview.Version})).EnsureSuccessStatusCode();
        (await other.GetAsync($"/api/clients/{second.Id}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound,(await http.GetAsync($"/api/banks/{bank.Id}")).StatusCode);
        (await other.DeleteAsync($"/api/clients/{second.Id}")).EnsureSuccessStatusCode();

        using var cleanupScope=factory.Services.CreateScope();
        var db=cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Users.Where(x=>x.Id==session.User.Id||x.Id==otherSession.User.Id).ExecuteDeleteAsync();
    }
}
