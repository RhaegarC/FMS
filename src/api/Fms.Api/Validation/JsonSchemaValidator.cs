using System.Text.Json;
using Json.Schema;

namespace Fms.Api.Validation;

/// <summary>
/// Validates that a form definition is standard JSON Schema (draft 2020-12) —
/// the guard for form create/update (feature 06). JSON Schema is extensible, so
/// unknown keywords are allowed; only invalid values for known keywords and
/// non-object documents are rejected.
/// </summary>
public sealed class JsonSchemaValidator
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
}
