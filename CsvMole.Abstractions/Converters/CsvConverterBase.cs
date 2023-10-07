namespace CsvMole.Abstractions.Converters;

public abstract class CsvConverterBase<T>
{
    public abstract T? ConvertFromString(string value);

    public virtual string ConvertToString(T? value)
    {
        return value == null ? string.Empty : value.ToString();
    }
}