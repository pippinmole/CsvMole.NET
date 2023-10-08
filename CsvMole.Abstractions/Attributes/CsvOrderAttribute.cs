namespace CsvMole.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CsvOrderAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}