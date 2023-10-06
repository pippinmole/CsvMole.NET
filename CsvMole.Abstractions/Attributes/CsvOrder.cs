namespace CsvMole.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
public class CsvOrder : Attribute
{
    public int Order { get; }

    public CsvOrder(int order)
    {
        Order = order;
    }
}