using BankStatementConverter.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BankStatementConverter.API;

[ApiController, Authorize, Route("api/clients/{id:guid}/photo")]
public class ClientPhotoController(ICatalogImageService service) : ControllerBase
{
    [HttpGet]
    public async Task<ImageDto> Get(Guid id, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        return await service.GetAsync(false, id, ct);
    }
    [HttpPut, RequestSizeLimit(2 * 1024 * 1024 + 65536)]
    public async Task<IActionResult> Put(Guid id, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var version = await service.SaveAsync(false, id, stream, file.Length, ct);
        return Ok(new { photoVersion = version });
    }
    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(false, id, ct);
        return NoContent();
    }
}
