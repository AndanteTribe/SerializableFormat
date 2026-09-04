// The parse cases in this file are adapted from dotnet/runtime's CompositeFormatTests
// and StringTests. The .NET Foundation source is licensed under the MIT license.

using System.Diagnostics;
using Xunit;
#if NET8_0_OR_GREATER
using BclCompositeFormat = System.Text.CompositeFormat;
#endif
using SutCompositeFormat = global::SerializableFormat.SerializableCompositeFormat;

namespace SerializableFormat.Tests;

public sealed class CompositeFormatTests
{
    [Fact]
    public void Parse_NullArgument_Throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => SutCompositeFormat.Parse(null!));

        Assert.Equal("format", exception.ParamName);
    }

    [Fact]
    public void DebuggerDisplay_ShowsFormatProperty()
    {
        var attribute = Assert.Single(
            typeof(SutCompositeFormat).GetCustomAttributes(typeof(DebuggerDisplayAttribute), inherit: false)
                .Cast<DebuggerDisplayAttribute>());

        Assert.Equal("{Format}", attribute.Value);
    }

    [Fact]
    public void Parse_FormatParameter_HasCompositeFormatStringSyntax()
    {
        var parameter = typeof(SutCompositeFormat).GetMethod(nameof(SutCompositeFormat.Parse))!.GetParameters()[0];
        var attribute = Assert.Single(
            parameter.CustomAttributes,
            candidate => candidate.AttributeType.FullName == "System.Diagnostics.CodeAnalysis.StringSyntaxAttribute");

        Assert.Equal("CompositeFormat", attribute.ConstructorArguments[0].Value);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("testing 123", 0)]
    [InlineData("testing {{123}}", 0)]
    [InlineData("{0}", 1)]
    [InlineData("{0} {1}", 2)]
    [InlineData("{2}", 3)]
    [InlineData("{2} {0}", 3)]
    [InlineData("{1} {34} {3}", 35)]
    public void MinimumArgumentCount_MatchesExpectedValue(string format, int expected)
    {
        var parsed = SutCompositeFormat.Parse(format);

        Assert.Equal(expected, parsed.MinimumArgumentCount);
        Assert.Equal(parsed._argsRequired, parsed.MinimumArgumentCount);
    }

#if NET8_0_OR_GREATER
    [Theory]
    [MemberData(nameof(CompositeFormatTestData.ValidFormats), MemberType = typeof(CompositeFormatTestData))]
    public void Parse_ValidFormat_MatchesBclInternalState(string format)
    {
        var bcl = BclCompositeFormat.Parse(format);
        var parsed = SutCompositeFormat.Parse(format);

        Assert.Same(format, parsed.Format);
        CompositeFormatAssert.MatchesBcl(bcl, parsed);
    }
#endif

    [Theory]
    [MemberData(nameof(CompositeFormatTestData.InvalidFormats), MemberType = typeof(CompositeFormatTestData))]
    public void Parse_InvalidFormat_ThrowsFormatExceptionLikeBcl(string format)
    {
#if NET8_0_OR_GREATER
        Assert.Throws<FormatException>(() => BclCompositeFormat.Parse(format));
#endif
        Assert.Throws<FormatException>(() => SutCompositeFormat.Parse(format));
    }

    [Fact]
    public void Parse_RepresentativeFormat_ProducesExpectedSegmentsAndDerivedFields()
    {
        const string Format = "prefix {{escaped}} {2,-8:X2} / {0:yyyy-MM-dd} suffix";

        var parsed = SutCompositeFormat.Parse(Format);

        Assert.Equal(Format, parsed.Format);
        Assert.Equal(3, parsed.MinimumArgumentCount);
        Assert.Equal(27, parsed._literalLength);
        Assert.Equal(2, parsed._formattedCount);
        Assert.Equal(3, parsed._argsRequired);
        Assert.Collection(
            parsed._segments,
            segment => CompositeFormatAssert.Segment(segment, "prefix {escaped} ", -1, 0, null),
            segment => CompositeFormatAssert.Segment(segment, null, 2, -8, "X2"),
            segment => CompositeFormatAssert.Segment(segment, " / ", -1, 0, null),
            segment => CompositeFormatAssert.Segment(segment, null, 0, 0, "yyyy-MM-dd"),
            segment => CompositeFormatAssert.Segment(segment, " suffix", -1, 0, null));
    }
}
