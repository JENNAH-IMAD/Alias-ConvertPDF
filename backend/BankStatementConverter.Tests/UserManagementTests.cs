using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using BankStatementConverter.Application;
using BankStatementConverter.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace BankStatementConverter.Tests;

public class UserManagementTests
{
    [Fact, Trait("Category", "Integration")]
    public async Task AdministrationEnforcesPermissionsAndRevokesSessions()
    {
        var connection = Environment.GetEnvironmentVariable("TEST_DATABASE_URL");
        Assert.False(string.IsNullOrEmpty(connection));
        var config = new NpgsqlConnectionStringBuilder(connection!);
        var database = "users_test_" + Guid.NewGuid().ToString("N");
        await using var control = new NpgsqlConnection(connection);
        await control.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", control)) await create.ExecuteNonQueryAsync();
        config.Database = database;
        var adminEmail = "admin@example.test";
        var password = "ValidPassword123!";
        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__Default", config.ConnectionString);
            Environment.SetEnvironmentVariable("Jwt__Key", Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
            Environment.SetEnvironmentVariable("Seed__AdminEmail", adminEmail);
            Environment.SetEnvironmentVariable("Seed__AdminPassword", password);
            Environment.SetEnvironmentVariable("Database__AutoMigrate", "true");
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.ConfigureServices(s => s.AddDataProtection().UseEphemeralDataProtectionProvider()));
            using var admin = factory.CreateClient();
            async Task<AuthResult> Login(HttpClient client, string email, string pass)
            {
                var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto(email, pass));
                response.EnsureSuccessStatusCode();
                var auth = (await response.Content.ReadFromJsonAsync<AuthResult>())!;
                client.DefaultRequestHeaders.Authorization = new("Bearer", auth.Token);
                return auth;
            }
            Assert.Equal(HttpStatusCode.Unauthorized, (await admin.GetAsync("/api/users")).StatusCode);
            var adminSession = await Login(admin, adminEmail, password);
            Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/users/{adminSession.User.Id}")).StatusCode);
            // The last-administrator invariant is enforced inside the transaction, independently of the UI.
            using (var scope = factory.Services.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IUserManagementService>();
                var exception = await Assert.ThrowsAsync<AppException>(() => service.DeleteAsync(adminSession.User.Id, Guid.NewGuid(), default));
                Assert.Equal(409, exception.Status);
                Assert.Contains("administrateur actif", exception.Message);
            }
            var input = new SaveUserInput("Lecteur", "reader@example.test", "User", true, ["clients.read"], password, null);
            var createResponse = await admin.PostAsJsonAsync("/api/users", input); createResponse.EnsureSuccessStatusCode();
            var managed = (await createResponse.Content.ReadFromJsonAsync<ManagedUserDto>())!;
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/users", input)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/users", input with { Name="Invalid", Email="invalid@example.test", Permissions=["users.admin"] })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/users", input with { Name="Invalid", Email="invalid@example.test", Permissions=["clients.delete"] })).StatusCode);
            using var reader = factory.CreateClient();
            var oldSession = await Login(reader, input.Email, password);
            Assert.Equal(["clients.read"], oldSession.User.Permissions!);
            Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync("/api/users")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync("/api/users/permissions")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostAsJsonAsync("/api/users", input)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await reader.PutAsJsonAsync($"/api/users/{managed.Id}", input)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await reader.DeleteAsync($"/api/users/{managed.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await reader.PutAsJsonAsync($"/api/users/{managed.Id}/password", new ResetPasswordInput(password))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostAsync($"/api/users/{managed.Id}/revoke-sessions", null)).StatusCode);
            (await reader.GetAsync("/api/clients")).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync("/api/banks")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync("/api/bank-accounts")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync("/api/dashboard")).StatusCode);
            var clientInput = new ClientInput("Client", "Société", "123456789012345", "IF", "RC", "client@example.test", "+212600000000");
            Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostAsJsonAsync("/api/clients", clientInput)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await reader.DeleteAsync($"/api/clients/{Guid.NewGuid()}/photo")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync($"/api/catalog-deletions/clients/{Guid.NewGuid()}")).StatusCode);
            var version = managed.UpdatedAt;
            async Task Update(SaveUserInput next)
            {
                var response = await admin.PutAsJsonAsync($"/api/users/{managed.Id}", next with {Version=managed.UpdatedAt});
                Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
                managed = (await response.Content.ReadFromJsonAsync<ManagedUserDto>())!;
            }
            await Update(input with { Permissions=["clients.read", "clients.write", "banks.read", "banks.write"] });
            Assert.Equal(HttpStatusCode.Unauthorized, (await reader.GetAsync("/api/clients")).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/users/{managed.Id}", input with {Version=version})).StatusCode);
            await Login(reader, input.Email, password);
            (await reader.PostAsJsonAsync("/api/clients", clientInput)).EnsureSuccessStatusCode();
            (await reader.PostAsJsonAsync("/api/banks", new BankInput("Banque", "BANK", ""))).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Forbidden, (await reader.DeleteAsync($"/api/banks/{Guid.NewGuid()}")).StatusCode);
            await Update(input with {Role="Admin", Permissions=Permissions.All});
            Assert.Equal(HttpStatusCode.Unauthorized, (await reader.GetAsync("/api/users")).StatusCode);
            await Login(reader, input.Email, password);
            (await reader.GetAsync("/api/users")).EnsureSuccessStatusCode();
            await Update(input);
            Assert.Equal(HttpStatusCode.Unauthorized, (await reader.GetAsync("/api/users")).StatusCode);
            await Login(reader, input.Email, password);
            Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync("/api/users")).StatusCode);
            await Update(input with {IsActive=false});
            Assert.Equal(HttpStatusCode.Unauthorized, (await reader.GetAsync("/api/auth/me")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await reader.PostAsJsonAsync("/api/auth/login", new LoginDto(input.Email,password))).StatusCode);
            await Update(input);
            await Login(reader, input.Email, password);
            (await admin.PutAsJsonAsync($"/api/users/{managed.Id}/password", new ResetPasswordInput("NewPassword123!"))).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Unauthorized, (await reader.GetAsync("/api/auth/me")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await reader.PostAsJsonAsync("/api/auth/login", new LoginDto(input.Email,password))).StatusCode);
            await Login(reader, input.Email, "NewPassword123!");
            (await admin.PostAsync($"/api/users/{managed.Id}/revoke-sessions", null)).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Unauthorized, (await reader.GetAsync("/api/auth/me")).StatusCode);
            await Login(reader, input.Email, "NewPassword123!");
            (await admin.DeleteAsync($"/api/users/{managed.Id}")).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Unauthorized, (await reader.GetAsync("/api/auth/me")).StatusCode);
            Assert.Single((await admin.GetFromJsonAsync<PageResult<ManagedUserDto>>("/api/users?role=Admin&active=true"))!.Items);
            using var checkScope = factory.Services.CreateScope();
            var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(1, await db.Clients.CountAsync()); Assert.Equal(1, await db.Banks.CountAsync());
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", control);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
