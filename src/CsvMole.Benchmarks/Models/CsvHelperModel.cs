using CsvHelper;

namespace CsvMole.Benchmarks.Models;

[CsvRecord]
public partial class CsvHelperModel
{
    [CsvColumn(Index = 0)]
    public string Id { get; set; } = null!;

    [CsvColumn(Index = 1)]
    public DateTime? Date { get; set; }
}