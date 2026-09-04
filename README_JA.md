# SerializableFormat

[![dotnet-test](https://github.com/AndanteTribe/SerializableFormat/actions/workflows/dotnet-test.yml/badge.svg)](https://github.com/AndanteTribe/SerializableFormat/actions/workflows/dotnet-test.yml)
[![nuget](https://img.shields.io/nuget/v/AndanteTribe.SerializableFormat.svg)](https://www.nuget.org/packages/AndanteTribe.SerializableFormat/)
[![Releases](https://img.shields.io/github/release/AndanteTribe/SerializableFormat.svg)](https://github.com/AndanteTribe/SerializableFormat/releases)
[![GitHub license](https://img.shields.io/github/license/AndanteTribe/SerializableFormat.svg)](./LICENSE)

[English](README.md) | 日本語

## 概要

**SerializableFormat** は、.NET 8 の `System.Text.CompositeFormat` に代わる、シリアライズ可能で .NET Standard 2.1 互換の .NET ライブラリです。

主な機能：

1. `SerializableCompositeFormat` — 複合書式文字列を一度解析し、元の `Format` と `MinimumArgumentCount` を公開します。
2. `FormatExtensions` — 解析済みの `SerializableCompositeFormat` に対して、`string.Format`・`StringBuilder.AppendFormat`・`Span<char>.TryWrite` と同様の操作を提供します。
3. `SerializableFormat.Json` — 解析済みの状態を `System.Text.Json` で保持するオプションパッケージです。
4. `SerializableFormat.MessagePack` — 解析済みの状態を MessagePack で保持するオプションパッケージです。

シリアライズ用パッケージは、解析済みのセグメント配列と BCL 実装で使用される 3 つの派生フィールドを保存します。デシリアライズ時は `Format` を再解析せず、その状態を直接復元します。

## インストール

### NuGet パッケージ

このライブラリは .NET Standard 2.1 以降と互換性のあるターゲットで利用できます。パッケージは NuGet から取得できます。

### .NET CLI

#### コアパッケージ

```ps1
dotnet add package AndanteTribe.SerializableFormat
```

#### System.Text.Json サポート（オプション）

```ps1
dotnet add package AndanteTribe.SerializableFormat.Json
```

#### MessagePack サポート（オプション）

```ps1
dotnet add package AndanteTribe.SerializableFormat.MessagePack
```

### パッケージマネージャー

#### コアパッケージ

```ps1
Install-Package AndanteTribe.SerializableFormat
```

#### System.Text.Json サポート（オプション）

```ps1
Install-Package AndanteTribe.SerializableFormat.Json
```

#### MessagePack サポート（オプション）

```ps1
Install-Package AndanteTribe.SerializableFormat.MessagePack
```

## クイックスタート

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

`SerializableCompositeFormat` は、解析済みの複合書式文字列を表します。`Parse` を一度呼び出した後、その結果をフォーマットやシリアライズで再利用できます。

- `Format` は元の複合書式文字列を返します。
- `MinimumArgumentCount` は、書式から参照される最大の引数インデックスに 1 を加えた値を返します。
- `Parse(string)` は、BCL の複合書式パーサーと互換性のある動作で、複合書式項目・アラインメント・書式指定子・エスケープされた波かっこを処理します。

```csharp
var format = SerializableCompositeFormat.Parse(
    "{{Order}} {0}: {1,-12:C2}");
```

## フォーマット

コアパッケージは、3 系統のフォーマット操作を提供します。それぞれに 1～7 個の引数向けジェネリックオーバーロードと、`object?[]`・`ReadOnlySpan<object?>` 向けオーバーロードがあります。

C# 14 では、静的拡張を `string.Format` から呼び出せます。以前の C# バージョンを使用する場合も、格納クラスを直接呼び出すことで利用できます：

```csharp
using SerializableFormat;
using System.Globalization;

var format = SerializableCompositeFormat.Parse("Order {0}: {1,10:N2}");

// C# 14 の静的拡張構文
var text = string.Format(
    CultureInfo.InvariantCulture,
    format,
    42,
    123.45m);

// 以前の C# バージョン向けの直接呼び出し
var compatibleText = FormatExtensions.Format(
    CultureInfo.InvariantCulture,
    format,
    42,
    123.45m);
```

同じ解析済み書式を `StringBuilder` に追記したり、出力先のスパンへ書き込んだりできます：

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

3 系統すべてで、指定された `IFormatProvider`（カスタムの `ICustomFormatter` を含む）、アラインメント、書式指定子が使用されます。

## System.Text.Json サポート

シリアライザーオプションに `CompositeFormatJsonConverter` を登録します：

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

## MessagePack サポート

`CompositeFormatResolver` は `SerializableCompositeFormat` 用のフォーマッターを解決します：

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

アプリケーションですでに別のリゾルバーを使用している場合は、アプリケーションの既存リゾルバーより前に、複合リゾルバーへ `CompositeFormatMessagePackFormatter.Instance` を登録してください。

## シリアライズされる状態

JSON 表現は、`Format`・`_segments`・`_literalLength`・`_formattedCount`・`_argsRequired` の各プロパティをこの順序で書き込みます。各セグメントは、`Literal`・`ArgIndex`・`Alignment`・`Format` を格納する 4 要素の配列です。

MessagePack は、同じ 5 つのトップレベル値を同じ順序の配列として保存します。各セグメントにも同じ 4 要素の配列表現を使用します。

前方互換性のため、JSON リーダーは未知のオブジェクトプロパティと、セグメント配列末尾の余分な値を無視します。MessagePack リーダーは、トップレベル配列とセグメント配列の両方で末尾の余分な値を読み飛ばします。

デシリアライズ時は 5 つの値をすべて直接復元します。`Format` の再解析、派生値の再計算、フィールド間の整合性検証は行いません。

## ライセンス

このライブラリは MIT ライセンスのもとで公開されています。
