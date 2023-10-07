using CsvMole.Abstractions.Converters;

namespace CsvMole.Example.Converters;

public class DateTimeCsvConverter : CsvConverterBase<DateTime?>
{
    public override DateTime? ConvertFromString(string value)
    {
        if ( DateTime.TryParse(value, out var result) )
        {
            return result;
        }

        return null;
    }

    public override string ConvertToString(DateTime? value)
    {
        return value.HasValue ? value.Value.ToString("yyyy-MM-dd") : string.Empty;
    }
}