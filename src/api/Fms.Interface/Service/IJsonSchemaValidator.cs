namespace Fms.Interface.Service;

/// <summary>Validates form definitions and submission payloads against JSON Schema
/// draft 2020-12 (features 06/07).</summary>
public interface IJsonSchemaValidator
{
    bool IsValid(string schemaJson, out string? error);

    bool ValidateInstance(string schemaJson, string dataJson, out string? error);
}
