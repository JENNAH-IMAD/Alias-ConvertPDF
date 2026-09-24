using System.Security.Cryptography;
using BankStatementConverter.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
namespace BankStatementConverter.Infrastructure;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string root;
    public LocalFileStorageService(IConfiguration config, IHostEnvironment environment)
    {
        var configured = config["Statements:StoragePath"];
        root = Path.GetFullPath(string.IsNullOrWhiteSpace(configured) ? Path.Combine(environment.ContentRootPath, "private-statements") : configured);
        Directory.CreateDirectory(root);
    }
    private string Resolve(string key)
    {
        if (!Guid.TryParseExact(key, "N", out _)) throw new AppException(400, "Clé de fichier invalide.");
        return Path.Combine(root, key);
    }
    public async Task<StoredFile> SaveAsync(byte[] bytes, CancellationToken ct)
    {
        var key = Guid.NewGuid().ToString("N");
        await File.WriteAllBytesAsync(Resolve(key), bytes, ct);
        return new(key, Convert.ToHexString(SHA256.HashData(bytes)), bytes.LongLength);
    }
    public async Task<byte[]> ReadAsync(string key, CancellationToken ct)
    {
        var path = Resolve(key);
        if (!File.Exists(path)) throw new AppException(404, "Fichier archivé indisponible.");
        return await File.ReadAllBytesAsync(path, ct);
    }
    public Task DeleteAsync(string key, CancellationToken ct) { File.Delete(Resolve(key)); return Task.CompletedTask; }
}
