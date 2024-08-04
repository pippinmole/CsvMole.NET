using CsvMole.Abstractions.Attributes;
using CsvMole.Abstractions.Options;
using CsvMole.Example.Models;

namespace CsvMole.Example;

[CsvParser]
public partial class CustomParser
{
    public partial IEnumerable<CustomModel> Parse(StringReader stringReader, CsvOptions? options);
}