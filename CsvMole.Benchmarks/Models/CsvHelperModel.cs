using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;

namespace CsvMole.Benchmarks.Models;

public class CsvHelperModel
{
    [Index(0)]
    public string Id { get; set; } = null!;

    [Index(1)]
    [TypeConverter(typeof(DateTimeConverter))]
    public DateTime? Date { get; set; }
}