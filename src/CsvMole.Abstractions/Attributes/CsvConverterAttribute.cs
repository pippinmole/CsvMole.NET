namespace CsvMole.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class CsvConverterAttribute(Type converterType) : Attribute
{
    public Type ConverterType { get; } = converterType;
}