using Fms.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fms.Api.Controllers;

/// <summary>The authenticated user's own profile — drives the admin/user portals.</summary>
[ApiController]
[Route("api/me")]
[Authorize]
public class MeController : FmsApiControllerBase
{
    [HttpGet]
    public ActionResult<MeResponse> Get()
    {
        var user = CurrentUser;
        return Ok(new MeResponse(user.Id, user.Name, user.Email, user.Role));
    }
}
