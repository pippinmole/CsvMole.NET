namespace CsvMole.Abstractions.Converters;

public class CsvDateTimeConverter : CsvConverterBase<DateTime?>
{
    public override DateTime? ConvertFromSpan(ReadOnlySpan<char> value)
    {
        if ( DateTime.TryParse(value, out var result) )
        {
            return result;
        }

        return null;
    }

    public override ReadOnlySpan<char> ConvertToString(DateTime? value)
    {
        return value?.ToString("yyyy-MM-dd") ?? ReadOnlySpan<char>.Empty;
    }
}