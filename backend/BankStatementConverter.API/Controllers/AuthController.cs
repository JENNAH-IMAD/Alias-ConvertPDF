using BankStatementConverter.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace BankStatementConverter.API;

[ApiController, Route("api/auth"), EnableRateLimiting("auth")]
public class AuthController(IAuthenticationService auth) : ControllerBase
{
    [HttpPost("login")] public Task<AuthResult> Login(LoginDto input, CancellationToken ct) => auth.LoginAsync(input, ct);
    [HttpPost("register")] public async Task<IActionResult> Register(RegisterDto input, CancellationToken ct) => StatusCode(201, await auth.RegisterAsync(input, ct));
}
