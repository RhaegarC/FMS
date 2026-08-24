using Fms.Api.Export;

namespace Fms.Tests;

/// <summary>
/// RED for feature 07: Excel export needs submission data as a flat column/value
/// table. Nested objects become dotted keys (<c>address.city</c>), arrays are
/// serialized as their JSON text, and scalars become display text — so a row of
/// heterogeneous submissions can share columns.
/// </summary>
public class SubmissionFlattenerTests
{
    [Fact]
    public void FlatObject_ProducesScalarColumns()
    {
        var flat = SubmissionFlattener.Flatten("""{"name":"Alice","age":30}""");
        Assert.Equal("Alice", flat["name"]);
        Assert.Equal("30", flat["age"]);
    }

    [Fact]
    public void NestedObject_UsesDottedKeys()
    {
        var flat = SubmissionFlattener.Flatten(
            """{"name":"Alice","address":{"city":"New York","zip":"10001"}}""");
        Assert.Equal("New York", flat["address.city"]);
        Assert.Equal("10001", flat["address.zip"]);
        Assert.Equal("Alice", flat["name"]);
    }

    [Fact]
    public void DeeplyNestedObject_UsesFullDottedPath()
    {
        var flat = SubmissionFlattener.Flatten(
            """{"a":{"b":{"c":1}}}""");
        Assert.Equal("1", flat["a.b.c"]);
    }

    [Fact]
    public void Array_IsSerializedAsJsonText()
    {
        var flat = SubmissionFlattener.Flatten("""{"tags":["a","b"]}""");
        Assert.Equal("""["a","b"]""", flat["tags"]);
    }

    [Fact]
    public void EmptyArray_IsSerializedAsEmptyJsonArray()
    {
        var flat = SubmissionFlattener.Flatten("""{"tags":[]}""");
        Assert.Equal("[]", flat["tags"]);
    }

    [Fact]
    public void Boolean_BecomesLowercaseText()
    {
        var flat = SubmissionFlattener.Flatten("""{"active":true}""");
        Assert.Equal("true", flat["active"]);
    }

    [Fact]
    public void Null_BecomesEmptyString()
    {
        var flat = SubmissionFlattener.Flatten("""{"note":null,"name":"x"}""");
        Assert.Equal("", flat["note"]);
    }

    [Fact]
    public void Decimal_KeepsJsonLiteral()
    {
        var flat = SubmissionFlattener.Flatten("""{"pi":3.14}""");
        Assert.Equal("3.14", flat["pi"]);
    }

    [Fact]
    public void EmptyObject_ProducesNoColumns()
    {
        Assert.Empty(SubmissionFlattener.Flatten("{}"));
    }

    [Fact]
    public void NonObjectInstance_ProducesNoColumns()
    {
        Assert.Empty(SubmissionFlattener.Flatten("[1,2,3]"));
    }

    [Fact]
    public void MalformedJson_ProducesNoColumns()
    {
        Assert.Empty(SubmissionFlattener.Flatten("{not json"));
    }
}
