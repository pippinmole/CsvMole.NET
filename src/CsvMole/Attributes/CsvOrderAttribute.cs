namespace CsvMole;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class CsvOrderAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}
