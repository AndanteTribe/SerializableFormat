using MessagePack;
using MessagePack.Formatters;

namespace SerializableFormat.MessagePack;

/// <summary>
/// Serializes <see cref="SerializableCompositeFormat"/> values while preserving their parsed fields.
/// </summary>
/// <remarks>
/// The top-level array contains <c>Format</c>, <c>_segments</c>, <c>_literalLength</c>,
/// <c>_formattedCount</c>, and <c>_argsRequired</c>, in that order.
/// </remarks>
public sealed class CompositeFormatMessagePackFormatter : IMessagePackFormatter<SerializableCompositeFormat?>
{
    private const int FieldCount = 5;
    private const int SegmentFieldCount = 4;

    private CompositeFormatMessagePackFormatter()
    {
    }

    /// <summary>Gets the shared formatter instance.</summary>
    public static readonly CompositeFormatMessagePackFormatter Instance = new();

    /// <inheritdoc/>
    public void Serialize(ref MessagePackWriter writer, SerializableCompositeFormat? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteArrayHeader(FieldCount);
        writer.Write(value.Format);

        writer.WriteArrayHeader(value._segments.Length);
        foreach (var segment in value._segments)
        {
            writer.CancellationToken.ThrowIfCancellationRequested();
            writer.WriteArrayHeader(SegmentFieldCount);
            writer.Write(segment.Literal);
            writer.Write(segment.ArgIndex);
            writer.Write(segment.Alignment);
            writer.Write(segment.Format);
        }

        writer.Write(value._literalLength);
        writer.Write(value._formattedCount);
        writer.Write(value._argsRequired);
    }

    /// <inheritdoc/>
    public SerializableCompositeFormat? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return null;
        }

        var fieldCount = reader.ReadArrayHeader();
        if (fieldCount < FieldCount)
        {
            throw new MessagePackSerializationException($"A composite format must contain at least {FieldCount} fields, but contained {fieldCount}.");
        }

        options.Security.DepthStep(ref reader);
        try
        {
            var format = reader.ReadString() ?? throw new MessagePackSerializationException("The composite format's Format field cannot be nil.");
            var segments = ReadSegments(ref reader, options);
            var literalLength = reader.ReadInt32();
            var formattedCount = reader.ReadInt32();
            var argsRequired = reader.ReadInt32();

            for (var index = FieldCount; index < fieldCount; index++)
            {
                reader.CancellationToken.ThrowIfCancellationRequested();
                reader.Skip();
            }

            return new SerializableCompositeFormat(format, segments, literalLength, formattedCount, argsRequired);
        }
        finally
        {
            reader.Depth--;
        }
    }

    private static (string? Literal, int ArgIndex, int Alignment, string? Format)[] ReadSegments(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            throw new MessagePackSerializationException("The composite format's _segments field cannot be nil.");
        }

        var segmentCount = reader.ReadArrayHeader();
        options.Security.DepthStep(ref reader);
        try
        {
            var segments = new List<(string? Literal, int ArgIndex, int Alignment, string? Format)>();
            for (var index = 0; index < segmentCount; index++)
            {
                reader.CancellationToken.ThrowIfCancellationRequested();
                segments.Add(ReadSegment(ref reader, options));
            }

            return segments.ToArray();
        }
        finally
        {
            reader.Depth--;
        }
    }

    private static (string? Literal, int ArgIndex, int Alignment, string? Format) ReadSegment(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            throw new MessagePackSerializationException("A composite format segment cannot be nil.");
        }

        var fieldCount = reader.ReadArrayHeader();
        if (fieldCount < SegmentFieldCount)
        {
            throw new MessagePackSerializationException(
                $"A composite format segment must contain at least {SegmentFieldCount} fields, but contained {fieldCount}.");
        }

        options.Security.DepthStep(ref reader);
        try
        {
            var literal = reader.ReadString();
            var argIndex = reader.ReadInt32();
            var alignment = reader.ReadInt32();
            var format = reader.ReadString();

            for (var index = SegmentFieldCount; index < fieldCount; index++)
            {
                reader.CancellationToken.ThrowIfCancellationRequested();
                reader.Skip();
            }

            return (literal, argIndex, alignment, format);
        }
        finally
        {
            reader.Depth--;
        }
    }
}