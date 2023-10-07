using CsvMole.Abstractions.Attributes;
using CsvMole.Example;

var parser = new CustomParser();
var stringReader = new StringReader("Id\n1");
var results = parser.Parse(stringReader);

foreach ( var result in results )
{
    Console.WriteLine(result);
}
