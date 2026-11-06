using System.Globalization;

namespace CsvMole;

public abstract class CsvConverter<T>
{
    public abstract bool TryParse(ReadOnlySpan<char> value, CultureInfo culture, out T result);
}
