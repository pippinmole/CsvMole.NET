namespace CsvMole.Abstractions.Converters;

public class CsvDateTimeConverter : CsvConverterBase<DateTime?>
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
        if ( value.HasValue )
        {
            return value.Value.ToString("yyyy-MM-dd");
        }

        return string.Empty;
    }
}