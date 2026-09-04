// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Adapted from the CompositeFormat formatting overloads in System.String, StringBuilder, and MemoryExtensions.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SerializableFormat;

/// <summary>Provides formatting operations for <see cref="SerializableCompositeFormat"/>.</summary>
public static class FormatExtensions
{
    private const int StackallocCharBufferSizeLimit = 256;

    extension(string)
    {
        /// <summary>Formats one argument using a parsed composite format.</summary>
        public static string Format<TArg0>(IFormatProvider? provider, SerializableCompositeFormat format, TArg0 arg0)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 1)
            {
                ThrowFormatIndexOutOfRange();
            }
            return FormatCore(provider, format, arg0, 0, 0, 0, 0, 0, 0, default);
        }

        /// <summary>Formats two arguments using a parsed composite format.</summary>
        public static string Format<TArg0, TArg1>(IFormatProvider? provider, SerializableCompositeFormat format, TArg0 arg0, TArg1 arg1)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 2)
            {
                ThrowFormatIndexOutOfRange();
            }
            return FormatCore(provider, format, arg0, arg1, 0, 0, 0, 0, 0, default);
        }

        /// <summary>Formats three arguments using a parsed composite format.</summary>
        public static string Format<TArg0, TArg1, TArg2>(IFormatProvider? provider, SerializableCompositeFormat format, TArg0 arg0, TArg1 arg1, TArg2 arg2)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 3)
            {
                ThrowFormatIndexOutOfRange();
            }
            return FormatCore(provider, format, arg0, arg1, arg2, 0, 0, 0, 0, default);
        }

        /// <summary>Formats four arguments using a parsed composite format.</summary>
        public static string Format<TArg0, TArg1, TArg2, TArg3>(IFormatProvider? provider, SerializableCompositeFormat format, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 4)
            {
                ThrowFormatIndexOutOfRange();
            }
            return FormatCore(provider, format, arg0, arg1, arg2, arg3, 0, 0, 0, default);
        }

        /// <summary>Formats five arguments using a parsed composite format.</summary>
        public static string Format<TArg0, TArg1, TArg2, TArg3, TArg4>(IFormatProvider? provider, SerializableCompositeFormat format, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 5)
            {
                ThrowFormatIndexOutOfRange();
            }
            return FormatCore(provider, format, arg0, arg1, arg2, arg3, arg4, 0, 0, default);
        }

        /// <summary>Formats six arguments using a parsed composite format.</summary>
        public static string Format<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5>(IFormatProvider? provider, SerializableCompositeFormat format, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 6)
            {
                ThrowFormatIndexOutOfRange();
            }
            return FormatCore(provider, format, arg0, arg1, arg2, arg3, arg4, arg5, 0, default);
        }

        /// <summary>Formats seven arguments using a parsed composite format.</summary>
        public static string Format<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(IFormatProvider? provider, SerializableCompositeFormat format, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 7)
            {
                ThrowFormatIndexOutOfRange();
            }
            return FormatCore(provider, format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, default);
        }

        /// <summary>Formats an array of arguments using a parsed composite format.</summary>
        public static string Format(IFormatProvider? provider, SerializableCompositeFormat format, params object?[] args)
        {
            ArgumentNullException.ThrowIfNull(format);
            ArgumentNullException.ThrowIfNull(args);

            return Format(provider, format, (ReadOnlySpan<object?>)args);
        }

        /// <summary>Formats a span of arguments using a parsed composite format.</summary>
        public static string Format(IFormatProvider? provider, SerializableCompositeFormat format, params ReadOnlySpan<object?> args)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > args.Length)
            {
                ThrowFormatIndexOutOfRange();
            }

            return args.Length switch
            {
                0 => format._literalLength == format.Format.Length
                    ? format.Format
                    : FormatCore(provider, format, (object?)null, 0, 0, 0, 0, 0, 0, args),
                1 => FormatCore(provider, format, args[0], 0, 0, 0, 0, 0, 0, args),
                2 => FormatCore(provider, format, args[0], args[1], 0, 0, 0, 0, 0, args),
                3 => FormatCore(provider, format, args[0], args[1], args[2], 0, 0, 0, 0, args),
                4 => FormatCore(provider, format, args[0], args[1], args[2], args[3], 0, 0, 0, args),
                5 => FormatCore(provider, format, args[0], args[1], args[2], args[3], args[4], 0, 0, args),
                6 => FormatCore(provider, format, args[0], args[1], args[2], args[3], args[4], args[5], 0, args),
                _ => FormatCore(provider, format, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args),
            };
        }
    }

    extension(StringBuilder builder)
    {
        /// <summary>Appends one formatted argument using a parsed composite format.</summary>
        public StringBuilder AppendFormat<TArg0>(IFormatProvider? provider, SerializableCompositeFormat format, TArg0 arg0)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 1)
            {
                ThrowFormatIndexOutOfRange();
            }
            return AppendFormatCore(builder, provider, format, arg0, 0, 0, 0, 0, 0, 0, default);
        }

        /// <summary>Appends two formatted arguments using a parsed composite format.</summary>
        public StringBuilder AppendFormat<TArg0, TArg1>(IFormatProvider? provider, SerializableCompositeFormat format, TArg0 arg0, TArg1 arg1)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 2)
            {
                ThrowFormatIndexOutOfRange();
            }
            return AppendFormatCore(builder, provider, format, arg0, arg1, 0, 0, 0, 0, 0, default);
        }

        /// <summary>Appends three formatted arguments using a parsed composite format.</summary>
        public StringBuilder AppendFormat<TArg0, TArg1, TArg2>(IFormatProvider? provider, SerializableCompositeFormat format, TArg0 arg0, TArg1 arg1, TArg2 arg2)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 3)
            {
                ThrowFormatIndexOutOfRange();
            }
            return AppendFormatCore(builder, provider, format, arg0, arg1, arg2, 0, 0, 0, 0, default);
        }

        /// <summary>Appends four formatted arguments using a parsed composite format.</summary>
        public StringBuilder AppendFormat<TArg0, TArg1, TArg2, TArg3>(IFormatProvider? provider, SerializableCompositeFormat format, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 4)
            {
                ThrowFormatIndexOutOfRange();
            }
            return AppendFormatCore(builder, provider, format, arg0, arg1, arg2, arg3, 0, 0, 0, default);
        }

        /// <summary>Appends five formatted arguments using a parsed composite format.</summary>
        public StringBuilder AppendFormat<TArg0, TArg1, TArg2, TArg3, TArg4>(IFormatProvider? provider, SerializableCompositeFormat format, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 5)
            {
                ThrowFormatIndexOutOfRange();
            }
            return AppendFormatCore(builder, provider, format, arg0, arg1, arg2, arg3, arg4, 0, 0, default);
        }

        /// <summary>Appends six formatted arguments using a parsed composite format.</summary>
        public StringBuilder AppendFormat<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5>(IFormatProvider? provider, SerializableCompositeFormat format, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 6)
            {
                ThrowFormatIndexOutOfRange();
            }
            return AppendFormatCore(builder, provider, format, arg0, arg1, arg2, arg3, arg4, arg5, 0, default);
        }

        /// <summary>Appends seven formatted arguments using a parsed composite format.</summary>
        public StringBuilder AppendFormat<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(IFormatProvider? provider, SerializableCompositeFormat format, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 7)
            {
                ThrowFormatIndexOutOfRange();
            }
            return AppendFormatCore(builder, provider, format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, default);
        }

        /// <summary>Appends an array of formatted arguments using a parsed composite format.</summary>
        public StringBuilder AppendFormat(IFormatProvider? provider, SerializableCompositeFormat format, params object?[] args)
        {
            ArgumentNullException.ThrowIfNull(format);
            ArgumentNullException.ThrowIfNull(args);

            return builder.AppendFormat(provider, format, (ReadOnlySpan<object?>)args);
        }

        /// <summary>Appends a span of formatted arguments using a parsed composite format.</summary>
        public StringBuilder AppendFormat(IFormatProvider? provider, SerializableCompositeFormat format, params ReadOnlySpan<object?> args)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > args.Length)
            {
                ThrowFormatIndexOutOfRange();
            }

            return args.Length switch
            {
                0 => AppendFormatCore(builder, provider, format, 0, 0, 0, 0, 0, 0, 0, args),
                1 => AppendFormatCore(builder, provider, format, args[0], 0, 0, 0, 0, 0, 0, args),
                2 => AppendFormatCore(builder, provider, format, args[0], args[1], 0, 0, 0, 0, 0, args),
                3 => AppendFormatCore(builder, provider, format, args[0], args[1], args[2], 0, 0, 0, 0, args),
                4 => AppendFormatCore(builder, provider, format, args[0], args[1], args[2], args[3], 0, 0, 0, args),
                5 => AppendFormatCore(builder, provider, format, args[0], args[1], args[2], args[3], args[4], 0, 0, args),
                6 => AppendFormatCore(builder, provider, format, args[0], args[1], args[2], args[3], args[4], args[5], 0, args),
                _ => AppendFormatCore(builder, provider, format, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args),
            };
        }
    }

    extension(Span<char> destination)
    {
        /// <summary>Tries to write one formatted argument to a character span.</summary>
        public bool TryWrite<TArg0>(IFormatProvider? provider, SerializableCompositeFormat format, out int charsWritten, TArg0 arg0)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 1)
            {
                ThrowFormatIndexOutOfRange();
            }
            return TryWriteCore(destination, provider, format, out charsWritten, arg0, 0, 0, 0, 0, 0, 0, default);
        }

        /// <summary>Tries to write two formatted arguments to a character span.</summary>
        public bool TryWrite<TArg0, TArg1>(IFormatProvider? provider, SerializableCompositeFormat format, out int charsWritten, TArg0 arg0, TArg1 arg1)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 2)
            {
                ThrowFormatIndexOutOfRange();
            }
            return TryWriteCore(destination, provider, format, out charsWritten, arg0, arg1, 0, 0, 0, 0, 0, default);
        }

        /// <summary>Tries to write three formatted arguments to a character span.</summary>
        public bool TryWrite<TArg0, TArg1, TArg2>(IFormatProvider? provider, SerializableCompositeFormat format, out int charsWritten, TArg0 arg0, TArg1 arg1, TArg2 arg2)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 3)
            {
                ThrowFormatIndexOutOfRange();
            }
            return TryWriteCore(destination, provider, format, out charsWritten, arg0, arg1, arg2, 0, 0, 0, 0, default);
        }

        /// <summary>Tries to write four formatted arguments to a character span.</summary>
        public bool TryWrite<TArg0, TArg1, TArg2, TArg3>(IFormatProvider? provider, SerializableCompositeFormat format, out int charsWritten, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 4)
            {
                ThrowFormatIndexOutOfRange();
            }
            return TryWriteCore(destination, provider, format, out charsWritten, arg0, arg1, arg2, arg3, 0, 0, 0, default);
        }

        /// <summary>Tries to write five formatted arguments to a character span.</summary>
        public bool TryWrite<TArg0, TArg1, TArg2, TArg3, TArg4>(IFormatProvider? provider, SerializableCompositeFormat format, out int charsWritten, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 5)
            {
                ThrowFormatIndexOutOfRange();
            }
            return TryWriteCore(destination, provider, format, out charsWritten, arg0, arg1, arg2, arg3, arg4, 0, 0, default);
        }

        /// <summary>Tries to write six formatted arguments to a character span.</summary>
        public bool TryWrite<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5>(IFormatProvider? provider, SerializableCompositeFormat format, out int charsWritten, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 6)
            {
                ThrowFormatIndexOutOfRange();
            }
            return TryWriteCore(destination, provider, format, out charsWritten, arg0, arg1, arg2, arg3, arg4, arg5, 0, default);
        }

        /// <summary>Tries to write seven formatted arguments to a character span.</summary>
        public bool TryWrite<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(IFormatProvider? provider, SerializableCompositeFormat format, out int charsWritten, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > 7)
            {
                ThrowFormatIndexOutOfRange();
            }
            return TryWriteCore(destination, provider, format, out charsWritten, arg0, arg1, arg2, arg3, arg4, arg5, arg6, default);
        }

        /// <summary>Tries to write an array of formatted arguments to a character span.</summary>
        public bool TryWrite(IFormatProvider? provider, SerializableCompositeFormat format, out int charsWritten, params object?[] args)
        {
            ArgumentNullException.ThrowIfNull(format);
            ArgumentNullException.ThrowIfNull(args);

            return destination.TryWrite(provider, format, out charsWritten, (ReadOnlySpan<object?>)args);
        }

        /// <summary>Tries to write a span of formatted arguments to a character span.</summary>
        public bool TryWrite(IFormatProvider? provider, SerializableCompositeFormat format, out int charsWritten, params ReadOnlySpan<object?> args)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.MinimumArgumentCount > args.Length)
            {
                ThrowFormatIndexOutOfRange();
            }

            return args.Length switch
            {
                0 => TryWriteCore(destination, provider, format, out charsWritten, 0, 0, 0, 0, 0, 0, 0, args),
                1 => TryWriteCore(destination, provider, format, out charsWritten, args[0], 0, 0, 0, 0, 0, 0, args),
                2 => TryWriteCore(destination, provider, format, out charsWritten, args[0], args[1], 0, 0, 0, 0, 0, args),
                3 => TryWriteCore(destination, provider, format, out charsWritten, args[0], args[1], args[2], 0, 0, 0, 0, args),
                4 => TryWriteCore(destination, provider, format, out charsWritten, args[0], args[1], args[2], args[3], 0, 0, 0, args),
                5 => TryWriteCore(destination, provider, format, out charsWritten, args[0], args[1], args[2], args[3], args[4], 0, 0, args),
                6 => TryWriteCore(destination, provider, format, out charsWritten, args[0], args[1], args[2], args[3], args[4], args[5], 0, args),
                _ => TryWriteCore(destination, provider, format, out charsWritten, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args),
            };
        }
    }

    private static string FormatCore<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(
        IFormatProvider? provider,
        SerializableCompositeFormat format,
        TArg0 arg0,
        TArg1 arg1,
        TArg2 arg2,
        TArg3 arg3,
        TArg4 arg4,
        TArg5 arg5,
        TArg6 arg6,
        ReadOnlySpan<object?> args)
    {
        if (format._formattedCount == 0 && format._literalLength == format.Format.Length)
        {
            return format.Format;
        }

        var handler = new DefaultInterpolatedStringHandler(
            format._literalLength,
            format._formattedCount,
            provider,
            stackalloc char[StackallocCharBufferSizeLimit]);

        AppendSegments(ref handler, format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, args);
        return handler.ToStringAndClear();
    }

    private static StringBuilder AppendFormatCore<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(
        StringBuilder builder,
        IFormatProvider? provider,
        SerializableCompositeFormat format,
        TArg0 arg0,
        TArg1 arg1,
        TArg2 arg2,
        TArg3 arg3,
        TArg4 arg4,
        TArg5 arg5,
        TArg6 arg6,
        ReadOnlySpan<object?> args)
    {
#if NETSTANDARD2_1
        var handler = new StringBuilderCompositeFormatHandler(
            format._literalLength,
            format._formattedCount,
            builder,
            provider);

        AppendSegments(ref handler, format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, args);
        return builder;
#else
        var handler = new StringBuilder.AppendInterpolatedStringHandler(
            format._literalLength,
            format._formattedCount,
            builder,
            provider);

        AppendSegments(ref handler, format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, args);
        return builder.Append(ref handler);
#endif
    }

    private static bool TryWriteCore<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(
        Span<char> destination,
        IFormatProvider? provider,
        SerializableCompositeFormat format,
        out int charsWritten,
        TArg0 arg0,
        TArg1 arg1,
        TArg2 arg2,
        TArg3 arg3,
        TArg4 arg4,
        TArg5 arg5,
        TArg6 arg6,
        ReadOnlySpan<object?> args)
    {
#if NETSTANDARD2_1
        var handler = new SpanCompositeFormatHandler(
            format._literalLength,
            format._formattedCount,
            destination,
            provider,
            out var shouldAppend);

        if (shouldAppend)
        {
            TryAppendSegments(ref handler, format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, args);
        }

        return handler.Complete(out charsWritten);
#else
        var handler = new MemoryExtensions.TryWriteInterpolatedStringHandler(
            format._literalLength,
            format._formattedCount,
            destination,
            provider,
            out var shouldAppend);

        if (shouldAppend)
        {
            TryAppendSegments(ref handler, format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, args);
        }

        return destination.TryWrite(provider, ref handler, out charsWritten);
#endif
    }

    private static void AppendSegments<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(
        ref DefaultInterpolatedStringHandler handler,
        SerializableCompositeFormat format,
        TArg0 arg0,
        TArg1 arg1,
        TArg2 arg2,
        TArg3 arg3,
        TArg4 arg4,
        TArg5 arg5,
        TArg6 arg6,
        ReadOnlySpan<object?> args)
    {
        foreach (var segment in format._segments)
        {
            if (segment.Literal is string literal)
            {
                handler.AppendLiteral(literal);
            }
            else
            {
                var index = segment.ArgIndex;
                switch (index)
                {
                    case 0:
                        handler.AppendFormatted(arg0, segment.Alignment, segment.Format);
                        break;
                    case 1:
                        handler.AppendFormatted(arg1, segment.Alignment, segment.Format);
                        break;
                    case 2:
                        handler.AppendFormatted(arg2, segment.Alignment, segment.Format);
                        break;
                    case 3:
                        handler.AppendFormatted(arg3, segment.Alignment, segment.Format);
                        break;
                    case 4:
                        handler.AppendFormatted(arg4, segment.Alignment, segment.Format);
                        break;
                    case 5:
                        handler.AppendFormatted(arg5, segment.Alignment, segment.Format);
                        break;
                    case 6:
                        handler.AppendFormatted(arg6, segment.Alignment, segment.Format);
                        break;
                    default:
                        Debug.Assert(index > 6);
                        handler.AppendFormatted(args[index], segment.Alignment, segment.Format);
                        break;
                }
            }
        }
    }

#if NETSTANDARD2_1
    private static void AppendSegments<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(
        ref StringBuilderCompositeFormatHandler handler,
        SerializableCompositeFormat format,
        TArg0 arg0,
        TArg1 arg1,
        TArg2 arg2,
        TArg3 arg3,
        TArg4 arg4,
        TArg5 arg5,
        TArg6 arg6,
        ReadOnlySpan<object?> args)
    {
        foreach (var segment in format._segments)
        {
            if (segment.Literal is string literal)
            {
                handler.AppendLiteral(literal);
            }
            else
            {
                var index = segment.ArgIndex;
                switch (index)
                {
                    case 0:
                        handler.AppendFormatted(arg0, segment.Alignment, segment.Format);
                        break;
                    case 1:
                        handler.AppendFormatted(arg1, segment.Alignment, segment.Format);
                        break;
                    case 2:
                        handler.AppendFormatted(arg2, segment.Alignment, segment.Format);
                        break;
                    case 3:
                        handler.AppendFormatted(arg3, segment.Alignment, segment.Format);
                        break;
                    case 4:
                        handler.AppendFormatted(arg4, segment.Alignment, segment.Format);
                        break;
                    case 5:
                        handler.AppendFormatted(arg5, segment.Alignment, segment.Format);
                        break;
                    case 6:
                        handler.AppendFormatted(arg6, segment.Alignment, segment.Format);
                        break;
                    default:
                        Debug.Assert(index > 6);
                        handler.AppendFormatted(args[index], segment.Alignment, segment.Format);
                        break;
                }
            }
        }
    }

    private static void TryAppendSegments<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(
        ref SpanCompositeFormatHandler handler,
        SerializableCompositeFormat format,
        TArg0 arg0,
        TArg1 arg1,
        TArg2 arg2,
        TArg3 arg3,
        TArg4 arg4,
        TArg5 arg5,
        TArg6 arg6,
        ReadOnlySpan<object?> args)
    {
        foreach (var segment in format._segments)
        {
            bool appended;
            if (segment.Literal is string literal)
            {
                appended = handler.AppendLiteral(literal);
            }
            else
            {
                var index = segment.ArgIndex;
                switch (index)
                {
                    case 0:
                        appended = handler.AppendFormatted(arg0, segment.Alignment, segment.Format);
                        break;
                    case 1:
                        appended = handler.AppendFormatted(arg1, segment.Alignment, segment.Format);
                        break;
                    case 2:
                        appended = handler.AppendFormatted(arg2, segment.Alignment, segment.Format);
                        break;
                    case 3:
                        appended = handler.AppendFormatted(arg3, segment.Alignment, segment.Format);
                        break;
                    case 4:
                        appended = handler.AppendFormatted(arg4, segment.Alignment, segment.Format);
                        break;
                    case 5:
                        appended = handler.AppendFormatted(arg5, segment.Alignment, segment.Format);
                        break;
                    case 6:
                        appended = handler.AppendFormatted(arg6, segment.Alignment, segment.Format);
                        break;
                    default:
                        Debug.Assert(index > 6);
                        appended = handler.AppendFormatted(args[index], segment.Alignment, segment.Format);
                        break;
                }
            }

            if (!appended)
            {
                break;
            }
        }
    }
#else
    private static void AppendSegments<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(
        ref StringBuilder.AppendInterpolatedStringHandler handler,
        SerializableCompositeFormat format,
        TArg0 arg0,
        TArg1 arg1,
        TArg2 arg2,
        TArg3 arg3,
        TArg4 arg4,
        TArg5 arg5,
        TArg6 arg6,
        ReadOnlySpan<object?> args)
    {
        foreach (var segment in format._segments)
        {
            if (segment.Literal is string literal)
            {
                handler.AppendLiteral(literal);
            }
            else
            {
                var index = segment.ArgIndex;
                switch (index)
                {
                    case 0:
                        handler.AppendFormatted(arg0, segment.Alignment, segment.Format);
                        break;
                    case 1:
                        handler.AppendFormatted(arg1, segment.Alignment, segment.Format);
                        break;
                    case 2:
                        handler.AppendFormatted(arg2, segment.Alignment, segment.Format);
                        break;
                    case 3:
                        handler.AppendFormatted(arg3, segment.Alignment, segment.Format);
                        break;
                    case 4:
                        handler.AppendFormatted(arg4, segment.Alignment, segment.Format);
                        break;
                    case 5:
                        handler.AppendFormatted(arg5, segment.Alignment, segment.Format);
                        break;
                    case 6:
                        handler.AppendFormatted(arg6, segment.Alignment, segment.Format);
                        break;
                    default:
                        Debug.Assert(index > 6);
                        handler.AppendFormatted(args[index], segment.Alignment, segment.Format);
                        break;
                }
            }
        }
    }

    private static void TryAppendSegments<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(
        ref MemoryExtensions.TryWriteInterpolatedStringHandler handler,
        SerializableCompositeFormat format,
        TArg0 arg0,
        TArg1 arg1,
        TArg2 arg2,
        TArg3 arg3,
        TArg4 arg4,
        TArg5 arg5,
        TArg6 arg6,
        ReadOnlySpan<object?> args)
    {
        foreach (var segment in format._segments)
        {
            bool appended;
            if (segment.Literal is string literal)
            {
                appended = handler.AppendLiteral(literal);
            }
            else
            {
                var index = segment.ArgIndex;
                switch (index)
                {
                    case 0:
                        appended = handler.AppendFormatted(arg0, segment.Alignment, segment.Format);
                        break;
                    case 1:
                        appended = handler.AppendFormatted(arg1, segment.Alignment, segment.Format);
                        break;
                    case 2:
                        appended = handler.AppendFormatted(arg2, segment.Alignment, segment.Format);
                        break;
                    case 3:
                        appended = handler.AppendFormatted(arg3, segment.Alignment, segment.Format);
                        break;
                    case 4:
                        appended = handler.AppendFormatted(arg4, segment.Alignment, segment.Format);
                        break;
                    case 5:
                        appended = handler.AppendFormatted(arg5, segment.Alignment, segment.Format);
                        break;
                    case 6:
                        appended = handler.AppendFormatted(arg6, segment.Alignment, segment.Format);
                        break;
                    default:
                        Debug.Assert(index > 6);
                        appended = handler.AppendFormatted(args[index], segment.Alignment, segment.Format);
                        break;
                }
            }

            if (!appended)
            {
                break;
            }
        }
    }
#endif

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowFormatIndexOutOfRange() =>
        throw new FormatException("Index (zero based) must be greater than or equal to zero and less than the size of the argument list.");
}