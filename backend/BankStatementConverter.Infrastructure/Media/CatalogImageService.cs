using BankStatementConverter.Application;
using Microsoft.EntityFrameworkCore;

namespace BankStatementConverter.Infrastructure;

public class CatalogImageService(AppDbContext db) : ICatalogImageService
{
    public const int MaxBytes = 2 * 1024 * 1024;

    public async Task<ImageDto> GetAsync(bool bank, Guid id, CancellationToken ct)
    {
        byte[]? bytes;
        string? contentType;
        if (bank)
        {
            var item = await db.Banks.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new { x.Logo, x.LogoContentType }).SingleOrDefaultAsync(ct)
                ?? throw new AppException(404, "Banque introuvable.");
            bytes = item.Logo; contentType = item.LogoContentType;
        }
        else
        {
            var item = await db.Clients.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new { x.Photo, x.PhotoContentType }).SingleOrDefaultAsync(ct)
                ?? throw new AppException(404, "Client introuvable.");
            bytes = item.Photo; contentType = item.PhotoContentType;
        }
        return new(bytes is null ? null : $"data:{contentType};base64,{Convert.ToBase64String(bytes)}");
    }

    public async Task<Guid> SaveAsync(bool bank, Guid id, Stream input, long length, CancellationToken ct)
    {
        Validation.Require(length is > 0 and <= MaxBytes, "Choisissez une image PNG ou JPEG de 2 Mo maximum.");
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int count;
        while ((count = await input.ReadAsync(chunk, ct)) > 0)
        {
            Validation.Require(buffer.Length + count <= MaxBytes, "Image supérieure à 2 Mo.");
            await buffer.WriteAsync(chunk.AsMemory(0, count), ct);
        }
        var bytes = buffer.ToArray();
        var png = bytes.Length >= 33 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) && bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8);
        var jpeg = bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 && bytes[^2] == 255 && bytes[^1] == 217;
        Validation.Require(png || jpeg, "Format non pris en charge. Utilisez une image PNG ou JPEG.");
        var contentType = png ? "image/png" : "image/jpeg";
        var version = Guid.NewGuid();
        if (bank)
        {
            var item = await db.Banks.FindAsync([id], ct) ?? throw new AppException(404, "Banque introuvable.");
            item.Logo = bytes; item.LogoContentType = contentType; item.LogoVersion = version;
        }
        else
        {
            var item = await db.Clients.FindAsync([id], ct) ?? throw new AppException(404, "Client introuvable.");
            item.Photo = bytes; item.PhotoContentType = contentType; item.PhotoVersion = version;
        }
        await db.SaveChangesAsync(ct);
        return version;
    }

    public async Task DeleteAsync(bool bank, Guid id, CancellationToken ct)
    {
        if (bank)
        {
            var item = await db.Banks.FindAsync([id], ct) ?? throw new AppException(404, "Banque introuvable.");
            item.Logo = null; item.LogoContentType = null; item.LogoVersion = null;
        }
        else
        {
            var item = await db.Clients.FindAsync([id], ct) ?? throw new AppException(404, "Client introuvable.");
            item.Photo = null; item.PhotoContentType = null; item.PhotoVersion = null;
        }
        await db.SaveChangesAsync(ct);
    }
}
