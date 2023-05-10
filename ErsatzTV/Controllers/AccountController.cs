using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers;

[ApiController]
public class AccountController : ControllerBase
{
    [HttpPost("account/logout")]
    public IActionResult Logout() => new SignOutResult(new[] { "oidc", "cookie" });
}
