using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Xunit;
using SutCompositeFormat = global::SerializableFormat.SerializableCompositeFormat;
using SutCompositeFormatMessagePackFormatter = global::SerializableFormat.MessagePack.CompositeFormatMessagePackFormatter;
using SutCompositeFormatResolver = global::SerializableFormat.MessagePack.CompositeFormatResolver;

namespace SerializableFormat.Tests;

public sealed class CompositeFormatMessagePackFormatterTests
{
    private static readonly MessagePackSerializerOptions s_options =
        MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                new IMessagePackFormatter[] { SutCompositeFormatMessagePackFormatter.Instance },
                new IFormatterResolver[] { StandardResolver.Instance }));

    [Fact]
    public void Serialize_WritesStableWireShape()
    {
        var value = SutCompositeFormat.Parse(
            "prefix {{escaped}} {2,-8:X2} / {0:yyyy-MM-dd} suffix");

        var bytes = MessagePackSerializer.Serialize(value, s_options);
        var reader = new MessagePackReader(bytes.AsMemory());

        Assert.Equal(5, reader.ReadArrayHeader());
        Assert.Equal(value.Format, reader.ReadString());

        Assert.Equal(5, reader.ReadArrayHeader());
        AssertMessagePackSegment(ref reader, "prefix {escaped} ", -1, 0, null);
        AssertMessagePackSegment(ref reader, null, 2, -8, "X2");
        AssertMessagePackSegment(ref reader, " / ", -1, 0, null);
        AssertMessagePackSegment(ref reader, null, 0, 0, "yyyy-MM-dd");
        AssertMessagePackSegment(ref reader, " suffix", -1, 0, null);

        Assert.Equal(27, reader.ReadInt32());
        Assert.Equal(2, reader.ReadInt32());
        Assert.Equal(3, reader.ReadInt32());
        Assert.True(reader.End);
    }

    [Fact]
    public void Resolver_ReturnsCompositeFormatFormatterOnly()
    {
        Assert.Same(
            SutCompositeFormatMessagePackFormatter.Instance,
            SutCompositeFormatResolver.Instance.GetFormatter<SutCompositeFormat>());
        Assert.Null(SutCompositeFormatResolver.Instance.GetFormatter<string>());
    }

    [Fact]
    public void SerializeThenDeserialize_Null_PreservesNull()
    {
        var bytes = MessagePackSerializer.Serialize<SutCompositeFormat?>(null, s_options);
        var actual = MessagePackSerializer.Deserialize<SutCompositeFormat?>(bytes, s_options);

        Assert.Equal([MessagePackCode.Nil], bytes);
        Assert.Null(actual);
    }

    [Theory]
    [MemberData(nameof(CompositeFormatTestData.RoundTripFormats), MemberType = typeof(CompositeFormatTestData))]
    public void SerializeThenDeserialize_PreservesEveryInternalField(string format)
    {
        var expected = SutCompositeFormat.Parse(format);

        var bytes = MessagePackSerializer.Serialize(expected, s_options);
        var actual = MessagePackSerializer.Deserialize<SutCompositeFormat>(bytes, s_options);

        Assert.NotNull(actual);
        CompositeFormatAssert.Equal(expected, actual);
    }

    [Fact]
    public void Deserialize_DoesNotReparseFormat_AndSkipsExtraArrayItems()
    {
        var payload = SerializeObjectArray(
            [
                "{",
                new object?[]
                {
                    new object?[] { "payload", -1, 0, null, "ignored segment item" },
                },
                7,
                0,
                0,
                new object?[] { "ignored", "top-level", "items" },
            ]);

        var actual = MessagePackSerializer.Deserialize<SutCompositeFormat>(payload, s_options);

        Assert.NotNull(actual);
        Assert.Equal("{", actual.Format);
        Assert.Equal(7, actual._literalLength);
        Assert.Equal(0, actual._formattedCount);
        Assert.Equal(0, actual._argsRequired);
        var segment = Assert.Single(actual._segments);
        CompositeFormatAssert.Segment(segment, "payload", -1, 0, null);
    }

    [Fact]
    public void Deserialize_TopLevelValueIsNotArray_ThrowsMessagePackSerializationException()
    {
        var payload = MessagePackSerializer.Serialize("not an array");

        Assert.Throws<MessagePackSerializationException>(() =>
            MessagePackSerializer.Deserialize<SutCompositeFormat>(payload, s_options));
    }

    [Fact]
    public void Deserialize_TopLevelArrayIsTooShort_ThrowsMessagePackSerializationException()
    {
        var payload = SerializeObjectArray(["payload", Array.Empty<object?>(), 0, 0]);

        Assert.Throws<MessagePackSerializationException>(() =>
            MessagePackSerializer.Deserialize<SutCompositeFormat>(payload, s_options));
    }

    [Fact]
    public void Deserialize_SegmentArrayIsTooShort_ThrowsMessagePackSerializationException()
    {
        var payload = SerializeObjectArray(
            [
                "payload",
                new object?[]
                {
                    new object?[] { "payload", -1, 0 },
                },
                7,
                0,
                0,
            ]);

        Assert.Throws<MessagePackSerializationException>(() =>
            MessagePackSerializer.Deserialize<SutCompositeFormat>(payload, s_options));
    }

    [Fact]
    public void Deserialize_DerivedFieldsDoNotMatchSegments_PreservesSerializedValues()
    {
        var payload = SerializeObjectArray(
            [
                "payload",
                new object?[]
                {
                    new object?[] { "payload", -1, 0, null },
                },
                123,
                456,
                789,
            ]);

        var actual = MessagePackSerializer.Deserialize<SutCompositeFormat>(payload, s_options);

        Assert.NotNull(actual);
        Assert.Equal(123, actual._literalLength);
        Assert.Equal(456, actual._formattedCount);
        Assert.Equal(789, actual._argsRequired);
        var segment = Assert.Single(actual._segments);
        CompositeFormatAssert.Segment(segment, "payload", -1, 0, null);
    }

    private static byte[] SerializeObjectArray(object?[] value) =>
        MessagePackSerializer.Serialize(value, MessagePackSerializerOptions.Standard);

    private static void AssertMessagePackSegment(
        ref MessagePackReader reader,
        string? literal,
        int argIndex,
        int alignment,
        string? format)
    {
        Assert.Equal(4, reader.ReadArrayHeader());
        Assert.Equal(literal, reader.ReadString());
        Assert.Equal(argIndex, reader.ReadInt32());
        Assert.Equal(alignment, reader.ReadInt32());
        Assert.Equal(format, reader.ReadString());
    }
}