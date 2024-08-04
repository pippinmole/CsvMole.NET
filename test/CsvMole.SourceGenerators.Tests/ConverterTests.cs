using CsvMole.Abstractions.Attributes;
using CsvMole.Abstractions.Converters;
using CsvMole.Abstractions.Options;

namespace CsvMole.SourceGenerators.Tests;

[CsvParser]
public partial class ConverterParser
{
    public partial IEnumerable<ConverterModel> Parse(StringReader stringReader, CsvOptions? options);
}

public sealed class ConverterModel
{
    public string Id { get; set; } = null!;
    
    [CsvConverter(typeof(CsvDateTimeConverter))]
    public DateTime? Date { get; set; }
}

public class ConverterTests
{
    [Test]
    public void DateTimeConverter_ConvertsProperly()
    {
        // Arrange
        const string text = "Id,Date\n1,2021-01-01";
        using var stringReader = new StringReader(text);

        var options = new CsvOptions { HasHeader = true };
        var parser = new ConverterParser();

        // Act
        var result = parser.Parse(stringReader, options)
            .ToList();

        // Assert
        Assert.That(result, Is.All.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Has.Count.EqualTo(1));

        var first = result[0];
        Assert.Multiple(() =>
        {
            Assert.That(first.Id, Is.EqualTo("1"));
            Assert.That(first.Date, Is.EqualTo(new DateTime(2021, 1, 1)));
        });
    }
}