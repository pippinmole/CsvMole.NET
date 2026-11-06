using System.Globalization;

namespace CsvMole.Example.Models;

// "$12.50" -> 12.50m
public sealed class DollarConverter : CsvConverter<decimal>
{
    public override bool TryParse(ReadOnlySpan<char> value, CultureInfo culture, out decimal result) =>
        decimal.TryParse(value.TrimStart('$'), NumberStyles.Number, culture, out result);
}

public sealed record Order(
    [property: CsvColumn("id")] int Id,
    [property: CsvColumn("placed")] DateOnly Placed,
    [property: CsvColumn("total"), CsvConverter(typeof(DollarConverter))] decimal Total,
    [property: CsvColumn("note")] string? Note);
