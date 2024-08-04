using CsvMole.Abstractions.Attributes;
using CsvMole.Abstractions.Converters;
using CsvMole.Abstractions.Options;
using CsvMole.Benchmarks.Models;

namespace CsvMole.Benchmarks;

[CsvParser]
public partial class CustomParserExample
{
    public partial IEnumerable<CustomParserModel> Parse(StringReader stringReader, CsvOptions? options = null);
}

public partial class CustomParserExample1
{
    public partial IEnumerable<CustomParserModel> Parse1(StringReader stringReader, CsvOptions? options = null);
}

public partial class CustomParserExample1
{
    private readonly CsvDateTimeConverter _dateTimeConverter = new();
    
    public partial IEnumerable<CustomParserModel> Parse1(StringReader stringReader, CsvOptions? options)
    {
        if ( options?.HasHeader ?? false )
        {
            stringReader.ReadLine();
        }

        while ( stringReader.ReadLine() is { } line )
        {
            var model = new CustomParserModel();
            var span = line.AsSpan();
            
            var start = 0;
            model.Id = ReadField(span, ref start).ToString();
            model.Date = _dateTimeConverter.ConvertFromSpan(ReadField(span, ref start));
            
            yield return model;
        }
    }

    private static ReadOnlySpan<char> ReadField(ReadOnlySpan<char> line, ref int start)
    {
        var end = line[start..].IndexOf(',');
        
        if(end == -1)
        {
            end = line.Length;
        }
        
        var fieldValue = line.Slice(start, end - start);

        start = end + 1;

        return fieldValue;
    }
}