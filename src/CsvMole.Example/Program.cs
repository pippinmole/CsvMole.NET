using CsvMole.Abstractions.Options;
using CsvMole.Example;

// Open a file called test.csv
var text = await File.ReadAllTextAsync("test.csv");
using var stringReader = new StringReader(text);
var parser = new CustomParser();

var options = new CsvOptions { HasHeader = true };
var results = parser.Parse(stringReader, options);

foreach ( var result in results )
{
    Console.WriteLine(result.Description);
}



