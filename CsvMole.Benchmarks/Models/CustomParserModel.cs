using CsvMole.Abstractions.Attributes;
using CsvMole.Example.Converters;

namespace CsvMole.Benchmarks.Models;

public class CustomParserModel
{
    [CsvOrder(0)]
    public string Id { get; set; } = null!;

    [CsvOrder(1)]
    [CsvConverter(typeof(DateTimeCsvConverter))]
    public DateTime? Date { get; set; }
}