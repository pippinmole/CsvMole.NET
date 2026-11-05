namespace CsvMole;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class CsvColumnAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
