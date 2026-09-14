namespace Fms.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Fms.Interface.Service;
using Fms.Model.DatabaseEntity;

[ApiController]
[Route("[controller]")]
public class UserController(IUserService userService) : Controller
{
    private readonly IUserService _userService = userService;

    [HttpGet("index")]
    public IActionResult Index()
    {
        return Ok("Welcome");
    }

    /// <summary>
    /// The caller's own record, provisioned on their first call.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        User? user = await _userService.GetOrCreateAsync();

        // Authenticated, but the token carries no object id to identify the caller by.
        // That is not a missing token, so there is no challenge to issue and no record
        // to return.
        return user is null ? Unauthorized() : Ok(user);
    }
}
