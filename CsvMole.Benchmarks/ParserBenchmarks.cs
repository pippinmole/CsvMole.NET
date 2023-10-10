using System.Globalization;
using BenchmarkDotNet.Attributes;
using CsvHelper;
using CsvMole.Benchmarks.Models;
using nietras.SeparatedValues;
using Sylvan.Data;

namespace CsvMole.Benchmarks;

[MemoryDiagnoser(false)]
public class ParserBenchmarks
{
    private string _content = null!;
    private string _contentFastPath = null!;
    
    [Params(1, 100, 1_000, 100_000)]
    public int N { get; set; }
    
    [GlobalSetup]
    public void Setup()
    {
        _content = "Id,Date\n";
        _contentFastPath = "Id,Date\n";
        
        for (var i = 0; i < N; i++)
        {
            _content += $"{i},{DateTime.Now}\n";
            _contentFastPath += $"{i},{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
        }
    }
    
    [Benchmark]
    public List<CustomParserModel> SourceGeneratedParse()
    {
        var stringReader = new StringReader(_content);
        return CustomParserExample.Parse(stringReader).ToList();
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
        foreach (var row in reader)
        {
            yield return (row["Id"].ToString(), row["Date"].Parse<DateTime>());
        }
    }
}