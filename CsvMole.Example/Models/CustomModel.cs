using CsvMole.Abstractions.Attributes;
using CsvMole.Abstractions.Converters;

namespace CsvMole.Example.Models;

public class CustomModel
{
    [CsvOrder(0)]
    public string Id { get; set; } = null!;

    [CsvOrder(1),CsvConverter(typeof(CsvDateTimeConverter))]
    public DateTime? Date { get; set; }
}