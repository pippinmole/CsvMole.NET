using CsvMole.Abstractions.Attributes;
using CsvMole.Abstractions.Options;
using CsvMole.Example.Models;

namespace CsvMole.Example;

[CsvParser]
public static partial class CustomParser
{
    public static partial IEnumerable<CustomModel> Parse(StringReader stringReader, CsvOptions? options);
}