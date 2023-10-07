namespace CsvMole.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class CsvConverter(Type converterType) : Attribute
{
    public Type ConverterType { get; } = converterType;
}