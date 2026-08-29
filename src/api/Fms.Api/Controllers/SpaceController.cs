using Fms.Api.Contracts;
using Fms.Interface.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fms.Api.Controllers;

/// <summary>
/// Space catalog (feature 06). Mutations are admin-only (the AdminOnly policy reads
/// the DB-stored role from the provisioned user); reads are permission-scoped for
/// non-admins inside CatalogService. A space grant covers every form under that
/// space; these endpoints are thin mapping/status-code adapters.
/// </summary>
[ApiController]
[Route("api/spaces")]
[Authorize]
public class SpaceController(ICatalogService catalog) : FmsApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SpaceDto>>> List()
    {
        var spaces = await catalog.ListSpacesAsync(CurrentUser);
        return Ok(spaces.Select(s => new SpaceDto(s.Id, s.Name)));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<SpaceDto>> Create([FromBody] CreateSpaceRequest request)
    {
        var space = await catalog.CreateSpaceAsync(request.Name);
        return Created($"/api/spaces/{space.Id}", new SpaceDto(space.Id, space.Name));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<SpaceDto>> Update(int id, [FromBody] UpdateSpaceRequest request)
    {
        var space = await catalog.UpdateSpaceAsync(id, request.Name);
        return Ok(new SpaceDto(space.Id, space.Name));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        await catalog.DeleteSpaceAsync(id);
        return NoContent();
    }

    [HttpGet("{spaceId:int}/forms")]
    public async Task<ActionResult<IEnumerable<FormDto>>> ListFormsInSpace(int spaceId)
    {
        var forms = await catalog.ListFormsInSpaceAsync(spaceId, CurrentUser);
        return Ok(forms.Select(DtoMapper.ToFormDto));
    }

    [HttpPost("{spaceId:int}/forms")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<FormDto>> CreateForm(int spaceId, [FromBody] CreateFormRequest request)
    {
        var form = await catalog.CreateFormAsync(spaceId, request.Name, request.Schema);
        return Created($"/api/forms/{form.Id}", DtoMapper.ToFormDto(form));
    }
}
