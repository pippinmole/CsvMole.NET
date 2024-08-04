using CsvMole.Abstractions.Attributes;
using CsvMole.Abstractions.Converters;

namespace CsvMole.Example.Models;

[CsvDelimiter(",")]
public class CustomModel
{
    [CsvOrder(0)]
    public string Id { get; set; } = null!;

    [CsvOrder(1)]
    [CsvConverter(typeof(CsvDateTimeConverter))]
    public DateTime? Date { get; set; }
    
    [CsvOrder(2)]
    public string? Age { get; set; }
    
    [CsvOrder(3)]
    public string? Description { get; set; }
}