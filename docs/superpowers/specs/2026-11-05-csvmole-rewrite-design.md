# CsvMole rewrite — design

Date: 2026-11-05
Status: draft for review

## Intent

CsvMole is a CSV parser for .NET where the user declares a partial parser class and the code that turns rows into
objects is source generated at compile time. The rewrite starts from scratch. The current implementation splits lines
with `string.Split`, so it can't handle quoted fields, it ignores `[CsvOrder]`, and it fails on common inputs.

Why use source generation at all:

- **No reflection at runtime**, so it is compatible with Native AOT and trimming, and has no startup cost.
- **Errors at compile time** for models that cannot be parsed.
- **Building immutable models directly** (records, constructors, `init`/`required`), which reflection-based libraries
  find awkward.

Raw speed comes from the hand-written tokenizer, not from generation. The goal is to be competitive with Sep and
Sylvan on the existing benchmarks.

## The guiding rule

**Generate only what depends on the shape of the user's model. Hand-write everything else.**

- Hand-written runtime library: tokenizing, buffering, header reading, error reporting, options, the converter base.
  These can be tested on their own, and fixes ship through a package update without regenerating user code.
- Generated: the body of each parser method, the mapping from columns to members, the parse call for each member,
  how the object is constructed, and compile-time diagnostics.

## Goals

- Reading CSV only, following RFC 4180: quoted fields, `""` escapes, line breaks inside quoted fields, CRLF / LF / CR
  line endings.
- Input from a `TextReader`, with both sync (`IEnumerable<T>`) and async (`IAsyncEnumerable<T>`) methods. Async
  supports cancellation.
- Columns mapped to members by position (`[CsvOrder]`), by header name, or both.
- Models can be mutable classes, records, classes with constructors, and `init`/`required` members.
- Bad rows throw by default. Users can opt into skipping them or reporting them through a callback.
- Performance within 20–30% of Sep on Sep's own datasets (see "Benchmarks and performance target").
- Zero reflection on the parse path. Verified by a Native AOT test project.

## Non-goals (this rewrite)

These are all listed in `ROADMAP.md`:

- Writing CSV.
- Parsing UTF-8 bytes (`Stream` / `ReadOnlySpan<byte>`) without a `TextReader`.
- Delimiters longer than one character.
- Letting a column be missing when it maps to a nullable member.

## Anti-goals (failure even if it works)

- Data silently landing in the wrong member, for example because declaration order was used instead of the mapping.
- Parsing that depends on the machine's locale.
- The generator crashing with an opaque "generator failed" warning instead of a clear diagnostic.
- Any reflection or `Activator.CreateInstance` on the parse path.
- Unbounded memory use on malformed input, such as an unterminated quote in a multi-GB file.

## Constraints

- Runtime library: `net10.0` only. .NET 8 and 9 reach end of support on 2026-11-10; .NET 10 is the current LTS.
- Generator: `netstandard2.0`. Roslyn requires this. It must be an `IIncrementalGenerator` using
  `ForAttributeWithMetadataName`, and its pipeline models must be equatable, holding no `ISymbol` or `SyntaxNode`, so
  that caching works.
- Single NuGet package, `CsvMole`: the runtime library plus the generator under `analyzers/dotnet/cs`. The
  `CsvMole.Abstractions` project is folded into the runtime library. Because the generated code and the runtime ship
  together, their versions always match.
- No new runtime dependencies. Test-only dependencies (for example `Verify.SourceGenerators`) are fine.

## User-facing API

Roughly the current API, with deliberate changes.

```csharp
[CsvParser]
public partial class CustomParser
{
    public partial IEnumerable<CustomModel> Parse(TextReader reader, CsvOptions? options = null);

    public partial IAsyncEnumerable<CustomModel> ParseAsync(
        TextReader reader, CsvOptions? options = null, CancellationToken cancellationToken = default);
}

[CsvDelimiter(';')]
public sealed class CustomModel
{
    [CsvOrder(0)]
    public required string Id { get; init; }

    [CsvColumn("date")]
    [CsvConverter(typeof(MyDateConverter))]
    public DateTime? Date { get; set; }

    [CsvOrder(2), CsvColumn("age")]
    public int Age { get; set; }
}

public record Person(
    [property: CsvColumn("name")] string Name,
    [property: CsvColumn("born")] DateOnly Born);
```

### Parser class and methods

- `[CsvParser]` goes on a `partial` class. Any accessibility is allowed, and it can be nested (the generator emits the
  containing partial types). A generic parser class is a diagnostic.
- A parser method is any `partial` method declared without a body in a `[CsvParser]` class. Other methods are ignored.
  This replaces the current rule of "every public method that returns a value".
- Supported signatures:
  - Returns `IEnumerable<T>` or `IAsyncEnumerable<T>`.
  - First parameter: `TextReader`. `StringReader` callers still compile.
  - Optional `CsvOptions?` parameter.
  - Optional `CancellationToken` parameter (async only). The generator applies `[EnumeratorCancellation]` to it in the
    implementation, so both `ParseAsync(r, o, ct)` and `ParseAsync(r).WithCancellation(ct)` work.
- The generated code uses the parameter names the user declared.
- Anything else is a diagnostic, not a crash.

### Mapping columns to members

- `[CsvOrder(n)]` maps a member to column index `n` (zero-based).
- `[CsvColumn("name")]` maps a member to the column with that header.
- Both can go on the same member, on properties and fields, and on constructor parameters.
- Members with neither attribute are not mapped (mapping is opt-in). *(My call; see "Calls made".)*
- **Resolution at runtime, once per file:**
  - If `HasHeader` is true, the header row is read. Members with `[CsvColumn]` are matched to header names. Matching is
    case-insensitive (ordinal ignore-case), and header cells are trimmed. If a member has both attributes, the header
    name wins.
  - If `HasHeader` is false, members are mapped by `[CsvOrder]`. A member that only has `[CsvColumn]` can't be
    resolved, so the parser throws `CsvParseException` before reading any rows.
  - A required column missing from the header throws immediately, naming the column. It does not wait until row 1.
    "Required" means **every member with `[CsvColumn]`**, nullable or not (optional columns are on the roadmap).
  - **No fallback:** if a member has both attributes and its header name isn't found, the parser throws. It does
    **not** fall back to the `[CsvOrder]` index, because a silent fallback is how data ends up in the wrong member.
  - A member with only `[CsvOrder]` maps to that absolute column index even when a header exists.
  - If the file's header contains a name twice and a member maps to that name, the parser throws (ambiguous). Duplicate
    header names that no member maps to are ignored.
  - **Empty input:** if `HasHeader` is true and the input has no header row (it's empty, or contains only blank lines),
    the parser returns an empty sequence and doesn't throw. Leading blank lines are skipped before the header when
    `SkipBlankLines` is true.
  - The result is a small `int[]` from member to column index, built once. Reading each row uses that fixed array.
- **Checks at compile time:** duplicate `[CsvOrder]` indices, duplicate `[CsvColumn]` names (ignoring case), negative
  indices.

### Building the model

- **Choosing a constructor:** if there is one public constructor, use it. If there are several, use the one marked
  `[CsvConstructor]`; if none is marked, that's a diagnostic listing the candidates. A record's primary constructor
  counts as one of the public constructors.
- **Constructor parameters** are matched to members by name, ignoring case (like `System.Text.Json`). Mapping
  attributes on the property (`[property: ...]`) or on the parameter both work.
- Remaining settable or `init` members that are mapped are assigned in an object initializer.
- A `required` member that isn't mapped is a diagnostic.
- A mapped member that can't be assigned (get-only and not a constructor parameter) is a diagnostic.
- Each field is parsed into a local variable first, then the object is constructed in one expression.

### Parsing values

The generator chooses the parse call for each member at compile time:

| Member type | Generated parse |
|---|---|
| `string` | `field.ToString()`. An empty field gives `""`. |
| `string?` (nullable annotated) | An empty field gives `null`, otherwise `field.ToString()`. |
| `bool`, `char`, all integer types, `float`, `double`, `decimal`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, `Guid` | `T.Parse(field, culture)` / `T.TryParse(...)` using the culture from options. |
| `enum` | `Enum.TryParse<T>(field, ignoreCase: true, out ...)`. |
| Any other `T : ISpanParsable<T>` | `T.TryParse(field, culture, out ...)`. |
| `Nullable<T>` of any of the above | An empty field gives `null`, otherwise as for `T`. |
| Member with `[CsvConverter(typeof(C))]` | Calls a cached instance of `C`. |
| Anything else | A compile-time diagnostic. |

- An empty field for a non-nullable value type is a parse error.
- Use `TryParse` forms, so that failures go through the error policy rather than exceptions on the hot path.

### Converters

```csharp
public abstract class CsvConverter<T>
{
    public abstract bool TryParse(ReadOnlySpan<char> value, CultureInfo culture, out T result);
}
```

- Converters work on spans only. The `string` overload and `ConvertToString` are removed (writing is on the roadmap).
- The converter must derive from `CsvConverter<TMember>` and have a public parameterless constructor. Both are checked
  at compile time.
- Each distinct converter type gets one `static readonly` instance per parser class. Its field name comes from the
  fully qualified type name, so two converters with the same short name don't clash.
- A converter returning `false` is a parse error that goes through the error policy.
- `CsvDateTimeConverter` is dropped. `DateTime` is now built in.

### Options

```csharp
public sealed class CsvOptions
{
    public static CsvOptions Default { get; }
    public bool HasHeader { get; init; } = true;
    public char? Delimiter { get; init; }          // null = use [CsvDelimiter] or ','
    public CultureInfo Culture { get; init; } = CultureInfo.InvariantCulture;
    public bool SkipBlankLines { get; init; } = true;
    public bool TrimFields { get; init; } = false; // never trims inside quotes
    public Action<CsvError>? OnError { get; init; } // null = throw
    public int MaxRecordLength { get; init; } = 1024 * 1024; // in chars
}
```

- A class rather than a struct, because it now holds a culture and a callback. Passing `null` means `Default`.
- **Delimiter:** `[CsvDelimiter(char)]` on the model sets the default and `CsvOptions.Delimiter` overrides it.
  Otherwise it is `','`. The attribute now takes a `char`, not a `string`.
- **`HasHeader` defaults to true.** *(My call; the current default is false. See "Calls made".)*
- **Error policy:** `OnError == null` throws `CsvParseException`. Otherwise the callback is called with a `CsvError`
  and the row is skipped. Skipping silently is `OnError = static _ => { }`.
- `MaxRecordLength` caps how far the buffer can grow for one record. Exceeding it is an error, which prevents
  unbounded memory use on, for example, an unterminated quote.

### Errors

- `CsvError` contains: line number (the physical line where the record starts, 1-based), column index, header name if
  known, member name, raw field text (truncated if long), and a reason (`ParseFailed`, `TooFewColumns`,
  `UnterminatedQuote`, `RecordTooLong`).
- `CsvParseException` wraps a `CsvError`, and its message includes all of these details.
- **Too many columns:** extra columns are ignored.
- **Too few columns:** an error, unless every missing column maps to a nullable member, in which case those members
  are `null`.
- `UnterminatedQuote` and `RecordTooLong` always throw, even if `OnError` is set, because the reader can't reliably
  recover from them.
- A quote in the middle of an unquoted field (`ab"c`) is kept as a literal character (lenient, like most parsers).
- With `TrimFields = true`, whitespace before an opening quote is skipped, and the field is then read as quoted.
  So `a,  "b,c"` gives `a` and `b,c`. With `TrimFields = false`, the RFC rule applies: `  "b` is an unquoted field and
  the quote is literal.
- A field read from a reader that has been cancelled throws `OperationCanceledException`, not an error routed to
  `OnError`.

## Runtime library (hand-written)

Namespace `CsvMole`. *(My call; the current namespace is `CsvMole.Abstractions.*`.)*

### Tokenizer / reader

- `CsvReader` reads from a `TextReader` into a `char[]` buffer rented from `ArrayPool<char>.Shared`. The buffer grows
  up to `MaxRecordLength` when a record doesn't fit, and is returned on dispose.
- It scans with `SearchValues<char>`, built once per reader, containing the delimiter, `"`, `\r` and `\n`.
  `IndexOfAny` uses SIMD internally.
  - **Fast path:** a record with no quote is split by the indices found.
  - **Slow path:** quoted fields are unescaped (`""` → `"`) into a scratch buffer only when they contain an escaped
    quote. Otherwise the field is a slice of the input buffer.
- It exposes the current record as field spans plus the record's starting line number. Field boundaries are stored in
  a reused `int[]`, so there is no allocation per row on the fast path.
- `Read()` advances one record. `ReadAsync(CancellationToken)` refills the buffer with
  `TextReader.ReadAsync(Memory<char>, ct)` when needed. Cancellation is checked once per buffer refill.
- **Async constraint:** spans can't live across an `await`. The generated async method does
  `while (await reader.ReadAsync(ct))`, then calls a **synchronous** generated method to bind the row, which receives
  the reader and reads spans. The binding code is shared between the sync and async methods.
- `CsvReader` is public, because the code generated into the user's assembly calls it. It is documented as a
  low-level API. *(My call: keep it minimal. Whether it is promoted as a standalone API is left open.)*

### Other runtime types

`CsvOptions`, `CsvError`, `CsvParseException`, `CsvConverter<T>`, the attributes (`CsvParser`, `CsvOrder`,
`CsvColumn`, `CsvDelimiter`, `CsvConverter`, `CsvConstructor`), and a small helper for header resolution that the
generated code calls (takes the header row and the list of names, returns an `int[]`).

## Generator

- One generated file per parser class, named `{Namespace}.{ClassName}.g.cs` to avoid collisions.
- The generated code starts with `// <auto-generated/>` and `#nullable enable`, uses `global::`-qualified names
  everywhere, and carries `[GeneratedCode("CsvMole", version)]`.
- For each method it emits:
  - The method body.
  - One shared, private, synchronous `Bind{Model}` per distinct model type in the class.
  - A static array of header names per model.
  - The converter fields.
- The pipeline is `ForAttributeWithMetadataName` → an equatable model (strings and `EquatableArray`s only) → emit.
- Diagnostics are reported through `ReportDiagnostic` with stable IDs. The generator never throws. If a method has an
  error, a stub that throws `NotSupportedException` is generated, so the user sees only the diagnostic and not cascading
  "partial method has no implementation" errors.

### Diagnostics (initial set)

| ID | Severity | Condition |
|---|---|---|
| CSVM001 | Error | Parser class not partial, or generic |
| CSVM002 | Error | Unsupported parser method signature |
| CSVM003 | Error | Member type has no built-in parse and no converter |
| CSVM004 | Error | Duplicate `[CsvOrder]` index or `[CsvColumn]` name |
| CSVM005 | Error | Mapped member can't be assigned |
| CSVM006 | Error | `required` member is not mapped |
| CSVM007 | Error | Ambiguous constructor (several public constructors, none marked `[CsvConstructor]`) |
| CSVM008 | Error | Converter type is invalid (wrong `T`, no parameterless constructor, doesn't derive from `CsvConverter<T>`) |
| CSVM009 | Warning | Async parser method has no `CancellationToken` parameter |
| CSVM010 | Warning | Model has no mapped members |

## Repository layout

The old code under `src/` and `test/` is replaced.

```
src/CsvMole/                       net10.0 runtime library (the package)
src/CsvMole.SourceGenerators/      netstandard2.0, packed into CsvMole under analyzers/
src/CsvMole.Benchmarks/            kept; updated to the new API; compares against CsvHelper, Sep, Sylvan
src/CsvMole.Example/               updated to the new API
test/CsvMole.Tests/                tokenizer + end-to-end parse tests
test/CsvMole.SourceGenerators.Tests/  snapshot tests of generated code + diagnostic tests
test/CsvMole.AotTests/             PublishAot=true console app run in CI; fails on any trim/AOT warning
ROADMAP.md
```

CI (`.github/workflows/test-ci.yml`): only the `10.0.x` SDK, runs all test projects, publishes the AOT test, and fixes
the coverage upload path (it currently points to a path that doesn't exist).

## Testing

- **Tokenizer:** RFC 4180 cases (quotes, escaped quotes, line breaks inside quotes, CRLF/LF/CR, empty fields, trailing
  delimiter, file without a final newline, empty file, header only), records that cross a buffer boundary (test with a
  tiny buffer), `MaxRecordLength`, unterminated quote, line numbers when records contain line breaks.
- **Generator:** snapshot tests of the generated code for each model shape (mutable class, record, constructor plus
  initializer, `required`, nested parser class, converter, nullable types, enums). One test per diagnostic.
- **End-to-end:** parse through the generated parsers, covering header mapping (reordered columns, extra columns,
  missing column), each error policy, cancellation, culture.
- **AOT:** the AOT project publishes with zero warnings and parses a sample file.
- **Benchmarks:** run manually, not in CI. Results are recorded in the PR that lands the tokenizer. See "Benchmarks
  and performance target".

## Benchmarks and performance target

Datasets are the same as Sep's comparison benchmarks, so results are directly comparable with Sep's published numbers:

| Dataset | Source | Exercises |
|---|---|---|
| `PackageAssets` | NCsvPerf's `PackageAssets.csv` (MIT), 25 columns, 1,000,000 rows | the fast path, string-heavy |
| `PackageAssetsQuotes` | the same data with quotes around some columns (generated as Sep does) | the quoted slow path |
| `PackageAssetsSpacesQuotes` | the same data with spaces around fields and inside quotes | `TrimFields` + quotes |
| `Floats` | generated with a fixed random seed, many float columns (as Sep's `FloatsTestData`) | number parsing |

- `PackageAssets.csv` is downloaded from
  `https://raw.githubusercontent.com/joelverhagen/NCsvPerf/main/NCsvPerf/TestData/PackageAssets.csv` on first run and
  cached locally (git-ignored). It is not committed.
- Each benchmark parses into a model (a `PackageAsset` class with 25 properties, and a floats model) and compares Sep,
  Sylvan and CsvHelper against CsvMole. It uses sync parsing from a `TextReader`, `[MemoryDiagnoser]`, and .NET 10.
- **Target (the tokenizer milestone is done when this is met):**
  - `PackageAssets` and `Floats`: mean time **within 20% of Sep**.
  - `PackageAssetsQuotes` and `PackageAssetsSpacesQuotes`: **within 30% of Sep**.
  - Allocations: only the model objects and their strings. No allocation per field or per row beyond that.
- The old tiny benchmarks (2 columns, ≤1,000 rows) are replaced.

## Left to the builder

- The internal structure of the tokenizer, provided it meets the behaviour and allocation rules above.
- The exact shape of the generated code, provided it follows the rules above.
- The default and initial buffer sizes.
- Wording of diagnostic messages.
- Order of work. Suggested order: runtime tokenizer and its tests first, then the generator, then benchmarks.

## Calls made (not explicitly agreed)

- Members without a mapping attribute are not mapped (opt-in).
- `HasHeader` defaults to `true`.
- Namespace becomes `CsvMole`. Attributes and converters move out of `CsvMole.Abstractions`.
- `CsvOptions` becomes a class. `OnError` is a single callback rather than a mode enum plus a callback.
- `UnterminatedQuote` and `RecordTooLong` always throw.
- `string` gives `""` for an empty field, and `string?` gives `null`.
- `CsvReader` is public but documented as low-level.
- `CsvDateTimeConverter` is removed.
- Header mapping is strict: no fallback to `[CsvOrder]` when a name is missing, every `[CsvColumn]` column is
  required, and a duplicate header name that a member maps to throws.
- Empty input with `HasHeader = true` returns an empty sequence.
- `TrimFields` skips whitespace before an opening quote.
