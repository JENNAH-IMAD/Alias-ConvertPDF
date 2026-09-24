using System.Security.Claims;
using BankStatementConverter.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankStatementConverter.API;

[ApiController, Authorize, Route("api/auth/me")]
public class SessionController(IUserManagementService service) : ControllerBase
{
    [HttpGet] public Task<UserDto> Get(CancellationToken ct) => service.GetSessionUserAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct);
}
