using CsvMole.Abstractions.Attributes;
using CsvMole.Example;

Console.WriteLine(typeof(CsvParserAttribute).FullName!);

var parser = new CustomParser();
var stringReader = new StringReader("Id\n1");
var results = parser.Parse(stringReader);

foreach ( var result in results )
{
    Console.WriteLine(result.Id);
}