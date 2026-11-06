<a id="readme-top"></a>
<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://github.com/pippinmole/CsvMole.NET">
    <img src="media/logo.png" alt="Logo" width="178" height="71">
  </a>

<h3 align="center">CsvMole.NET</h3>
  <p align="center">
    A source generated CSV Parser for .NET
    <br />
    <br />
    <a href="https://github.com/pippinmole/CsvMole.NET/issues/new?assignees=&labels=&projects=&template=bug_report.md&title=">Report Bug</a>
    ·
    <a href="https://github.com/pippinmole/CsvMole.NET/issues/new?assignees=&labels=&projects=&template=feature_request.md&title=">Request Feature</a>
  </p>
</div>

<!-- GETTING STARTED -->
## Getting Started

There is currently no package on NuGet, but you can clone the repo and build the project yourself. Requires .NET 10.

Reference `src/CsvMole` as a project and `src/CsvMole.SourceGenerators` as an analyzer
(`OutputItemType="Analyzer" ReferenceOutputAssembly="false"`). `src/CsvMole.Example` shows the setup.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- USAGE EXAMPLES -->
## Usage

Declare a `partial` parser class with `partial` methods; the generator writes the bodies.

```csharp
using System.Globalization;
using CsvMole;

[CsvParser]
public partial class OrderParser
{
    public partial IEnumerable<Order> Parse(TextReader reader, CsvOptions? options = null);

    public partial IAsyncEnumerable<Order> ParseAsync(TextReader reader, CsvOptions? options = null,
        CancellationToken cancellationToken = default);
}

// "$12.50" -> 12.50m
public sealed class DollarConverter : CsvConverter<decimal>
{
    public override bool TryParse(ReadOnlySpan<char> value, CultureInfo culture, out decimal result) =>
        decimal.TryParse(value.TrimStart('$'), NumberStyles.Number, culture, out result);
}

public sealed record Order(
    [property: CsvColumn("id")] int Id,
    [property: CsvColumn("placed")] DateOnly Placed,
    [property: CsvColumn("total"), CsvConverter(typeof(DollarConverter))] decimal Total,
    [property: CsvColumn("note")] string? Note);
```

```csharp
var parser = new OrderParser();

using (var reader = new StreamReader("orders.csv"))
    foreach (var order in parser.Parse(reader))
        Console.WriteLine(order);

// Async, skipping bad rows instead of throwing
var options = new CsvOptions { OnError = e => Console.WriteLine($"skipped: {e}") };
using (var reader = new StreamReader("orders.csv"))
    await foreach (var order in parser.ParseAsync(reader, options, cancellationToken))
        Console.WriteLine(order);
```

### Mapping

- `[CsvColumn("name")]` maps a member to a header column (case-insensitive).
- `[CsvOrder(n)]` maps a member to a zero-based column position.
- `[CsvDelimiter(';')]` on the model sets its delimiter (a `char`; default `,`).
- `[CsvConverter(typeof(C))]` uses a custom `CsvConverter<T>`. For a nullable member, an empty field is passed to the converter (not turned into `null`); a missing column gives `null`.
- Models can be records, classes with constructors (mark one with `[CsvConstructor]` if ambiguous), or use `required` and `init` members.

The returned `IEnumerable<T>` / `IAsyncEnumerable<T>` streams the reader and can be enumerated only once.

Parsed automatically: `string`, numbers, `bool`, `char`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`,
`TimeSpan`, `Guid`, enums, `Nullable<T>` of these, and any `ISpanParsable<T>`.

### Options

| `CsvOptions` | Default | |
|---|---|---|
| `HasHeader` | `true` | First row is a header |
| `Delimiter` | from `[CsvDelimiter]`, else `,` | |
| `Culture` | `CultureInfo.InvariantCulture` | Used for parsing values |
| `SkipBlankLines` | `true` | |
| `TrimFields` | `false` | Trim whitespace outside quotes |
| `OnError` | `null` | `null` throws `CsvParseException`; otherwise the row is skipped and the callback gets a `CsvError` |
| `MaxRecordLength` | 1 MiB | |

## Performance

Benchmarked against [Sep](https://github.com/nietras/Sep), Sylvan and CsvHelper on Sep's datasets (PackageAssets
from [NCsvPerf](https://github.com/joelverhagen/NCsvPerf), and Floats), 1,000,000 rows each, BenchmarkDotNet default job,
.NET 10, Ryzen 7 7800X3D. Ratio is time relative to Sep (lower is faster); CsvMole allocates the same as Sep on all of them.

| Dataset | CsvMole vs Sep |
|---|---|
| PackageAssets | 1.12x |
| PackageAssetsQuotes | 1.05x |
| PackageAssetsSpacesQuotes | 1.09x |
| Floats (40 columns) | 0.92x |

The benchmarks live in `src/CsvMole.Benchmarks`. See [ROADMAP.md](ROADMAP.md) for what is planned.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- CONTRIBUTING -->
## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

<p align="right">(<a href="#readme-top">back to top</a>)</p>


<!-- LICENSE -->
## License

Distributed under the MIT License. See [LICENSE](LICENSE) for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>
