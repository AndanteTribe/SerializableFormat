using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace SerializableFormat.Tests;

public sealed class FormatExtensionsTests
{
    private static readonly IFormatProvider s_invariantCulture = CultureInfo.InvariantCulture;

    [Fact]
    public void PublicSurface_ContainsAllTwentySevenFormattingOverloads()
    {
        var methods = typeof(FormatExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        Assert.Equal(9, methods.Count(method => method.Name == "Format"));
        Assert.Equal(9, methods.Count(method => method.Name == "AppendFormat"));
        Assert.Equal(9, methods.Count(method => method.Name == "TryWrite"));
        Assert.Equal(27, methods.Length);

        AssertMethodFamily(
            methods,
            "Format",
            typeof(string),
            [typeof(IFormatProvider), typeof(SerializableCompositeFormat)],
            hasExtensionReceiver: false);
        AssertMethodFamily(
            methods,
            "AppendFormat",
            typeof(StringBuilder),
            [typeof(StringBuilder), typeof(IFormatProvider), typeof(SerializableCompositeFormat)],
            hasExtensionReceiver: true);
        AssertMethodFamily(
            methods,
            "TryWrite",
            typeof(bool),
            [typeof(Span<char>), typeof(IFormatProvider), typeof(SerializableCompositeFormat), typeof(int).MakeByRefType()],
            hasExtensionReceiver: true,
            charsWrittenIndex: 3);
    }

    [Fact]
    public void StringFormat_GenericOverloadsOneThroughSeven_FormatExpectedValues()
    {
        Assert.Equal("0", string.Format(s_invariantCulture, Parse("{0}"), 0));
        Assert.Equal("01", string.Format(s_invariantCulture, Parse("{0}{1}"), 0, 1));
        Assert.Equal("012", string.Format(s_invariantCulture, Parse("{0}{1}{2}"), 0, 1, 2));
        Assert.Equal("0123", string.Format(s_invariantCulture, Parse("{0}{1}{2}{3}"), 0, 1, 2, 3));
        Assert.Equal("01234", string.Format(s_invariantCulture, Parse("{0}{1}{2}{3}{4}"), 0, 1, 2, 3, 4));
        Assert.Equal("012345", string.Format(s_invariantCulture, Parse("{0}{1}{2}{3}{4}{5}"), 0, 1, 2, 3, 4, 5));
        Assert.Equal("0123456", string.Format(s_invariantCulture, Parse("{0}{1}{2}{3}{4}{5}{6}"), 0, 1, 2, 3, 4, 5, 6));
    }

    [Fact]
    public void StringFormat_ArrayAndSpanOverloads_FormatExpectedValues()
    {
        var format = Parse("{0}|{1}|{7}");
        object?[] args = [0, 1, 2, 3, 4, 5, 6, 7];
        ReadOnlySpan<object?> span = args;

        Assert.Equal("0|1|7", string.Format(s_invariantCulture, format, args));
        Assert.Equal("0|1|7", string.Format(s_invariantCulture, format, span));
        Assert.Equal("{}", string.Format(s_invariantCulture, Parse("{{}}")));
    }

    [Fact]
    public void StringFormat_CanBeCalledThroughContainingClass()
    {
        var result = FormatExtensions.Format(
            s_invariantCulture,
            Parse("{0:N2}"),
            12.5m);

        Assert.Equal("12.50", result);
    }

    [Fact]
    public void StringBuilderAppendFormat_AllOverloadShapes_FormatExpectedValues()
    {
        var builder = new StringBuilder();

        AssertSameAndReset(builder, builder.AppendFormat(s_invariantCulture, Parse("{0}"), 0), "0");
        AssertSameAndReset(builder, builder.AppendFormat(s_invariantCulture, Parse("{0}{1}"), 0, 1), "01");
        AssertSameAndReset(builder, builder.AppendFormat(s_invariantCulture, Parse("{0}{1}{2}"), 0, 1, 2), "012");
        AssertSameAndReset(builder, builder.AppendFormat(s_invariantCulture, Parse("{0}{1}{2}{3}"), 0, 1, 2, 3), "0123");
        AssertSameAndReset(builder, builder.AppendFormat(s_invariantCulture, Parse("{0}{1}{2}{3}{4}"), 0, 1, 2, 3, 4), "01234");
        AssertSameAndReset(builder, builder.AppendFormat(s_invariantCulture, Parse("{0}{1}{2}{3}{4}{5}"), 0, 1, 2, 3, 4, 5), "012345");
        AssertSameAndReset(builder, builder.AppendFormat(s_invariantCulture, Parse("{0}{1}{2}{3}{4}{5}{6}"), 0, 1, 2, 3, 4, 5, 6), "0123456");

        object?[] args = [0, 1, 2, 3, 4, 5, 6, 7];
        AssertSameAndReset(builder, builder.AppendFormat(s_invariantCulture, Parse("{0}{7}"), args), "07");

        ReadOnlySpan<object?> span = args;
        AssertSameAndReset(builder, builder.AppendFormat(s_invariantCulture, Parse("{0}{7}"), span), "07");
    }

    [Fact]
    public void SpanTryWrite_AllOverloadShapes_FormatExpectedValues()
    {
        Span<char> destination = stackalloc char[32];

        AssertTryWrite(destination, Parse("{0}"), "0", 0);
        AssertTryWrite(destination, Parse("{0}{1}"), "01", 0, 1);
        AssertTryWrite(destination, Parse("{0}{1}{2}"), "012", 0, 1, 2);
        AssertTryWrite(destination, Parse("{0}{1}{2}{3}"), "0123", 0, 1, 2, 3);
        AssertTryWrite(destination, Parse("{0}{1}{2}{3}{4}"), "01234", 0, 1, 2, 3, 4);
        AssertTryWrite(destination, Parse("{0}{1}{2}{3}{4}{5}"), "012345", 0, 1, 2, 3, 4, 5);
        AssertTryWrite(destination, Parse("{0}{1}{2}{3}{4}{5}{6}"), "0123456", 0, 1, 2, 3, 4, 5, 6);

        object?[] args = [0, 1, 2, 3, 4, 5, 6, 7];
        Assert.True(destination.TryWrite(s_invariantCulture, Parse("{0}{7}"), out var arrayCharsWritten, args));
        Assert.Equal("07", destination[..arrayCharsWritten].ToString());

        ReadOnlySpan<object?> span = args;
        Assert.True(destination.TryWrite(s_invariantCulture, Parse("{0}{7}"), out var spanCharsWritten, span));
        Assert.Equal("07", destination[..spanCharsWritten].ToString());
    }

    [Fact]
    public void Formatting_MatchesBclForFormatsAlignmentNullsAndEscapedBraces()
    {
        const string Format = "{{{0,-10:N2}}}|{1,5}|{2:yyyy-MM-dd}|{3}";
        object?[] args = [1234.5m, null, new DateTime(2026, 9, 4), "tail"];
        var expected = string.Format(s_invariantCulture, Format, args);
        var format = Parse(Format);

        Assert.Equal(expected, string.Format(s_invariantCulture, format, args));

        var builder = new StringBuilder("prefix:");
        var returned = builder.AppendFormat(s_invariantCulture, format, args);
        Assert.Same(builder, returned);
        Assert.Equal("prefix:" + expected, builder.ToString());

        Span<char> destination = stackalloc char[128];
        Assert.True(destination.TryWrite(s_invariantCulture, format, out var charsWritten, args));
        Assert.Equal(expected, destination[..charsWritten].ToString());
    }

    [Theory]
    [MemberData(nameof(CompositeFormatTestData.ValidFormats), MemberType = typeof(CompositeFormatTestData))]
    public void Formatting_ValidFormatsMatchBclAcrossAllDestinations(string formatText)
    {
        object?[] args = [123, 456, 789, 12, 34];
        var expected = string.Format(s_invariantCulture, formatText, args);
        var format = Parse(formatText);

        Assert.Equal(expected, string.Format(s_invariantCulture, format, args));

        var builder = new StringBuilder();
        builder.AppendFormat(s_invariantCulture, format, args);
        Assert.Equal(expected, builder.ToString());

        var destination = new char[expected.Length];
        Assert.True(destination.AsSpan().TryWrite(
            s_invariantCulture,
            format,
            out var charsWritten,
            args));
        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, destination.AsSpan(0, charsWritten).ToString());
    }

    [Fact]
    public void Formatting_UsesISpanFormattableForGenericArguments()
    {
        var format = Parse("<{0,12:X}>");

        var stringTracker = new CallTracker();
        Assert.Equal("<      span-X>", string.Format(s_invariantCulture, format, new SpanFormattableValue(stringTracker)));
        Assert.True(stringTracker.TryFormatCalled);
        Assert.False(stringTracker.ToStringCalled);

        var builderTracker = new CallTracker();
        var builder = new StringBuilder();
        builder.AppendFormat(s_invariantCulture, format, new SpanFormattableValue(builderTracker));
        Assert.Equal("<      span-X>", builder.ToString());
        Assert.True(builderTracker.TryFormatCalled);
        Assert.False(builderTracker.ToStringCalled);

        var spanTracker = new CallTracker();
        Span<char> destination = stackalloc char[32];
        Assert.True(destination.TryWrite(
            s_invariantCulture,
            format,
            out var charsWritten,
            new SpanFormattableValue(spanTracker)));
        Assert.Equal("<      span-X>", destination[..charsWritten].ToString());
        Assert.True(spanTracker.TryFormatCalled);
        Assert.False(spanTracker.ToStringCalled);
    }

    [Fact]
    public void Formatting_CustomFormatterTakesPrecedence()
    {
        var provider = new CustomFormatter();
        var format = Parse("<{0,8:X}>");
        var tracker = new CallTracker();
        var value = new SpanFormattableValue(tracker);

        Assert.Equal("<  custom>", string.Format(provider, format, value));
        Assert.False(tracker.TryFormatCalled);
        Assert.False(tracker.ToStringCalled);

        var builder = new StringBuilder();
        builder.AppendFormat(provider, format, value);
        Assert.Equal("<  custom>", builder.ToString());

        Span<char> destination = stackalloc char[16];
        Assert.True(destination.TryWrite(provider, format, out var charsWritten, value));
        Assert.Equal("<  custom>", destination[..charsWritten].ToString());
    }

    [Fact]
    public void SpanTryWrite_InsufficientDestination_ReturnsFalseAndZeroCharsWritten()
    {
        Span<char> destination = stackalloc char[3];

        var success = destination.TryWrite(s_invariantCulture, Parse("value={0}"), out var charsWritten, 42);

        Assert.False(success);
        Assert.Equal(0, charsWritten);
    }

    [Fact]
    public void Formatting_InsufficientArguments_ThrowsFormatExceptionBeforeWriting()
    {
        var format = Parse("before{1}");
        var builder = new StringBuilder("existing");
        var destination = new char[32];

        Assert.Throws<FormatException>(() => FormatExtensions.Format(s_invariantCulture, format, 0));
        Assert.Throws<FormatException>(() => builder.AppendFormat(s_invariantCulture, format, 0));
        Assert.Throws<FormatException>(() => destination.AsSpan().TryWrite(s_invariantCulture, format, out _, 0));
        Assert.Equal("existing", builder.ToString());
    }

    [Fact]
    public void Formatting_NullFormatOrArgumentArray_ThrowsArgumentNullException()
    {
        var format = Parse("{0}");
        var builder = new StringBuilder();
        var destination = new char[32];

        Assert.Equal("format", Assert.Throws<ArgumentNullException>(
            () => FormatExtensions.Format(s_invariantCulture, null!, 0)).ParamName);
        Assert.Equal("args", Assert.Throws<ArgumentNullException>(
            () => FormatExtensions.Format(s_invariantCulture, format, (object?[])null!)).ParamName);
        Assert.Equal("args", Assert.Throws<ArgumentNullException>(
            () => builder.AppendFormat(s_invariantCulture, format, (object?[])null!)).ParamName);
        Assert.Equal("args", Assert.Throws<ArgumentNullException>(
            () => destination.AsSpan().TryWrite(s_invariantCulture, format, out _, (object?[])null!)).ParamName);
    }

    private static SerializableCompositeFormat Parse(string format) => SerializableCompositeFormat.Parse(format);

    private static void AssertMethodFamily(
        MethodInfo[] methods,
        string name,
        Type returnType,
        Type[] leadingParameterTypes,
        bool hasExtensionReceiver,
        int? charsWrittenIndex = null)
    {
        var family = methods.Where(method => method.Name == name).ToArray();
        var genericMethods = family.Where(method => method.IsGenericMethodDefinition).ToArray();

        Assert.Equal(Enumerable.Range(1, 7), genericMethods.Select(method => method.GetGenericArguments().Length).OrderBy(length => length));
        Assert.All(genericMethods, method =>
        {
            Assert.Equal(returnType, method.ReturnType);
            Assert.Equal(leadingParameterTypes, method.GetParameters().Take(leadingParameterTypes.Length).Select(parameter => parameter.ParameterType));
            Assert.Equal(hasExtensionReceiver, method.IsDefined(typeof(ExtensionAttribute), inherit: false));
            if (charsWrittenIndex is int index)
            {
                Assert.True(method.GetParameters()[index].IsOut);
            }
        });

        var nonGenericMethods = family.Where(method => !method.IsGenericMethod).ToArray();
        Assert.Equal(2, nonGenericMethods.Length);

        var arrayMethod = Assert.Single(nonGenericMethods, method => method.GetParameters()[^1].ParameterType == typeof(object[]));
        Assert.True(arrayMethod.GetParameters()[^1].IsDefined(typeof(ParamArrayAttribute), inherit: false));

        var spanMethod = Assert.Single(nonGenericMethods, method => method.GetParameters()[^1].ParameterType == typeof(ReadOnlySpan<object>));
        Assert.Contains(
            spanMethod.GetParameters()[^1].GetCustomAttributesData(),
            attribute => attribute.AttributeType.FullName == "System.Runtime.CompilerServices.ParamCollectionAttribute");

        Assert.All(nonGenericMethods, method =>
        {
            Assert.Equal(returnType, method.ReturnType);
            Assert.Equal(leadingParameterTypes, method.GetParameters().Take(leadingParameterTypes.Length).Select(parameter => parameter.ParameterType));
            Assert.Equal(hasExtensionReceiver, method.IsDefined(typeof(ExtensionAttribute), inherit: false));
            if (charsWrittenIndex is int index)
            {
                Assert.True(method.GetParameters()[index].IsOut);
            }
        });
    }

    private static void AssertSameAndReset(StringBuilder expected, StringBuilder actual, string expectedText)
    {
        Assert.Same(expected, actual);
        Assert.Equal(expectedText, actual.ToString());
        actual.Clear();
    }

    private static void AssertTryWrite<TArg0>(Span<char> destination, SerializableCompositeFormat format, string expected, TArg0 arg0)
    {
        Assert.True(destination.TryWrite(s_invariantCulture, format, out var charsWritten, arg0));
        Assert.Equal(expected, destination[..charsWritten].ToString());
    }

    private static void AssertTryWrite<TArg0, TArg1>(Span<char> destination, SerializableCompositeFormat format, string expected, TArg0 arg0, TArg1 arg1)
    {
        Assert.True(destination.TryWrite(s_invariantCulture, format, out var charsWritten, arg0, arg1));
        Assert.Equal(expected, destination[..charsWritten].ToString());
    }

    private static void AssertTryWrite<TArg0, TArg1, TArg2>(Span<char> destination, SerializableCompositeFormat format, string expected, TArg0 arg0, TArg1 arg1, TArg2 arg2)
    {
        Assert.True(destination.TryWrite(s_invariantCulture, format, out var charsWritten, arg0, arg1, arg2));
        Assert.Equal(expected, destination[..charsWritten].ToString());
    }

    private static void AssertTryWrite<TArg0, TArg1, TArg2, TArg3>(Span<char> destination, SerializableCompositeFormat format, string expected, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3)
    {
        Assert.True(destination.TryWrite(s_invariantCulture, format, out var charsWritten, arg0, arg1, arg2, arg3));
        Assert.Equal(expected, destination[..charsWritten].ToString());
    }

    private static void AssertTryWrite<TArg0, TArg1, TArg2, TArg3, TArg4>(Span<char> destination, SerializableCompositeFormat format, string expected, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
    {
        Assert.True(destination.TryWrite(s_invariantCulture, format, out var charsWritten, arg0, arg1, arg2, arg3, arg4));
        Assert.Equal(expected, destination[..charsWritten].ToString());
    }

    private static void AssertTryWrite<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5>(Span<char> destination, SerializableCompositeFormat format, string expected, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
    {
        Assert.True(destination.TryWrite(s_invariantCulture, format, out var charsWritten, arg0, arg1, arg2, arg3, arg4, arg5));
        Assert.Equal(expected, destination[..charsWritten].ToString());
    }

    private static void AssertTryWrite<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(Span<char> destination, SerializableCompositeFormat format, string expected, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
    {
        Assert.True(destination.TryWrite(s_invariantCulture, format, out var charsWritten, arg0, arg1, arg2, arg3, arg4, arg5, arg6));
        Assert.Equal(expected, destination[..charsWritten].ToString());
    }

    private sealed class CallTracker
    {
        internal bool TryFormatCalled { get; set; }

        internal bool ToStringCalled { get; set; }
    }

    private readonly struct SpanFormattableValue : ISpanFormattable
    {
        private readonly CallTracker _tracker;

        internal SpanFormattableValue(CallTracker tracker) => _tracker = tracker;

        public bool TryFormat(
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format,
            IFormatProvider? provider)
        {
            _tracker.TryFormatCalled = true;
            var text = format.SequenceEqual("X") ? "span-X" : "span";
            if (text.AsSpan().TryCopyTo(destination))
            {
                charsWritten = text.Length;
                return true;
            }

            charsWritten = 0;
            return false;
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            _tracker.ToStringCalled = true;
            return "fallback";
        }

        public override string ToString()
        {
            _tracker.ToStringCalled = true;
            return "fallback";
        }
    }

    private sealed class CustomFormatter : IFormatProvider, ICustomFormatter
    {
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : null;

        public string Format(string? format, object? arg, IFormatProvider? formatProvider) => "custom";
    }
}