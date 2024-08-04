using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CsvHelper;
using CsvMole.Benchmarks.Models;
using nietras.SeparatedValues;
using Sylvan.Data;

namespace CsvMole.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser(false)]
public sealed class ParserBenchmarks
{
    private string _content = null!;
    private string _contentFastPath = null!;

    private readonly CustomParserExample _customParserExample = new();
    private readonly CustomParserExample1 _customParserExample1 = new();
    
    [Params(1, 100, 1_000)] //, 100_000
    public int N { get; set; }
    
    [GlobalSetup]
    public void Setup()
    {
        _content = "Id,Date\n";
        _contentFastPath = "Id,Date\n";
        
        for (var i = 0; i < N; i++)
        {
            _content += $"{i},{DateTime.Now:yyyy-M-d dddd}\n";
            _contentFastPath += $"{i},{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
        }
    }
    
    [Benchmark]
    public List<CustomParserModel> SourceGeneratedParse()
    {
        var stringReader = new StringReader(_content);
        return _customParserExample.Parse(stringReader).ToList();
    }
    
    [Benchmark]
    public List<CustomParserModel> SourceGeneratedParse_New()
    {
        var stringReader = new StringReader(_content);
        return _customParserExample1.Parse1(stringReader).ToList();
    }
    
    [Benchmark]
    public List<CsvHelperModel> CsvHelperParse()
    {
        var stringReader = new StringReader(_content);
        using var csvReader = new CsvReader(stringReader, CultureInfo.InvariantCulture);
        return csvReader.GetRecords<CsvHelperModel>().ToList();
    }
    
    [Benchmark]
    public List<CsvHelperSylvan> SylvanParse_General()
    {
        using var textReader = new StringReader(_content);
        using var dr = Sylvan.Data.Csv.CsvDataReader.Create(textReader);
        return dr.GetRecords<CsvHelperSylvan>().ToList();
    }
    
    [Benchmark]
    public List<CsvHelperSylvan> SylvanParse()
    {
        using var textReader = new StringReader(_content);
        using var dr = Sylvan.Data.Csv.CsvDataReader.Create(textReader);
    
        var list = new List<CsvHelperSylvan>();
        while (dr.Read())
        {
            list.Add(
                new CsvHelperSylvan
                {
                    Id = dr.GetString(0),
                    Date = dr.GetDateTime(1)
                }
            );
        }
    
        return list;
    }
    
    [Benchmark]
    public List<CsvHelperSylvan> SylvanParse_FastPath()
    {
        using var textReader = new StringReader(_contentFastPath);
        using var dr = Sylvan.Data.Csv.CsvDataReader.Create(textReader);
    
        var list = new List<CsvHelperSylvan>();
        while (dr.Read())
        {
            list.Add(
                new CsvHelperSylvan
                {
                    Id = dr.GetString(0),
                    Date = dr.GetDateTime(1)
                }
            );
        }
    
        return list;
    }
    
    [Benchmark]
    public List<SepModel> SepParse()
    {
        using var reader = Sep.Reader().FromText(_content);
        return Enumerate(reader).Select(x =>
            new SepModel
            {
                Id = x.Key,
                Date = x.Value
            }).ToList();
    }
    
    public static IEnumerable<(string Key, DateTime? Value)> Enumerate(SepReader reader)
    {
        using var sepReader = reader.GetEnumerator();
        foreach (var row in sepReader)
        {
            yield return (row[0].ToString(), row["Date"].Parse<DateTime>());
        }
    }
}