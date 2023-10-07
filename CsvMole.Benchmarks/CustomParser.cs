using CsvMole.Abstractions.Attributes;
using CsvMole.Benchmarks.Models;

namespace CsvMole.Benchmarks;

[CsvParser]
public static partial class CustomParserExample
{
    public static partial IEnumerable<CustomParserModel> Parse(StringReader stringReader);
}