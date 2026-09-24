using System.Security.Claims;
using BankStatementConverter.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankStatementConverter.API;

[ApiController, Authorize(Roles = "Admin"), Route("api/users")]
public class UsersController(IUserManagementService service) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet] public Task<PageResult<ManagedUserDto>> List(CancellationToken ct, int page = 1, int pageSize = 20, string? search = null, string? role = null, bool? active = null) => service.ListAsync(page, pageSize, search, role, active, ct);
    [HttpGet("permissions")] public object PermissionList() => new { all = Permissions.All, defaults = Permissions.Default };
    [HttpPost] public async Task<IActionResult> Create(SaveUserInput input, CancellationToken ct) => StatusCode(201, await service.SaveAsync(null, input, Actor, ct));
    [HttpPut("{id:guid}")] public Task<ManagedUserDto> Update(Guid id, SaveUserInput input, CancellationToken ct) => service.SaveAsync(id, input, Actor, ct);
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await service.DeleteAsync(id, Actor, ct); return NoContent(); }
    [HttpPut("{id:guid}/password")] public async Task<IActionResult> Password(Guid id, ResetPasswordInput input, CancellationToken ct) { await service.ResetPasswordAsync(id, input.Password, ct); return NoContent(); }
    [HttpPost("{id:guid}/revoke-sessions")] public async Task<IActionResult> Revoke(Guid id, CancellationToken ct) { await service.RevokeSessionsAsync(id, ct); return NoContent(); }
}
