using BankStatementConverter.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BankStatementConverter.API;
[ApiController, Authorize, Route("api/dashboard")]
public class DashboardController(IDashboardService service) : ControllerBase
{
    [HttpGet] public Task<DashboardDto> Get(CancellationToken ct) => service.GetAsync(ct);
}
