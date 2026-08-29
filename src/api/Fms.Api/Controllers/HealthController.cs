using Fms.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Fms.Api.Controllers;

/// <summary>Anonymous liveness probe — orchestration checks it before auth is up.</summary>
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<HealthStatus> Get() => Ok(new HealthStatus("Fms.Api", "ok"));
}
