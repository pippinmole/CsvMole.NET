using CsvMole.Abstractions.Options;
using CsvMole.Example;

// Open a file called test.csv
var text = await File.ReadAllTextAsync("test.csv");
using var stringReader = new StringReader(text);

var options = new CsvOptions { HasHeader = true };
var results = CustomParser.Parse(stringReader, options);

foreach ( var result in results )
{
    Console.WriteLine(result.Date);
}



