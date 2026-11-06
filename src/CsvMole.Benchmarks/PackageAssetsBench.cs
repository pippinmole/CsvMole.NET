using System.Buffers;
using BenchmarkDotNet.Attributes;
using CsvMole.Benchmarks.Data;
using CsvMole.Benchmarks.Models;
using nietras.SeparatedValues;
using SepLib = nietras.SeparatedValues.Sep;
using HelperReader = CsvHelper.CsvReader;
using HelperOptions = CsvHelper.CsvReaderOptions;
using SylvanReader = Sylvan.Data.Csv.CsvDataReader;
using SylvanOptions = Sylvan.Data.Csv.CsvDataReaderOptions;
using static CsvMole.Benchmarks.Models.PackageAsset;

namespace CsvMole.Benchmarks;

public enum AssetsDataset { PackageAssets, PackageAssetsQuotes, PackageAssetsSpacesQuotes }

[CsvParser]
public partial class BenchParsers
{
    public partial IEnumerable<PackageAsset> ParseAssets(TextReader reader, CsvOptions? options = null);
    public partial IEnumerable<FloatsRow> ParseFloats(TextReader reader, CsvOptions? options = null);
}

/// <summary>
/// Every library parses every field of every row into <see cref="PackageAsset"/> (no string pooling) and collects the
/// models in a list, as Sep's Asset benchmarks do. Quoted variants unescape; the spaces variant trims outside quotes.
/// </summary>
[MemoryDiagnoser]
public class PackageAssetsBench
{
    static readonly BenchParsers Parsers = new();
    string _text = "";
    bool _quotes, _spaces;
    CsvOptions _moleOptions = CsvOptions.Default;

    [Params(AssetsDataset.PackageAssets, AssetsDataset.PackageAssetsQuotes, AssetsDataset.PackageAssetsSpacesQuotes)]
    public AssetsDataset Dataset { get; set; }

    [Params(1_000_000)]
    public int Rows { get; set; } = 1_000_000;

    [GlobalSetup]
    public void Setup()
    {
        _quotes = Dataset != AssetsDataset.PackageAssets;
        _spaces = Dataset == AssetsDataset.PackageAssetsSpacesQuotes;
        _text = PackageAssetsData.Get(Rows, _quotes, _spaces);
        _moleOptions = new CsvOptions { HasHeader = false, TrimFields = _spaces };

        Sanity.Check(Rows,
            ("Sep", Sep()), ("Sylvan", Sylvan()), ("CsvHelper", CsvHelper()), ("CsvMole", CsvMole()));
    }

    [Benchmark(Baseline = true)]
    public List<PackageAsset> Sep()
    {
        var list = new List<PackageAsset>();
        using var reader = SepLib.Reader(o => o with
        {
            HasHeader = false,
            Unescape = _quotes,
            Trim = _spaces ? SepTrim.Outer : SepTrim.None,
        }).From(new StringReader(_text));
        foreach (var row in reader)
        {
            list.Add(new PackageAsset
            {
                ScanId = ParseNullableGuid(row[0].Span),
                ScanTimestamp = ParseNullableDateTimeOffset(row[1].Span),
                Id = row[2].ToString(),
                Version = row[3].ToString(),
                Created = ParseDateTimeOffset(row[4].Span),
                ResultType = row[5].ToString(),
                PatternSet = row[6].ToString(),
                PropertyAnyValue = row[7].ToString(),
                PropertyCodeLanguage = row[8].ToString(),
                PropertyTargetFrameworkMoniker = row[9].ToString(),
                PropertyLocale = row[10].ToString(),
                PropertyManagedAssembly = row[11].ToString(),
                PropertyMSBuild = row[12].ToString(),
                PropertyRuntimeIdentifier = row[13].ToString(),
                PropertySatelliteAssembly = row[14].ToString(),
                Path = row[15].ToString(),
                FileName = row[16].ToString(),
                FileExtension = row[17].ToString(),
                TopLevelFolder = row[18].ToString(),
                RoundTripTargetFrameworkMoniker = row[19].ToString(),
                FrameworkName = row[20].ToString(),
                FrameworkVersion = row[21].ToString(),
                FrameworkProfile = row[22].ToString(),
                PlatformName = row[23].ToString(),
                PlatformVersion = row[24].ToString(),
            });
        }
        return list;
    }

    [Benchmark]
    public List<PackageAsset> Sylvan()
    {
        var list = new List<PackageAsset>();
        var buffer = ArrayPool<char>.Shared.Rent(32 * 1024);
        try
        {
            // Sylvan has no trim option, so the spaces variant reads with quoting off and trims/unquotes here.
            var options = new SylvanOptions { HasHeaders = false, Quote = _spaces ? '\0' : '"' };
            using var r = SylvanReader.Create(new StringReader(_text), buffer, options);
            while (r.Read())
            {
                list.Add(new PackageAsset
                {
                    ScanId = ParseNullableGuid(r.GetFieldSpan(0)),
                    ScanTimestamp = ParseNullableDateTimeOffset(r.GetFieldSpan(1)),
                    Id = SylvanString(r, 2),
                    Version = SylvanString(r, 3),
                    Created = ParseDateTimeOffset(r.GetFieldSpan(4)),
                    ResultType = SylvanString(r, 5),
                    PatternSet = SylvanString(r, 6),
                    PropertyAnyValue = SylvanString(r, 7),
                    PropertyCodeLanguage = SylvanString(r, 8),
                    PropertyTargetFrameworkMoniker = SylvanString(r, 9),
                    PropertyLocale = SylvanString(r, 10),
                    PropertyManagedAssembly = SylvanString(r, 11),
                    PropertyMSBuild = SylvanString(r, 12),
                    PropertyRuntimeIdentifier = SylvanString(r, 13),
                    PropertySatelliteAssembly = SylvanString(r, 14),
                    Path = SylvanString(r, 15),
                    FileName = SylvanString(r, 16),
                    FileExtension = SylvanString(r, 17),
                    TopLevelFolder = SylvanString(r, 18),
                    RoundTripTargetFrameworkMoniker = SylvanString(r, 19),
                    FrameworkName = SylvanString(r, 20),
                    FrameworkVersion = SylvanString(r, 21),
                    FrameworkProfile = SylvanString(r, 22),
                    PlatformName = SylvanString(r, 23),
                    PlatformVersion = SylvanString(r, 24),
                });
            }
        }
        finally { ArrayPool<char>.Shared.Return(buffer); }
        return list;
    }

    string SylvanString(SylvanReader r, int i) => _spaces ? TrimUnquote(r.GetFieldSpan(i)).ToString() : r.GetString(i);

    // shortcut: no "" unescaping, fine because PackageAssets.csv has no quotes of its own (the sanity check would catch it).
    static ReadOnlySpan<char> TrimUnquote(ReadOnlySpan<char> s)
    {
        s = s.Trim();
        return s.Length >= 2 && s[0] == '"' && s[^1] == '"' ? s[1..^1] : s;
    }

    [Benchmark]
    public List<PackageAsset> CsvHelper()
    {
        var list = new List<PackageAsset>();
        Span<char> c = stackalloc char[64]; // CsvHelper 34 exposes UTF-8 spans; parsed fields are copied out as UTF-16
        var options = new HelperOptions { HasHeader = false, TrackLineNumbers = true, TrimWhitespace = _spaces };
        using var r = HelperReader.Create(new StringReader(_text), options);
        while (r.Read())
        {
            list.Add(new PackageAsset
            {
                ScanId = ParseNullableGuid(c[..r.GetChars(0, c)]),
                ScanTimestamp = ParseNullableDateTimeOffset(c[..r.GetChars(1, c)]),
                Id = r.GetString(2),
                Version = r.GetString(3),
                Created = ParseDateTimeOffset(c[..r.GetChars(4, c)]),
                ResultType = r.GetString(5),
                PatternSet = r.GetString(6),
                PropertyAnyValue = r.GetString(7),
                PropertyCodeLanguage = r.GetString(8),
                PropertyTargetFrameworkMoniker = r.GetString(9),
                PropertyLocale = r.GetString(10),
                PropertyManagedAssembly = r.GetString(11),
                PropertyMSBuild = r.GetString(12),
                PropertyRuntimeIdentifier = r.GetString(13),
                PropertySatelliteAssembly = r.GetString(14),
                Path = r.GetString(15),
                FileName = r.GetString(16),
                FileExtension = r.GetString(17),
                TopLevelFolder = r.GetString(18),
                RoundTripTargetFrameworkMoniker = r.GetString(19),
                FrameworkName = r.GetString(20),
                FrameworkVersion = r.GetString(21),
                FrameworkProfile = r.GetString(22),
                PlatformName = r.GetString(23),
                PlatformVersion = r.GetString(24),
            });
        }
        return list;
    }

    [Benchmark]
    public List<PackageAsset> CsvMole()
    {
        var list = new List<PackageAsset>();
        foreach (var asset in Parsers.ParseAssets(new StringReader(_text), _moleOptions))
            list.Add(asset);
        return list;
    }
}

static class Sanity
{
    /// <summary>Throws unless every library returned <paramref name="rows"/> records and equal first records.</summary>
    public static void Check<T>(int rows, params (string Name, List<T> Result)[] results) =>
        Check(rows, results.Select(r => (r.Name, r.Result.Count, r.Result.Count > 0 ? r.Result[0] : default)).ToArray());

    public static void Check<T>(int rows, params (string Name, int Count, T? First)[] results)
    {
        var (refName, _, refFirst) = results[0];
        foreach (var (name, count, first) in results)
        {
            if (count != rows)
                throw new InvalidOperationException($"{name} returned {count} records, expected {rows}.");
            if (!EqualityComparer<T>.Default.Equals(first, refFirst))
                throw new InvalidOperationException($"{name} record 0 differs from {refName}:\n{first}\n{refFirst}");
        }
        Console.WriteLine($"// Sanity check passed: {results.Length} libraries, {rows} records, record 0 equal.");
    }
}
