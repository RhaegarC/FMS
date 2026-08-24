using Fms.Api.Validation;

namespace Fms.Tests;

/// <summary>
/// RED for feature 07: a submitted payload must be validated against the target
/// form's schema (draft 2020-12) before it is stored. This is a security-relevant
/// hot spot — a form's schema is the contract for what its submissions may contain.
/// The same validator that guards form definitions (<see cref="JsonSchemaValidator"/>
/// IsValid) gains a ValidateInstance method that evaluates an instance document.
/// </summary>
public class SubmissionValidationTests
{
    private readonly JsonSchemaValidator _validator = new();

    private const string ExpenseSchema =
        """{"type":"object","properties":{"amount":{"type":"number"},"vendor":{"type":"string"}},"required":["amount","vendor"]}""";

    [Fact]
    public void DataMatchingSchema_IsValid() =>
        Assert.True(_validator.ValidateInstance(ExpenseSchema, """{"amount":42.5,"vendor":"Acme"}""", out _));

    [Fact]
    public void DataWithOnlyRequiredFields_IsValid() =>
        Assert.True(_validator.ValidateInstance(ExpenseSchema, """{"amount":1,"vendor":"x"}""", out _));

    [Fact]
    public void MissingRequiredField_IsInvalid()
    {
        Assert.False(_validator.ValidateInstance(ExpenseSchema, """{"amount":42}""", out var error));
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void WrongTypeForProperty_IsInvalid() =>
        Assert.False(_validator.ValidateInstance(ExpenseSchema, """{"amount":"lots","vendor":"Acme"}""", out _));

    [Fact]
    public void NonObjectInstance_IsInvalid() =>
        Assert.False(_validator.ValidateInstance(ExpenseSchema, """[1,2,3]""", out _));

    [Fact]
    public void MalformedInstanceJson_IsInvalid() =>
        Assert.False(_validator.ValidateInstance(ExpenseSchema, "{not json", out _));

    [Fact]
    public void MalformedSchemaJson_IsInvalid_NotThrows()
    {
        // A stored schema is always validated on create/update, but ValidateInstance
        // must degrade gracefully (deny) if it ever meets a malformed definition.
        Assert.False(_validator.ValidateInstance("{bad schema", """{"amount":1,"vendor":"x"}""", out _));
    }

    [Fact]
    public void EnumViolation_IsInvalid()
    {
        const string enumSchema =
            """{"type":"object","properties":{"status":{"type":"string","enum":["open","closed"]}},"required":["status"]}""";
        Assert.False(_validator.ValidateInstance(enumSchema, """{"status":"archived"}""", out _));
    }

    [Fact]
    public void EnumMember_IsValid()
    {
        const string enumSchema =
            """{"type":"object","properties":{"status":{"type":"string","enum":["open","closed"]}},"required":["status"]}""";
        Assert.True(_validator.ValidateInstance(enumSchema, """{"status":"open"}""", out _));
    }

    [Fact]
    public void EmptySchema_IsInvalid() =>
        Assert.False(_validator.ValidateInstance("", """{"a":1}""", out _));
}
