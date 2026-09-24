using BankStatementConverter.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BankStatementConverter.API;

[ApiController, Authorize, Route("api/banks/{id:guid}/logo")]
public class BankLogoController(ICatalogImageService service) : ControllerBase
{
    [HttpGet]
    public async Task<ImageDto> Get(Guid id, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        return await service.GetAsync(true, id, ct);
    }
    [HttpPut, RequestSizeLimit(2 * 1024 * 1024 + 65536)]
    public async Task<IActionResult> Put(Guid id, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var version = await service.SaveAsync(true, id, stream, file.Length, ct);
        return Ok(new { logoVersion = version });
    }
    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(true, id, ct);
        return NoContent();
    }
}
