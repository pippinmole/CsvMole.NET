using CsvMole;
using CsvMole.Example;

var path = Path.Combine(AppContext.BaseDirectory, "test.csv");
var parser = new OrderParser();

Console.WriteLine("Sync:");
using (var reader = new StreamReader(path))
    foreach (var order in parser.Parse(reader))
        Console.WriteLine(order);

Console.WriteLine("Async, skipping bad rows:");
var options = new CsvOptions { OnError = e => Console.WriteLine($"  skipped: {e}") };
using (var reader = new StreamReader(path))
    await foreach (var order in parser.ParseAsync(reader, options, CancellationToken.None))
        Console.WriteLine(order);
