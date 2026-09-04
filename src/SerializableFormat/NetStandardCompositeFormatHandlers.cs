// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Adapted from StringBuilder.AppendInterpolatedStringHandler and MemoryExtensions.TryWriteInterpolatedStringHandler.

#if NETSTANDARD2_1
using System.Text;

namespace SerializableFormat;

internal struct StringBuilderCompositeFormatHandler
{
    private const int StackallocCharBufferSizeLimit = 256;

    private readonly StringBuilder _builder;
    private readonly IFormatProvider? _provider;
    private readonly bool _hasCustomFormatter;

    internal StringBuilderCompositeFormatHandler(
        int literalLength,
        int formattedCount,
        StringBuilder builder,
        IFormatProvider? provider)
    {
        _ = literalLength;
        _ = formattedCount;
        _builder = builder;
        _provider = provider;
        _hasCustomFormatter = provider is not null && provider.GetFormat(typeof(ICustomFormatter)) is not null;
    }

    internal void AppendLiteral(string value) => _builder.Append(value);

    internal void AppendFormatted<T>(T value, int alignment, string? format)
    {
        if (alignment == 0)
        {
            AppendFormatted(value, format);
        }
        else if (alignment < 0)
        {
            var startingPosition = _builder.Length;
            AppendFormatted(value, format);
            var paddingRequired = -alignment - (_builder.Length - startingPosition);
            if (paddingRequired > 0)
            {
                _builder.Append(' ', paddingRequired);
            }
        }
        else
        {
            var formatted = new ValueStringBuilder(stackalloc char[StackallocCharBufferSizeLimit]);
            FormatIntoTemporarySpace(ref formatted, value, format);
            AppendFormatted(formatted.AsSpan(), alignment);
        }
    }

    private void AppendFormatted<T>(T value, string? format)
    {
        if (_hasCustomFormatter)
        {
            AppendCustomFormatter(value, format);
            return;
        }

        if (value is null)
        {
            return;
        }

        if (value is IFormattable)
        {
            if (value is ISpanFormattable spanFormattable)
            {
                var formatted = new ValueStringBuilder(stackalloc char[StackallocCharBufferSizeLimit]);
                FormatSpanFormattable(ref formatted, spanFormattable, format);
                _builder.Append(formatted.AsSpan());
            }
            else
            {
                _builder.Append(((IFormattable)value).ToString(format, _provider));
            }

            return;
        }

        _builder.Append(value.ToString());
    }

    private void AppendCustomFormatter<T>(T value, string? format)
    {
        var formatter = (ICustomFormatter?)_provider?.GetFormat(typeof(ICustomFormatter));
        if (formatter?.Format(format, value, _provider) is string customFormatted)
        {
            _builder.Append(customFormatted);
        }
    }

    private void AppendFormatted(ReadOnlySpan<char> value, int alignment)
    {
        var leftAlign = false;
        if (alignment < 0)
        {
            leftAlign = true;
            alignment = -alignment;
        }

        var paddingRequired = alignment - value.Length;
        if (paddingRequired <= 0)
        {
            _builder.Append(value);
        }
        else if (leftAlign)
        {
            _builder.Append(value);
            _builder.Append(' ', paddingRequired);
        }
        else
        {
            _builder.Append(' ', paddingRequired);
            _builder.Append(value);
        }
    }

    private void FormatIntoTemporarySpace<T>(ref ValueStringBuilder destination, T value, string? format)
    {
        if (_hasCustomFormatter)
        {
            var formatter = (ICustomFormatter?)_provider?.GetFormat(typeof(ICustomFormatter));
            if (formatter?.Format(format, value, _provider) is string customFormatted)
            {
                destination.Append(customFormatted);
            }

            return;
        }

        if (value is null)
        {
            return;
        }

        if (value is ISpanFormattable spanFormattable)
        {
            FormatSpanFormattable(ref destination, spanFormattable, format);
            return;
        }

        var text = value is IFormattable formattable
            ? formattable.ToString(format, _provider)
            : value.ToString();
        if (text is not null)
        {
            destination.Append(text);
        }
    }

    private void FormatSpanFormattable(ref ValueStringBuilder destination, ISpanFormattable value, string? format)
    {
        while (true)
        {
            var available = destination.GetAppendSpan();
            if (value.TryFormat(available, out var charsWritten, format, _provider))
            {
                if ((uint)charsWritten > (uint)available.Length)
                {
                    throw new FormatException("Input string was not in a correct format.");
                }

                destination.Advance(charsWritten);
                return;
            }

            destination.Grow();
        }
    }
}

internal ref struct SpanCompositeFormatHandler
{
    private readonly Span<char> _destination;
    private readonly IFormatProvider? _provider;
    private readonly bool _hasCustomFormatter;
    private int _position;
    private bool _success;

    internal SpanCompositeFormatHandler(
        int literalLength,
        int formattedCount,
        Span<char> destination,
        IFormatProvider? provider,
        out bool shouldAppend)
    {
        _ = formattedCount;
        _destination = destination;
        _provider = provider;
        _position = 0;
        _success = shouldAppend = destination.Length >= literalLength;
        _hasCustomFormatter = provider is not null && provider.GetFormat(typeof(ICustomFormatter)) is not null;
    }

    internal bool AppendLiteral(string value)
    {
        if (value.AsSpan().TryCopyTo(_destination.Slice(_position)))
        {
            _position += value.Length;
            return true;
        }

        return Fail();
    }

    internal bool AppendFormatted<T>(T value, int alignment, string? format)
    {
        var startingPosition = _position;
        if (!AppendFormatted(value, format))
        {
            return false;
        }

        return alignment == 0 || AppendOrInsertAlignmentIfNeeded(startingPosition, alignment);
    }

    internal bool Complete(out int charsWritten)
    {
        charsWritten = _success ? _position : 0;
        return _success;
    }

    private bool AppendFormatted<T>(T value, string? format)
    {
        if (_hasCustomFormatter)
        {
            return AppendCustomFormatter(value, format);
        }

        if (value is ISpanFormattable spanFormattable)
        {
            if (spanFormattable.TryFormat(
                _destination.Slice(_position),
                out var charsWritten,
                format,
                _provider))
            {
                _position += charsWritten;
                return true;
            }

            return Fail();
        }

        var text = value is IFormattable formattable
            ? formattable.ToString(format, _provider)
            : value?.ToString();
        return text is null || AppendLiteral(text);
    }

    private bool AppendCustomFormatter<T>(T value, string? format)
    {
        var formatter = (ICustomFormatter?)_provider?.GetFormat(typeof(ICustomFormatter));
        return formatter?.Format(format, value, _provider) is not string customFormatted
            || AppendLiteral(customFormatted);
    }

    private bool AppendOrInsertAlignmentIfNeeded(int startingPosition, int alignment)
    {
        var charsWritten = _position - startingPosition;
        var leftAlign = false;
        if (alignment < 0)
        {
            leftAlign = true;
            alignment = -alignment;
        }

        var paddingNeeded = alignment - charsWritten;
        if (paddingNeeded <= 0)
        {
            return true;
        }

        if (paddingNeeded > _destination.Length - _position)
        {
            return Fail();
        }

        if (leftAlign)
        {
            _destination.Slice(_position, paddingNeeded).Fill(' ');
        }
        else
        {
            _destination.Slice(startingPosition, charsWritten)
                .CopyTo(_destination.Slice(startingPosition + paddingNeeded));
            _destination.Slice(startingPosition, paddingNeeded).Fill(' ');
        }

        _position += paddingNeeded;
        return true;
    }

    private bool Fail()
    {
        _success = false;
        return false;
    }
}
#endif