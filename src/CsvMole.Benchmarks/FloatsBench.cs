using System.Buffers;
using System.Globalization;
using BenchmarkDotNet.Attributes;
using HelperOptions = CsvHelper.CsvReaderOptions;
using nietras.SeparatedValues;
using CsvMole.Benchmarks.Data;
using CsvMole.Benchmarks.Models;
using SepLib = nietras.SeparatedValues.Sep;
using HelperReader = CsvHelper.CsvReader;
using SylvanReader = Sylvan.Data.Csv.CsvDataReader;

namespace CsvMole.Benchmarks;

/// <summary>
/// Sep's Floats dataset (20 features, so 40 float columns): every library parses every field of every row into a
/// <see cref="FloatsRow"/>. Rows are not collected (as in Sep's Floats benchmark); the first and last escape.
/// </summary>
[MemoryDiagnoser]
public class FloatsBench
{
    const int FeatureCount = 20;
    const int Cols = 2 * FeatureCount;
    static readonly BenchParsers Parsers = new();
    static readonly CsvOptions MoleOptions = new();
    string _text = "";

    [Params(1_000_000)]
    public int Rows { get; set; } = 1_000_000;

    [GlobalSetup]
    public void Setup()
    {
        _text = FloatsData.Generate(Rows, FeatureCount);
        Sanity.Check(Rows, Summary("Sep", Sep()), Summary("Sylvan", Sylvan()), Summary("CsvHelper", CsvHelper()),
            Summary("CsvMole", CsvMole()));
    }

    public readonly record struct Result(int Count, FloatsRow? First, FloatsRow? Last);

    static (string, int, FloatsRow?) Summary(string name, Result r) => (name, r.Count, r.First);

    [Benchmark(Baseline = true)]
    public Result Sep()
    {
        Span<float> v = stackalloc float[Cols];
        FloatsRow? first = null, last = null;
        var count = 0;
        using var reader = SepLib.Reader(o => o with { Sep = new SepLib(';') }).From(new StringReader(_text));
        foreach (var row in reader)
        {
            for (var i = 0; i < Cols; i++) v[i] = row[i].Parse<float>();
            last = From(v);
            first ??= last;
            count++;
        }
        return new(count, first, last);
    }

    [Benchmark]
    public Result Sylvan()
    {
        Span<float> v = stackalloc float[Cols];
        FloatsRow? first = null, last = null;
        var count = 0;
        var buffer = ArrayPool<char>.Shared.Rent(32 * 1024);
        try
        {
            using var r = SylvanReader.Create(new StringReader(_text), buffer, new() { Delimiter = ';' });
            while (r.Read())
            {
                for (var i = 0; i < Cols; i++) v[i] = r.GetFloat(i);
                last = From(v);
                first ??= last;
                count++;
            }
        }
        finally { ArrayPool<char>.Shared.Return(buffer); }
        return new(count, first, last);
    }

    [Benchmark]
    public Result CsvHelper()
    {
        Span<float> v = stackalloc float[Cols];
        FloatsRow? first = null, last = null;
        var count = 0;
        Span<char> c = stackalloc char[64];
        var options = new HelperOptions { Separator = ';', TrackLineNumbers = true };
        using var r = HelperReader.Create(new StringReader(_text), options);
        while (r.Read())
        {
            for (var i = 0; i < Cols; i++) v[i] = float.Parse(c[..r.GetChars(i, c)], CultureInfo.InvariantCulture);
            last = From(v);
            first ??= last;
            count++;
        }
        return new(count, first, last);
    }

    [Benchmark]
    public Result CsvMole()
    {
        FloatsRow? first = null, last = null;
        var count = 0;
        foreach (var row in Parsers.ParseFloats(new StringReader(_text), MoleOptions))
        {
            last = row;
            first ??= last;
            count++;
        }
        return new(count, first, last);
    }

    static FloatsRow From(ReadOnlySpan<float> v) => new()
    {
        GT_Feature0 = v[0],
        GT_Feature1 = v[1],
        GT_Feature2 = v[2],
        GT_Feature3 = v[3],
        GT_Feature4 = v[4],
        GT_Feature5 = v[5],
        GT_Feature6 = v[6],
        GT_Feature7 = v[7],
        GT_Feature8 = v[8],
        GT_Feature9 = v[9],
        GT_Feature10 = v[10],
        GT_Feature11 = v[11],
        GT_Feature12 = v[12],
        GT_Feature13 = v[13],
        GT_Feature14 = v[14],
        GT_Feature15 = v[15],
        GT_Feature16 = v[16],
        GT_Feature17 = v[17],
        GT_Feature18 = v[18],
        GT_Feature19 = v[19],
        RE_Feature0 = v[20],
        RE_Feature1 = v[21],
        RE_Feature2 = v[22],
        RE_Feature3 = v[23],
        RE_Feature4 = v[24],
        RE_Feature5 = v[25],
        RE_Feature6 = v[26],
        RE_Feature7 = v[27],
        RE_Feature8 = v[28],
        RE_Feature9 = v[29],
        RE_Feature10 = v[30],
        RE_Feature11 = v[31],
        RE_Feature12 = v[32],
        RE_Feature13 = v[33],
        RE_Feature14 = v[34],
        RE_Feature15 = v[35],
        RE_Feature16 = v[36],
        RE_Feature17 = v[37],
        RE_Feature18 = v[38],
        RE_Feature19 = v[39],
    };
}
