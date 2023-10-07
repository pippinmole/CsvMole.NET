namespace CsvMole.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class CsvOrder(int order) : Attribute
{
    public int Order { get; } = order;
}