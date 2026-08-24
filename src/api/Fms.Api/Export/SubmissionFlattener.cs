using System.Text.Json;
using System.Text.Json.Nodes;

namespace Fms.Api.Export;

/// <summary>
/// Flattens a submission's JSON data into a single-level column/value table for
/// Excel export (feature 07). Nested objects become dotted keys
/// (<c>address.city</c>), arrays are kept as their JSON text, and scalars become
/// display text (numbers keep their JSON literal, booleans lowercase, null an
/// empty string). Recursion follows document order, so the exporter can union
/// columns deterministically across heterogeneous submissions.
/// </summary>
public static class SubmissionFlattener
{
    /// <summary>Returns the flattened column/value map for <paramref name="dataJson"/>.
    /// A non-object or malformed document yields no columns (defensive — submissions
    /// are schema-validated before storage, so this should not normally happen).</summary>
    public static IReadOnlyDictionary<string, string> Flatten(string dataJson)
    {
        var result = new Dictionary<string, string>();

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(dataJson);
        }
        catch (JsonException)
        {
            return result;
        }

        if (node is not JsonObject obj)
        {
            return result;
        }

        FlattenObject(obj, "", result);
        return result;
    }

    private static void FlattenObject(JsonObject obj, string prefix, Dictionary<string, string> result)
    {
        foreach (var property in obj)
        {
            var path = prefix.Length == 0 ? property.Key : $"{prefix}.{property.Key}";
            switch (property.Value)
            {
                case null:
                    result[path] = "";
                    break;
                case JsonObject nested:
                    FlattenObject(nested, path, result);
                    break;
                case JsonArray:
                    // Keep arrays as their compact JSON text — a single column cell.
                    result[path] = property.Value.ToJsonString();
                    break;
                case JsonValue scalar:
                    result[path] = ScalarText(scalar);
                    break;
                default:
                    result[path] = "";
                    break;
            }
        }
    }

    private static string ScalarText(JsonValue value) => value.GetValueKind() switch
    {
        JsonValueKind.String => value.GetValue<string>(),
        JsonValueKind.Number => value.ToJsonString(), // preserves the JSON literal (3.14, -5, 1e3)
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => "",
    };
}
