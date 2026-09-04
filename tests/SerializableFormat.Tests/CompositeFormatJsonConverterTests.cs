using System.Text.Json;
using Xunit;
using SutCompositeFormat = global::SerializableFormat.SerializableCompositeFormat;
using SutCompositeFormatJsonConverter = global::SerializableFormat.Json.CompositeFormatJsonConverter;

namespace SerializableFormat.Tests;

public sealed class CompositeFormatJsonConverterTests
{
    private static readonly JsonSerializerOptions s_options = CreateOptions();

    [Fact]
    public void Serialize_WritesStableWireShape()
    {
        var value = SutCompositeFormat.Parse(
            "prefix {{escaped}} {2,-8:X2} / {0:yyyy-MM-dd} suffix");

        var json = JsonSerializer.Serialize(value, s_options);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(
            ["Format", "_segments", "_literalLength", "_formattedCount", "_argsRequired"],
            root.EnumerateObject().Select(property => property.Name));
        Assert.Equal(value.Format, root.GetProperty("Format").GetString());
        Assert.Equal(27, root.GetProperty("_literalLength").GetInt32());
        Assert.Equal(2, root.GetProperty("_formattedCount").GetInt32());
        Assert.Equal(3, root.GetProperty("_argsRequired").GetInt32());

        var segments = root.GetProperty("_segments").EnumerateArray().ToArray();
        Assert.Equal(5, segments.Length);
        AssertJsonSegment(segments[0], "prefix {escaped} ", -1, 0, null);
        AssertJsonSegment(segments[1], null, 2, -8, "X2");
        AssertJsonSegment(segments[2], " / ", -1, 0, null);
        AssertJsonSegment(segments[3], null, 0, 0, "yyyy-MM-dd");
        AssertJsonSegment(segments[4], " suffix", -1, 0, null);
    }

    [Theory]
    [MemberData(nameof(CompositeFormatTestData.RoundTripFormats), MemberType = typeof(CompositeFormatTestData))]
    public void SerializeThenDeserialize_PreservesEveryInternalField(string format)
    {
        var expected = SutCompositeFormat.Parse(format);

        var json = JsonSerializer.Serialize(expected, s_options);
        var actual = JsonSerializer.Deserialize<SutCompositeFormat>(json, s_options);

        Assert.NotNull(actual);
        CompositeFormatAssert.Equal(expected, actual);
    }

    [Fact]
    public void Deserialize_DoesNotReparseFormat_AndIgnoresUnknownProperties()
    {
        const string Json = """
            {
              "UnknownTopLevel": { "nested": [1, 2, 3] },
              "Format": "{",
              "_segments": [
                ["payload", -1, 0, null, { "ignored": true }]
              ],
              "_literalLength": 7,
              "_formattedCount": 0,
              "_argsRequired": 0
            }
            """;

        var actual = JsonSerializer.Deserialize<SutCompositeFormat>(Json, s_options);

        Assert.NotNull(actual);
        Assert.Equal("{", actual.Format);
        Assert.Equal(7, actual._literalLength);
        Assert.Equal(0, actual._formattedCount);
        Assert.Equal(0, actual._argsRequired);
        var segment = Assert.Single(actual._segments);
        CompositeFormatAssert.Segment(segment, "payload", -1, 0, null);
    }

    [Theory]
    [MemberData(nameof(InvalidPayloads))]
    public void Deserialize_StructurallyInvalidPayload_ThrowsJsonException(string json)
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<SutCompositeFormat>(json, s_options));
    }

    [Fact]
    public void Deserialize_DerivedFieldsDoNotMatchSegments_PreservesSerializedValues()
    {
        const string Json = """
            {
              "Format": "payload",
              "_segments": [
                ["payload", -1, 0, null]
              ],
              "_literalLength": 123,
              "_formattedCount": 456,
              "_argsRequired": 789
            }
            """;

        var actual = JsonSerializer.Deserialize<SutCompositeFormat>(Json, s_options);

        Assert.NotNull(actual);
        Assert.Equal(123, actual._literalLength);
        Assert.Equal(456, actual._formattedCount);
        Assert.Equal(789, actual._argsRequired);
        var segment = Assert.Single(actual._segments);
        CompositeFormatAssert.Segment(segment, "payload", -1, 0, null);
    }

    public static IEnumerable<object[]> InvalidPayloads()
    {
        yield return ["{}"];
        yield return ["""
            {
              "Format": "payload",
              "_segments": 42,
              "_literalLength": 7,
              "_formattedCount": 0,
              "_argsRequired": 0
            }
            """];
        yield return ["""
            {
              "Format": "payload",
              "_segments": [
                ["payload", -1, 0]
              ],
              "_literalLength": 7,
              "_formattedCount": 0,
              "_argsRequired": 0
            }
            """];
        yield return ["""
            {
              "Format": "payload",
              "_segments": [],
              "_literalLength": 0,
              "_formattedCount": 0
            }
            """];
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new SutCompositeFormatJsonConverter());
        return options;
    }

    private static void AssertJsonSegment(
        JsonElement actual,
        string? literal,
        int argIndex,
        int alignment,
        string? format)
    {
        var items = actual.EnumerateArray().ToArray();

        Assert.Equal(4, items.Length);
        Assert.Equal(literal, GetNullableString(items[0]));
        Assert.Equal(argIndex, items[1].GetInt32());
        Assert.Equal(alignment, items[2].GetInt32());
        Assert.Equal(format, GetNullableString(items[3]));
    }

    private static string? GetNullableString(JsonElement value) =>
        value.ValueKind == JsonValueKind.Null ? null : value.GetString();
}