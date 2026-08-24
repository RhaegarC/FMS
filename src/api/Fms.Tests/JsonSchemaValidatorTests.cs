using Fms.Api.Validation;

namespace Fms.Tests;

/// <summary>
/// RED for feature 06: form definitions must be valid JSON Schema (draft 2020-12).
/// The validator guards both form create and update. Note: JSON Schema is
/// extensible — unknown keywords are allowed; only *invalid values for known
/// keywords* (and non-schema documents) are rejected.
/// </summary>
public class JsonSchemaValidatorTests
{
    private readonly JsonSchemaValidator _validator = new();

    [Fact]
    public void EmptyObject_IsAValidSchema() =>
        Assert.True(_validator.IsValid("{}", out _));

    [Fact]
    public void BasicObjectSchema_IsValid() =>
        Assert.True(_validator.IsValid(
            """{"type":"object","properties":{"name":{"type":"string"}},"required":["name"]}""",
            out _));

    [Fact]
    public void UnknownKeywords_AreAllowed_AsValidJsonSchema() =>
        Assert.True(_validator.IsValid("""{"x-vendor-note":"ok"}""", out _));

    [Fact]
    public void Draft202012Keywords_AreAccepted()
    {
        // $defs / $ref, prefixItems, minContains, unevaluatedProperties are all
        // draft-2020-12 keywords — a schema using them must pass.
        var schema = """
        {
          "$defs": { "positive": { "type": "integer", "minimum": 0 } },
          "$ref": "#/$defs/positive",
          "prefixItems": [ { "type": "string" } ],
          "minContains": 1,
          "unevaluatedProperties": false
        }
        """;
        Assert.True(_validator.IsValid(schema, out _));
    }

    [Theory]
    [InlineData("""{"type":"not-a-real-type"}""")]
    [InlineData("""{"properties": 42}""")]
    [InlineData("""{"required": "name"}""")]
    public void InvalidSchema_IsRejected(string schema) =>
        Assert.False(_validator.IsValid(schema, out _));

    [Theory]
    [InlineData("")]
    [InlineData("{not json")]
    [InlineData("[1,2,3]")]
    [InlineData("\"just a string\"")]
    public void NotASchemaDocument_IsRejected(string schema) =>
        Assert.False(_validator.IsValid(schema, out _));
}
