using BankStatementConverter.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BankStatementConverter.API;
public record ConfirmCatalogDeletion(string Version);
[ApiController,Authorize,Route("api/catalog-deletions/{resource}/{id:guid}")]
public class CatalogDeletionController(ICatalogDeletionService service):ControllerBase
{
    [HttpGet] public async Task<IActionResult> Preview(string resource,Guid id,CancellationToken ct)
    {
        return Ok(await service.PreviewAsync(resource,id,ct));
    }
    [HttpPost] public async Task<IActionResult> Delete(string resource,Guid id,ConfirmCatalogDeletion input,CancellationToken ct)
    {
        await service.DeleteAsync(resource,id,input.Version,ct);return NoContent();
    }
}
