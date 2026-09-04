# SerializableFormat

[![dotnet-test](https://github.com/AndanteTribe/SerializableFormat/actions/workflows/dotnet-test.yml/badge.svg)](https://github.com/AndanteTribe/SerializableFormat/actions/workflows/dotnet-test.yml)
[![nuget](https://img.shields.io/nuget/v/AndanteTribe.SerializableFormat.svg)](https://www.nuget.org/packages/AndanteTribe.SerializableFormat/)
[![Releases](https://img.shields.io/github/release/AndanteTribe/SerializableFormat.svg)](https://github.com/AndanteTribe/SerializableFormat/releases)
[![GitHub license](https://img.shields.io/github/license/AndanteTribe/SerializableFormat.svg)](./LICENSE)

English | [日本語](README_JA.md)

## Overview

**SerializableFormat** is a .NET library that provides a serializable, .NET Standard 2.1-compatible alternative to .NET 8's `System.Text.CompositeFormat`.

It provides the following:

1. `SerializableCompositeFormat` — Parses a composite format string once and exposes its original `Format` and `MinimumArgumentCount`.
2. `FormatExtensions` — Provides `string.Format`, `StringBuilder.AppendFormat`, and `Span<char>.TryWrite`-style operations for a parsed `SerializableCompositeFormat`.
3. `SerializableFormat.Json` — An optional package that preserves the parsed state with `System.Text.Json`.
4. `SerializableFormat.MessagePack` — An optional package that preserves the parsed state with MessagePack.

The serialization packages store the parsed segment array and the three derived fields used by the BCL implementation. Deserialization restores that state directly without parsing `Format` again.

## Installation

### NuGet Packages

This library requires a target compatible with .NET Standard 2.1 or later. The packages can be obtained from NuGet.

### .NET CLI

#### Core package

```ps1
dotnet add package AndanteTribe.SerializableFormat
```

#### System.Text.Json support (optional)

```ps1
dotnet add package AndanteTribe.SerializableFormat.Json
```

#### MessagePack support (optional)

```ps1
dotnet add package AndanteTribe.SerializableFormat.MessagePack
```

### Package Manager

#### Core package

```ps1
Install-Package AndanteTribe.SerializableFormat
```

#### System.Text.Json support (optional)

```ps1
Install-Package AndanteTribe.SerializableFormat.Json
```

#### MessagePack support (optional)

```ps1
Install-Package AndanteTribe.SerializableFormat.MessagePack
```

## Quick Start

```csharp
using SerializableFormat;
using System.Globalization;

var format = SerializableCompositeFormat.Parse("Order {0}: {1,10:N2}");

Console.WriteLine(format.Format);               // "Order {0}: {1,10:N2}"
Console.WriteLine(format.MinimumArgumentCount); // 2

var text = string.Format(
    CultureInfo.InvariantCulture,
    format,
    42,
    123.45m);

Console.WriteLine(text);
```

## SerializableCompositeFormat

`SerializableCompositeFormat` represents a parsed composite format string. Call `Parse` once, then reuse the result for formatting or serialization.

- `Format` returns the original composite format string.
- `MinimumArgumentCount` returns one more than the largest argument index referenced by the format.
- `Parse(string)` supports composite format items, alignment, format specifiers, and escaped braces with behavior compatible with the BCL composite-format parser.

```csharp
var format = SerializableCompositeFormat.Parse(
    "{{Order}} {0}: {1,-12:C2}");
```

## Formatting

The core package provides three families of formatting operations. Each family includes generic overloads for one through seven arguments, plus `object?[]` and `ReadOnlySpan<object?>` overloads.

With C# 14, the static extension can be called through `string.Format`. Calling the containing class directly also works for consumers using older C# versions:

```csharp
using SerializableFormat;
using System.Globalization;

var format = SerializableCompositeFormat.Parse("Order {0}: {1,10:N2}");

// C# 14 static extension syntax
var text = string.Format(
    CultureInfo.InvariantCulture,
    format,
    42,
    123.45m);

// Direct call for older C# versions
var compatibleText = FormatExtensions.Format(
    CultureInfo.InvariantCulture,
    format,
    42,
    123.45m);
```

The same parsed format can be appended to a `StringBuilder` or written to a destination span:

```csharp
using System.Text;

var builder = new StringBuilder();
builder.AppendFormat(
    CultureInfo.InvariantCulture,
    format,
    42,
    123.45m);

Span<char> destination = stackalloc char[64];
if (destination.TryWrite(
        CultureInfo.InvariantCulture,
        format,
        out var charsWritten,
        42,
        123.45m))
{
    Console.WriteLine(destination[..charsWritten]);
}
```

All three operation families honor the supplied `IFormatProvider`, including a custom `ICustomFormatter`, as well as alignment and format specifiers.

## System.Text.Json Support

Register `CompositeFormatJsonConverter` in the serializer options:

```csharp
using SerializableFormat;
using SerializableFormat.Json;
using System.Text.Json;

var options = new JsonSerializerOptions();
options.Converters.Add(new CompositeFormatJsonConverter());

var format = SerializableCompositeFormat.Parse("Order {0}: {1,10:N2}");
var json = JsonSerializer.Serialize(format, options);
var restored = JsonSerializer.Deserialize<SerializableCompositeFormat>(json, options)!;
```

## MessagePack Support

`CompositeFormatResolver` resolves the formatter for `SerializableCompositeFormat`:

```csharp
using MessagePack;
using SerializableFormat;
using SerializableFormat.MessagePack;

var options = MessagePackSerializerOptions.Standard
    .WithResolver(CompositeFormatResolver.Instance);

var format = SerializableCompositeFormat.Parse("Order {0}: {1,10:N2}");
var bytes = MessagePackSerializer.Serialize(format, options);
var restored = MessagePackSerializer.Deserialize<SerializableCompositeFormat>(bytes, options);
```

If the application already uses another resolver, register `CompositeFormatMessagePackFormatter.Instance` in a composite resolver ahead of the application's existing resolvers.

## Serialized State

The JSON representation writes these properties in order: `Format`, `_segments`, `_literalLength`, `_formattedCount`, and `_argsRequired`. Each segment is a four-element array containing `Literal`, `ArgIndex`, `Alignment`, and `Format`.

MessagePack stores the same five top-level values in the same order as an array. Each segment uses the same four-element array representation.

For forward compatibility, the JSON reader ignores unknown object properties and extra values at the end of segment arrays. The MessagePack reader skips trailing values in both top-level and segment arrays.

Deserialization restores all five values directly. It does not reparse `Format`, recompute derived values, or validate that the fields agree with each other.

## License

This library is released under the MIT license.
