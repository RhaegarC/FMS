using System.Text.Json;
using Fms.Interface.Service;
using Json.Schema;

namespace Fms.Service.Validation;

/// <summary>
/// Validates that a form definition is standard JSON Schema (draft 2020-12) —
/// the guard for form create/update (feature 06). JSON Schema is extensible, so
/// unknown keywords are allowed; only invalid values for known keywords and
/// non-object documents are rejected. Stateless — registered as a singleton.
/// </summary>
public sealed class JsonSchemaValidator : IJsonSchemaValidator
{
    private static readonly JsonSchema MetaSchema = MetaSchemas.Draft202012;

    /// <summary>Returns true iff <paramref name="schemaJson"/> is a JSON object that
    /// conforms to the draft-2020-12 meta-schema. <paramref name="error"/> carries a
    /// human-readable reason when it does not.</summary>
    public bool IsValid(string schemaJson, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(schemaJson))
        {
            error = "Schema is empty.";
            return false;
        }

        JsonElement root;
        try
        {
            using var doc = JsonDocument.Parse(schemaJson);
            root = doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            error = $"Schema is not valid JSON: {ex.Message}";
            return false;
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            error = "Schema must be a JSON object.";
            return false;
        }

        if (!MetaSchema.Evaluate(root).IsValid)
        {
            error = "Schema does not conform to JSON Schema draft 2020-12.";
            return false;
        }

        return true;
    }

    /// <summary>Returns true iff <paramref name="dataJson"/> conforms to the schema
    /// in <paramref name="schemaJson"/> (feature 07 — the submit-time guard).
    /// A malformed or non-object schema is treated as deny (fail closed), mirroring
    /// <see cref="IsValid"/>'s contract. <paramref name="error"/> carries a
    /// human-readable reason when validation fails.</summary>
    public bool ValidateInstance(string schemaJson, string dataJson, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(schemaJson))
        {
            error = "Form has no schema.";
            return false;
        }

        if (!IsValid(schemaJson, out var schemaError))
        {
            error = $"Form schema is not valid JSON Schema (draft 2020-12): {schemaError}";
            return false;
        }

        JsonElement data;
        try
        {
            using var doc = JsonDocument.Parse(dataJson);
            data = doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            error = $"Payload is not valid JSON: {ex.Message}";
            return false;
        }

        if (data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            error = "Payload is empty.";
            return false;
        }

        var schema = JsonSchema.FromText(schemaJson);
        var result = schema.Evaluate(data);
        if (result.IsValid)
        {
            return true;
        }

        // Surface the first failing instance location (JSON pointer) for a
        // human-readable rejection message; fall back to a generic reason.
        var firstError = result.Details?.FirstOrDefault(d => !d.IsValid);
        error = firstError?.InstanceLocation is { } location
            ? $"Payload does not match the form schema at {location}."
            : "Payload does not match the form schema.";
        return false;
    }
}
