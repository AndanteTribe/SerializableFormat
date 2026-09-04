// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Polyfills ArgumentNullException.ThrowIfNull for .NET Standard 2.1.

#if NETSTANDARD2_1
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SerializableFormat;

internal static class ArgumentNullExceptionExtensions
{
    extension(ArgumentNullException)
    {
        public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
            {
                throw new ArgumentNullException(paramName);
            }
        }
    }
}
#endif