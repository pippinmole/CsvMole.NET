namespace CsvMole.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CsvDelimiterAttribute(string delimiter) : Attribute
{
    public string Delimiter { get; } = delimiter;
}