namespace CsvMole;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class CsvDelimiterAttribute(char delimiter) : Attribute
{
    public char Delimiter { get; } = delimiter;
}
