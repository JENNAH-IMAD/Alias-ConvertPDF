using BankStatementConverter.Application;
using BankStatementConverter.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BankStatementConverter.API;

[ApiController, Authorize]
public abstract class CatalogController<TEntity,TInput,TDto>(ICatalogService<TInput,TDto> service) : ControllerBase where TEntity : Entity,new()
{
    [HttpGet] public Task<PageResult<TDto>> List(CancellationToken ct, int page = 1, int pageSize = 50) => service.ListAsync(page, pageSize, ct);
    [HttpGet("{id:guid}")] public Task<TDto> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);
    [HttpPost] public virtual async Task<ActionResult<TDto>> Create(TInput input, CancellationToken ct) => StatusCode(201, await service.SaveAsync(null, input, ct));
    [HttpPut("{id:guid}")] public virtual Task<TDto> Update(Guid id, TInput input, CancellationToken ct) => service.SaveAsync(id, input, ct);
    [HttpDelete("{id:guid}")] public virtual async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await service.DeleteAsync(id, ct); return NoContent(); }
}
[Route("api/clients")] public class ClientsController(ICatalogService<ClientInput,ClientDto> s) : CatalogController<Client,ClientInput,ClientDto>(s);
[ApiController, Authorize, Route("api/banks")]
public class BanksController(IBankService service) : ControllerBase
{
    [HttpGet] public Task<PageResult<BankDto>> List(CancellationToken ct, int page = 1, int pageSize = 20, string? search = null, string sort = "name") => service.ListAsync(page, pageSize, search, sort, ct);
    [HttpGet("{id:guid}")] public Task<BankDto> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);
    [HttpPost] public async Task<ActionResult<BankDto>> Create(BankInput input, CancellationToken ct)
    { var result = await service.SaveAsync(null, input, ct); return CreatedAtAction(nameof(Get), new { id = result.Id }, result); }
    [HttpPut("{id:guid}")] public Task<BankDto> Update(Guid id, BankInput input, CancellationToken ct) => service.SaveAsync(id, input, ct);
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await service.DeleteAsync(id, ct); return NoContent(); }
    [HttpGet("{id:guid}/accounts")] public Task<PageResult<BankAccountSummary>> Accounts(Guid id, CancellationToken ct, int page = 1, int pageSize = 20) => service.AccountsAsync(id, page, pageSize, ct);
}
[Route("api/bank-accounts")] public class AccountsController(ICatalogService<BankAccountInput,BankAccountDto> s) : CatalogController<BankAccount,BankAccountInput,BankAccountDto>(s);
