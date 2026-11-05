namespace CsvMole;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class CsvConverterAttribute(Type converterType) : Attribute
{
    public Type ConverterType { get; } = converterType;
}
