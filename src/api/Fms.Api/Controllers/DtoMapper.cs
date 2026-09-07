using Fms.Api.Contracts;
using Fms.Model.Entities;

namespace Fms.Api.Controllers;

/// <summary>Entity → DTO mappings shared by the controllers (thin adapters; the
/// CatalogService/SubmissionService return entities, the API exposes DTOs).</summary>
internal static class DtoMapper
{
    public static FormDto ToFormDto(Form f) => new(f.Id, f.SpaceId, f.Name, f.Schema, f.LastModifiedOn);

    public static SubmissionDto ToDto(Submission s) =>
        new(s.Id, s.FormId, s.UserId, s.User.Email, s.Data, s.CreatedOn);
}
