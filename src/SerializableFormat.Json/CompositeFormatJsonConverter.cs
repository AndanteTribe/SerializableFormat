using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerializableFormat.Json;

/// <summary>
/// Converts <see cref="SerializableCompositeFormat"/> values to and from JSON while preserving their parsed fields.
/// </summary>
public sealed class CompositeFormatJsonConverter : JsonConverter<SerializableCompositeFormat>
{
    private const string FormatPropertyName = nameof(SerializableCompositeFormat.Format);
    private const string SegmentsPropertyName = nameof(SerializableCompositeFormat._segments);
    private const string LiteralLengthPropertyName = nameof(SerializableCompositeFormat._literalLength);
    private const string FormattedCountPropertyName = nameof(SerializableCompositeFormat._formattedCount);
    private const string ArgsRequiredPropertyName = nameof(SerializableCompositeFormat._argsRequired);

    /// <inheritdoc/>
    public override SerializableCompositeFormat Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected a JSON object for a composite format.");
        }

        string? format = null;
        (string? Literal, int ArgIndex, int Alignment, string? Format)[]? segments = null;
        var literalLength = 0;
        var formattedCount = 0;
        var argsRequired = 0;

        var hasFormat = false;
        var hasSegments = false;
        var hasLiteralLength = false;
        var hasFormattedCount = false;
        var hasArgsRequired = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (!hasFormat || !hasSegments || !hasLiteralLength || !hasFormattedCount || !hasArgsRequired)
                {
                    throw new JsonException("The composite format JSON is missing one or more required fields.");
                }

                return new SerializableCompositeFormat(format!, segments!, literalLength, formattedCount, argsRequired);
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected a property name in the composite format JSON object.");
            }

            var propertyName = reader.GetString()!;
            if (!reader.Read())
            {
                throw new JsonException("The composite format JSON ended before a property value was read.");
            }

            switch (propertyName)
            {
                case FormatPropertyName:
                    format = ReadNonNullString(ref reader, FormatPropertyName);
                    hasFormat = true;
                    break;

                case SegmentsPropertyName:
                    segments = ReadSegments(ref reader);
                    hasSegments = true;
                    break;

                case LiteralLengthPropertyName:
                    literalLength = ReadInt32(ref reader, LiteralLengthPropertyName);
                    hasLiteralLength = true;
                    break;

                case FormattedCountPropertyName:
                    formattedCount = ReadInt32(ref reader, FormattedCountPropertyName);
                    hasFormattedCount = true;
                    break;

                case ArgsRequiredPropertyName:
                    argsRequired = ReadInt32(ref reader, ArgsRequiredPropertyName);
                    hasArgsRequired = true;
                    break;

                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException("The composite format JSON object was not closed.");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, SerializableCompositeFormat value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(FormatPropertyName, value.Format);

        writer.WritePropertyName(SegmentsPropertyName);
        writer.WriteStartArray();
        foreach (var segment in value._segments)
        {
            writer.WriteStartArray();
            writer.WriteStringValue(segment.Literal);
            writer.WriteNumberValue(segment.ArgIndex);
            writer.WriteNumberValue(segment.Alignment);
            writer.WriteStringValue(segment.Format);
            writer.WriteEndArray();
        }

        writer.WriteEndArray();
        writer.WriteNumber(LiteralLengthPropertyName, value._literalLength);
        writer.WriteNumber(FormattedCountPropertyName, value._formattedCount);
        writer.WriteNumber(ArgsRequiredPropertyName, value._argsRequired);
        writer.WriteEndObject();
    }

    private static (string? Literal, int ArgIndex, int Alignment, string? Format)[] ReadSegments(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"The '{SegmentsPropertyName}' field must be an array.");
        }

        var segments = new List<(string? Literal, int ArgIndex, int Alignment, string? Format)>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return segments.ToArray();
            }

            segments.Add(ReadSegment(ref reader));
        }

        throw new JsonException($"The '{SegmentsPropertyName}' array was not closed.");
    }

    private static (string? Literal, int ArgIndex, int Alignment, string? Format) ReadSegment(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Each composite format segment must be an array.");
        }

        MoveToRequiredSegmentItem(ref reader);
        var literal = ReadNullableString(ref reader, "Literal");
        MoveToRequiredSegmentItem(ref reader);
        var argIndex = ReadInt32(ref reader, "ArgIndex");
        MoveToRequiredSegmentItem(ref reader);
        var alignment = ReadInt32(ref reader, "Alignment");
        MoveToRequiredSegmentItem(ref reader);
        var format = ReadNullableString(ref reader, "Format");

        if (!reader.Read())
        {
            throw new JsonException("The composite format segment array was not closed.");
        }

        while (reader.TokenType != JsonTokenType.EndArray)
        {
            reader.Skip();
            if (!reader.Read())
            {
                throw new JsonException("The composite format segment array was not closed.");
            }
        }

        return (literal, argIndex, alignment, format);
    }

    private static void MoveToRequiredSegmentItem(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            throw new JsonException("The composite format segment array was not closed.");
        }

        if (reader.TokenType == JsonTokenType.EndArray)
        {
            throw new JsonException("A composite format segment must contain at least four items.");
        }
    }

    private static string ReadNonNullString(ref Utf8JsonReader reader, string propertyName)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"The '{propertyName}' field must be a string.");
        }

        return reader.GetString()!;
    }

    private static string? ReadNullableString(ref Utf8JsonReader reader, string propertyName)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"The '{propertyName}' field must be a string or null.");
        }

        return reader.GetString();
    }

    private static int ReadInt32(ref Utf8JsonReader reader, string propertyName)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out int value))
        {
            throw new JsonException($"The '{propertyName}' field must be a 32-bit integer.");
        }

        return value;
    }
}