// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Adapted from System.Text.CompositeFormat for use on .NET Standard 2.1.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SerializableFormat;

/// <summary>Represents a parsed composite format string.</summary>
[DebuggerDisplay("{Format}")]
public sealed class SerializableCompositeFormat
{
    /// <summary>The parsed segments that make up the composite format string.</summary>
    /// <remarks>
    /// Every segment represents either a literal or a format hole, based on whether <c>Literal</c>
    /// is non-null or <c>ArgIndex</c> is non-negative.
    /// </remarks>
    internal readonly (string? Literal, int ArgIndex, int Alignment, string? Format)[] _segments;

    /// <summary>The sum of the lengths of all of the literals in <see cref="_segments"/>.</summary>
    internal readonly int _literalLength;

    /// <summary>The number of segments in <see cref="_segments"/> that represent format holes.</summary>
    internal readonly int _formattedCount;

    /// <summary>The number of args required to satisfy the format holes.</summary>
    /// <remarks>This is equal to one more than the largest index required by any format hole.</remarks>
    internal readonly int _argsRequired;

    /// <summary>Initializes the instance.</summary>
    /// <param name="format">The composite format string that was parsed.</param>
    /// <param name="segments">The parsed segments.</param>
    private SerializableCompositeFormat(string format, (string? Literal, int ArgIndex, int Alignment, string? Format)[] segments)
    {
        // Store the format.
        Debug.Assert(format is not null);
        Format = format;

        // Store the segments.
        Debug.Assert(segments is not null);
        _segments = segments;

        // Compute derivative information from the segments.
        var literalLength = 0;
        var formattedCount = 0;
        var argsRequired = 0;
        foreach (var segment in segments)
        {
            Debug.Assert(
                (segment.Literal is not null) ^ (segment.ArgIndex >= 0),
                "The segment should not represent both a literal and a format hole.");

            if (segment.Literal is string literal)
            {
                literalLength += literal.Length; // no concern about overflow as these were parsed out of a single string
            }
            else if (segment.ArgIndex >= 0)
            {
                formattedCount++;
                argsRequired = Math.Max(argsRequired, segment.ArgIndex + 1);
            }
        }

        // Store the derivative information.
        Debug.Assert(literalLength >= 0);
        Debug.Assert(formattedCount >= 0);
        Debug.Assert(formattedCount == 0 || argsRequired > 0);
        _literalLength = literalLength;
        _formattedCount = formattedCount;
        _argsRequired = argsRequired;
    }

    /// <summary>Initializes an instance from its serialized fields without reparsing or recomputing them.</summary>
    internal SerializableCompositeFormat(
        string format,
        (string? Literal, int ArgIndex, int Alignment, string? Format)[] segments,
        int literalLength,
        int formattedCount,
        int argsRequired)
    {
        Format = format;
        _segments = segments;
        _literalLength = literalLength;
        _formattedCount = formattedCount;
        _argsRequired = argsRequired;
    }

    /// <summary>Gets the original composite format string used to create this <see cref="SerializableCompositeFormat"/> instance.</summary>
    public string Format { get; }

    /// <summary>Gets the minimum number of arguments that must be passed to a formatting operation using this <see cref="SerializableCompositeFormat"/>.</summary>
    /// <remarks>It is permissible to supply more arguments than this value, but it is an error to pass fewer.</remarks>
    public int MinimumArgumentCount => _argsRequired;

    /// <summary>Parses the composite format string <paramref name="format"/>.</summary>
    /// <param name="format">The string to parse.</param>
    /// <returns>The parsed <see cref="SerializableCompositeFormat"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="format"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">A format item in <paramref name="format"/> is invalid.</exception>
    public static SerializableCompositeFormat Parse([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format)
    {
        ArgumentNullException.ThrowIfNull(format);

        var segments = new List<(string? Literal, int ArgIndex, int Alignment, string? Format)>();
        var failureOffset = 0;
        ParseFailureReason failureReason = default;
        if (!TryParseLiterals(format, segments, ref failureOffset, ref failureReason))
        {
            ThrowFormatInvalidString(failureOffset, failureReason);
        }

        return new SerializableCompositeFormat(format, segments.ToArray());
    }

    /// <summary>Parses the composite format string into segments.</summary>
    /// <param name="format">The format string.</param>
    /// <param name="segments">The list into which to store the segments.</param>
    /// <param name="failureOffset">The offset at which a parsing error occurred if <see langword="false"/> is returned.</param>
    /// <param name="failureReason">The reason for a parsing failure if <see langword="false"/> is returned.</param>
    /// <returns><see langword="true"/> if the format string can be parsed successfully; otherwise, <see langword="false"/>.</returns>
    private static bool TryParseLiterals(
        ReadOnlySpan<char> format,
        List<(string? Literal, int ArgIndex, int Alignment, string? Format)> segments,
        ref int failureOffset,
        ref ParseFailureReason failureReason)
    {
        // This parsing logic is copied from string.Format. It is the same code modified to not format
        // as part of parsing and instead store the parsed literals and argument specifiers (alignment
        // and format) for later use.

        // Rather than parsing directly into the segments list, literals are parsed into a reusable builder.
        // Due to the nature of the parsing logic copied from string.Format, and our desire not to veer from
        // it significantly in order to maintain compatibility and avoid accidental regression, multiple literals
        // next to each other might be parsed separately due to braces in between them. This builder then
        // allows us to merge those segments back together easily prior to their being appended to the list.
        var builder = new ValueStringBuilder(stackalloc char[256]);

        // Repeatedly find the next hole and process it.
        var pos = 0;
        char ch;
        while (true)
        {
            // Skip until either the end of the input or the first unescaped opening brace, whichever comes first.
            // Along the way we need to also unescape escaped closing braces.
            while (true)
            {
                // Find the next brace. If there is not one, the remainder of the input is text to be appended, and we are done.
                var remainder = format.Slice(pos);
                var countUntilNextBrace = remainder.IndexOfAny('{', '}');
                if (countUntilNextBrace < 0)
                {
                    builder.Append(remainder);
                    segments.Add((builder.ToString(), -1, 0, null));
                    return true;
                }

                // Append the text until the brace.
                builder.Append(remainder.Slice(0, countUntilNextBrace));
                pos += countUntilNextBrace;

                // Get the brace. It must be followed by another character, either a copy of itself in the case of being
                // escaped, or an arbitrary character that is part of the hole in the case of an opening brace.
                var brace = format[pos];
                if (!TryMoveNext(format, ref pos, out ch))
                {
                    goto FailureUnclosedFormatItem;
                }

                if (brace == ch)
                {
                    builder.Append(ch);
                    pos++;
                    continue;
                }

                // This was not an escape, so it must be an opening brace.
                if (brace != '{')
                {
                    goto FailureUnexpectedClosingBrace;
                }

                // Proceed to parse the hole.
                segments.Add((builder.ToString(), -1, 0, null));
                builder.Length = 0;
                break;
            }

            // We are now positioned just after the opening brace of an argument hole, which consists of
            // an opening brace, an index, an optional width preceded by a comma, and an optional format
            // preceded by a colon, with arbitrary amounts of spaces throughout.
            var width = 0;
            string? itemFormat = null;

            // First up is the index parameter, which is of the form:
            //     at least one digit
            //     optional any number of spaces
            // We have already read the first digit into ch.
            Debug.Assert(format[pos - 1] == '{');
            Debug.Assert(ch != '{');
            var index = ch - '0';
            if ((uint)index >= 10u)
            {
                goto FailureExpectedAsciiDigit;
            }

            // Common case is a single digit index followed by a closing brace. If it is not a closing brace,
            // proceed to finish parsing the full hole format.
            if (!TryMoveNext(format, ref pos, out ch))
            {
                goto FailureUnclosedFormatItem;
            }

            if (ch != '}')
            {
                // Continue consuming optional additional digits.
                while (IsAsciiDigit(ch))
                {
                    index = index * 10 + ch - '0';
                    if (!TryMoveNext(format, ref pos, out ch))
                    {
                        goto FailureUnclosedFormatItem;
                    }
                }

                // Consume optional whitespace.
                while (ch == ' ')
                {
                    if (!TryMoveNext(format, ref pos, out ch))
                    {
                        goto FailureUnclosedFormatItem;
                    }
                }

                // Parse the optional alignment, which is of the form:
                //     comma
                //     optional any number of spaces
                //     optional -
                //     at least one digit
                //     optional any number of spaces
                if (ch == ',')
                {
                    // Consume optional whitespace.
                    do
                    {
                        if (!TryMoveNext(format, ref pos, out ch))
                        {
                            goto FailureUnclosedFormatItem;
                        }
                    }
                    while (ch == ' ');

                    // Consume an optional minus sign indicating left alignment.
                    var leftJustify = 1;
                    if (ch == '-')
                    {
                        leftJustify = -1;
                        if (!TryMoveNext(format, ref pos, out ch))
                        {
                            goto FailureUnclosedFormatItem;
                        }
                    }

                    // Parse alignment digits. The read character must be a digit.
                    width = ch - '0';
                    if ((uint)width >= 10u)
                    {
                        goto FailureExpectedAsciiDigit;
                    }

                    if (!TryMoveNext(format, ref pos, out ch))
                    {
                        goto FailureUnclosedFormatItem;
                    }

                    while (IsAsciiDigit(ch))
                    {
                        width = width * 10 + ch - '0';
                        if (!TryMoveNext(format, ref pos, out ch))
                        {
                            goto FailureUnclosedFormatItem;
                        }
                    }

                    width *= leftJustify;

                    // Consume optional whitespace.
                    while (ch == ' ')
                    {
                        if (!TryMoveNext(format, ref pos, out ch))
                        {
                            goto FailureUnclosedFormatItem;
                        }
                    }
                }

                // The next character needs to either be a closing brace for the end of the hole,
                // or a colon indicating the start of the format.
                if (ch != '}')
                {
                    if (ch != ':')
                    {
                        goto FailureUnclosedFormatItem;
                    }

                    // Search for the closing brace; everything in between is the format,
                    // but opening braces are not allowed.
                    var startingPos = pos;
                    while (true)
                    {
                        if (!TryMoveNext(format, ref pos, out ch))
                        {
                            goto FailureUnclosedFormatItem;
                        }

                        if (ch == '}')
                        {
                            break;
                        }

                        if (ch == '{')
                        {
                            goto FailureUnclosedFormatItem;
                        }
                    }

                    startingPos++;
                    itemFormat = format.Slice(startingPos, pos - startingPos).ToString();
                }
            }

            Debug.Assert(format[pos] == '}');
            pos++;

            segments.Add((null, index, width, itemFormat));

            // Continue parsing the rest of the format string.
        }

    FailureUnexpectedClosingBrace:
        failureReason = ParseFailureReason.UnexpectedClosingBrace;
        failureOffset = pos;
        return false;

    FailureUnclosedFormatItem:
        failureReason = ParseFailureReason.UnclosedFormatItem;
        failureOffset = pos;
        return false;

    FailureExpectedAsciiDigit:
        failureReason = ParseFailureReason.ExpectedAsciiDigit;
        failureOffset = pos;
        return false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool TryMoveNext(ReadOnlySpan<char> format, ref int pos, out char nextChar)
        {
            pos++;
            if ((uint)pos >= (uint)format.Length)
            {
                nextChar = '\0';
                return false;
            }

            nextChar = format[pos];
            return true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAsciiDigit(char value) => (uint)(value - '0') < 10u;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowFormatInvalidString(int offset, ParseFailureReason reason)
    {
        var detail = reason switch
        {
            ParseFailureReason.UnexpectedClosingBrace => "Unexpected closing brace without a corresponding opening brace.",
            ParseFailureReason.UnclosedFormatItem => "Format item ends prematurely.",
            ParseFailureReason.ExpectedAsciiDigit => "Expected an ASCII digit.",
            _ => "The format item is invalid.",
        };

        throw new FormatException($"Input string was not in a correct format. Failure to parse near offset {offset}. {detail}");
    }

    private enum ParseFailureReason
    {
        None,
        UnexpectedClosingBrace,
        UnclosedFormatItem,
        ExpectedAsciiDigit,
    }
}