using Fms.Api.Contracts;
using Fms.Interface.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fms.Api.Controllers;

/// <summary>Form update/delete (feature 06). Admin-only, like the rest of the
/// catalog mutations; creation lives under a space (SpaceController).</summary>
[ApiController]
[Route("api/forms")]
[Authorize]
public class FormController(ICatalogService catalog) : FmsApiControllerBase
{
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<FormDto>> Update(string id, [FromBody] UpdateFormRequest request)
    {
        var form = await catalog.UpdateFormAsync(id, request.Name, request.Schema);
        return Ok(DtoMapper.ToFormDto(form));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(string id)
    {
        await catalog.DeleteFormAsync(id);
        return NoContent();
    }
}
