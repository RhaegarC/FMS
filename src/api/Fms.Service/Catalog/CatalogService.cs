using Fms.Interface.Repository;
using Fms.Interface.Service;
using Fms.Model.Entities;
using Fms.Service.Exceptions;
using Fms.Service.AccessControl;

namespace Fms.Service.Catalog;

/// <summary>Space/form catalog use cases (feature 06): admin mutations, permission-scoped
/// reads for non-admins, JSON Schema validation on form create/update. Expresses rules
/// as domain exceptions; the Api layer maps them to HTTP status codes.</summary>
public sealed class CatalogService(
    ISpaceRepository spaces,
    IFormRepository forms,
    IPermissionRepository permissions,
    IPermissionEvaluator evaluator,
    IJsonSchemaValidator schemaValidator) : ICatalogService
{
    public async Task<List<Space>> ListSpacesAsync(User user, CancellationToken cancellationToken = default)
    {
        var all = await spaces.ListAllOrderedAsync(cancellationToken);
        if (IsAdmin(user))
        {
            return all;
        }

        var grants = await permissions.ListAllAsync(cancellationToken);
        var subject = PermissionEvaluator.ToSubject(user);
        return all.Where(s => evaluator.CanAccessSpace(subject, grants, s.Id)).ToList();
    }

    public async Task<Space> CreateSpaceAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Space name is required.");
        }

        var space = new Space { Name = name.Trim() };
        spaces.Add(space);
        await spaces.SaveChangesAsync(cancellationToken);
        return space;
    }

    public async Task<Space> UpdateSpaceAsync(string id, string name, CancellationToken cancellationToken = default)
    {
        var space = await spaces.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Space {id} does not exist.");
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Space name is required.");
        }

        space.Name = name.Trim();
        await spaces.SaveChangesAsync(cancellationToken);
        return space;
    }

    public async Task DeleteSpaceAsync(string id, CancellationToken cancellationToken = default)
    {
        var space = await spaces.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Space {id} does not exist.");
        spaces.Remove(space); // cascades to forms (and their submissions)
        await spaces.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Form>> ListFormsInSpaceAsync(string spaceId, User user, CancellationToken cancellationToken = default)
    {
        if (!await spaces.ExistsAsync(spaceId, cancellationToken))
        {
            throw new NotFoundException($"Space {spaceId} does not exist.");
        }

        var all = await forms.ListBySpaceOrderedAsync(spaceId, cancellationToken);
        if (IsAdmin(user))
        {
            return all;
        }

        var grants = await permissions.ListAllAsync(cancellationToken);
        var subject = PermissionEvaluator.ToSubject(user);
        return all.Where(f => evaluator.CanAccessForm(subject, grants, f.Id, f.SpaceId)).ToList();
    }

    public async Task<Form> CreateFormAsync(string spaceId, string name, string schema, CancellationToken cancellationToken = default)
    {
        if (!await spaces.ExistsAsync(spaceId, cancellationToken))
        {
            throw new NotFoundException($"Space {spaceId} does not exist.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Form name is required.");
        }

        if (!schemaValidator.IsValid(schema, out var schemaError))
        {
            throw new ValidationException($"Invalid JSON Schema (draft 2020-12): {schemaError}");
        }

        var form = new Form
        {
            SpaceId = spaceId,
            Name = name.Trim(),
            Schema = schema,
        };
        forms.Add(form);
        await forms.SaveChangesAsync(cancellationToken);
        return form;
    }

    public async Task<Form> UpdateFormAsync(string id, string name, string schema, CancellationToken cancellationToken = default)
    {
        var form = await forms.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Form {id} does not exist.");

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Form name is required.");
        }

        if (!schemaValidator.IsValid(schema, out var schemaError))
        {
            throw new ValidationException($"Invalid JSON Schema (draft 2020-12): {schemaError}");
        }

        form.Name = name.Trim();
        form.Schema = schema;
        await forms.SaveChangesAsync(cancellationToken);
        return form;
    }

    public async Task DeleteFormAsync(string id, CancellationToken cancellationToken = default)
    {
        var form = await forms.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Form {id} does not exist.");
        forms.Remove(form); // cascades to its submissions
        await forms.SaveChangesAsync(cancellationToken);
    }

    private static bool IsAdmin(User user) => string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase);
}
