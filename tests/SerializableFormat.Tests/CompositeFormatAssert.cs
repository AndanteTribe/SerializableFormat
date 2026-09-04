using Xunit;
#if NET8_0_OR_GREATER
using System.Reflection;
using System.Runtime.CompilerServices;
using BclCompositeFormat = System.Text.CompositeFormat;
#endif
using SutCompositeFormat = global::SerializableFormat.SerializableCompositeFormat;

namespace SerializableFormat.Tests;

internal static class CompositeFormatAssert
{
#if NET8_0_OR_GREATER
    private static readonly FieldInfo s_bclSegmentsField = GetBclField("_segments");
    private static readonly FieldInfo s_bclLiteralLengthField = GetBclField("_literalLength");
    private static readonly FieldInfo s_bclFormattedCountField = GetBclField("_formattedCount");
    private static readonly FieldInfo s_bclArgsRequiredField = GetBclField("_argsRequired");

    public static void MatchesBcl(BclCompositeFormat expected, SutCompositeFormat actual)
    {
        Assert.Equal(expected.Format, actual.Format);
        Assert.Equal(expected.MinimumArgumentCount, actual.MinimumArgumentCount);

        var expectedSegments = GetBclSegments(expected);
        Assert.Equal(expectedSegments.Length, actual._segments.Length);
        for (var i = 0; i < expectedSegments.Length; i++)
        {
            Assert.Equal(expectedSegments[i], actual._segments[i]);
        }

        Assert.Equal(GetBclInt32(s_bclLiteralLengthField, expected), actual._literalLength);
        Assert.Equal(GetBclInt32(s_bclFormattedCountField, expected), actual._formattedCount);
        Assert.Equal(GetBclInt32(s_bclArgsRequiredField, expected), actual._argsRequired);
    }
#endif

    public static void Equal(SutCompositeFormat expected, SutCompositeFormat actual)
    {
        Assert.Equal(expected.Format, actual.Format);
        Assert.Equal(expected.MinimumArgumentCount, actual.MinimumArgumentCount);
        Assert.Equal(expected._literalLength, actual._literalLength);
        Assert.Equal(expected._formattedCount, actual._formattedCount);
        Assert.Equal(expected._argsRequired, actual._argsRequired);

        Assert.Equal(expected._segments.Length, actual._segments.Length);
        for (var i = 0; i < expected._segments.Length; i++)
        {
            Assert.Equal(expected._segments[i], actual._segments[i]);
        }
    }

    public static void Segment(
        (string? Literal, int ArgIndex, int Alignment, string? Format) actual,
        string? literal,
        int argIndex,
        int alignment,
        string? format)
    {
        Assert.Equal(literal, actual.Literal);
        Assert.Equal(argIndex, actual.ArgIndex);
        Assert.Equal(alignment, actual.Alignment);
        Assert.Equal(format, actual.Format);
    }

#if NET8_0_OR_GREATER
    private static FieldInfo GetBclField(string name) =>
        typeof(BclCompositeFormat).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException($"The BCL CompositeFormat field '{name}' was not found.");

    private static int GetBclInt32(FieldInfo field, BclCompositeFormat value) =>
        (int)(field.GetValue(value) ?? throw new InvalidOperationException($"The BCL field '{field.Name}' was null."));

    private static (string? Literal, int ArgIndex, int Alignment, string? Format)[] GetBclSegments(
        BclCompositeFormat value)
    {
        var source = (Array)(s_bclSegmentsField.GetValue(value)
            ?? throw new InvalidOperationException("The BCL CompositeFormat segments were null."));
        var result = new (string? Literal, int ArgIndex, int Alignment, string? Format)[source.Length];

        for (var i = 0; i < source.Length; i++)
        {
            var tuple = (ITuple)(source.GetValue(i)
                ?? throw new InvalidOperationException($"The BCL CompositeFormat segment at index {i} was null."));
            result[i] = ((string?)tuple[0], (int)tuple[1]!, (int)tuple[2]!, (string?)tuple[3]);
        }

        return result;
    }
#endif
}