using CsvMole.Abstractions.Attributes;
using CsvMole.Example;

var stringReader = new StringReader("Id\n1");
var results = CustomParser.Parse(stringReader);

foreach ( var result in results )
{
    Console.WriteLine(result);
}
