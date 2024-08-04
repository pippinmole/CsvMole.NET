namespace CsvMole.Abstractions.Converters;

public abstract class CsvConverterBase<T>
{
    public virtual T? ConvertFromString(string value) => ConvertFromSpan(value.AsSpan());
    public abstract T? ConvertFromSpan(ReadOnlySpan<char> value);

    public virtual ReadOnlySpan<char> ConvertToString(T? value)
    {
        return value == null ? ReadOnlySpan<char>.Empty : value.ToString() ?? ReadOnlySpan<char>.Empty;
    }
}